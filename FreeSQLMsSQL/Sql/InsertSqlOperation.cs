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
   internal class InsertSqlOperation<T> : SqlOperation
   {
      // variáveis locais
      private readonly T obj;
      private object[] newIDs;

      public InsertSqlOperation(T obj, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         this.obj = obj;
      }

      public override void Execute()
      {
         try
         {
            // faz a leitura das tabelas com permissão para inclusão (Crud - Create)
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Create)).ToArray();

            // define o número de retornos dos id's das tabelas
            newIDs = new object[tables.Length];

            foreach (var t in tables)
            {
               // faz a leitura da propriedade que contém a chave primária
               var pk = GetPrimaryKeyProperty<T>(t);
               var key = GetKeyAttribute(pk, t);

               // não é um campo identidade e é auto-numeração; recupera o próximo ID da tabela
               if (!key.IsPrimary && key.AutoIncrement)
               {
                  var idCommand = GetNextIDCommand<T>(t);
                  newIDs[t.Index] = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
                  pk.SetValue(obj, newIDs[t.Index], null);
               }
               
               // não é um campo identidade e não é auto-numeração; retorna o próprio valor da propriedade
               else if (!key.IsPrimary && !key.AutoIncrement)
               {
                  newIDs[t.Index] = pk.GetValue(obj, null);
               }

               // executa a inserção
               var readCommand = GetInsertCommand(obj, t);
               ExecuteCommand(readCommand);

               // é um campo identidade; recupera o ID gerado pela banco
               if (key.IsPrimary)
               {
                  var idCommand = GetLastIDCommand(t);
                  newIDs[t.Index] = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
                  pk.SetValue(obj, newIDs[t.Index], null);
               }
            }
         }
         catch { throw; }
      }

      public object[] NewIDs
      {
         get { return newIDs; }
      }
   }
}