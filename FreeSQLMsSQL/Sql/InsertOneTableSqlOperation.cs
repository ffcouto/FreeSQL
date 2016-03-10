/*
FreeSQL
Copyright (C) 2016 Fabiano Couto

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Suite 500, Boston, MA 02110-1335, USA.
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.MsSQL
{
   internal class InsertOneTableSqlOperation<T> : SqlOperation
   {
      // variáveis locais
      private readonly T obj;
      private readonly int tabIndex;
      private int newID;

      public InsertOneTableSqlOperation(T obj, int tableIndex, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         this.obj = obj;
         this.tabIndex = tableIndex;
      }

      public override void Execute()
      {
         try
         {
            // faz a leitura das tabela especificada e possui permissão para inclusão (Crud - Create)
            var t = GetTableAttributes<T>().FirstOrDefault(a => a.Index == tabIndex && a.CRUD.HasFlag(CrudOptions.Create));

            if (t == null)
               throw new IndexOutOfRangeException("O índice informado não pertence a nenhuma tabela definida.");

            // faz a leitura da propriedade que contém a chave primária
            var pk = GetPrimaryKeyProperty<T>(t);
            var key = GetKeyAttribute(pk, t);

            // é um campo primário e não é auto-numeração; recupera o próximo ID da tabela
            if (key.IsPrimary && !key.AutoIncrement)
            {
               var idCommand = GetNextIDCommand<T>(t);
               newID = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
               pk.SetValue(obj, newID, null);
            }

            // não é um campo primário e não é auto-numeração; retorna o próprio valor da propriedade
            else if (!key.IsPrimary && !key.AutoIncrement)
            {
               newID = (int)pk.GetValue(obj, null);
            }

            // executa a inserção
            var readCommand = GetInsertCommand<T>(obj, t);
            ExecuteCommand(readCommand);

            // é um campo auto-numeração; recupera o ID gerado pela banco
            if (key.AutoIncrement)
            {
               var idCommand = GetLastIDCommand(t);
               newID = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
               pk.SetValue(obj, newID, null);
            }
         }
         catch { throw; }
      }

      public int NewID
      {
         get { return newID; }
      }
   }
}