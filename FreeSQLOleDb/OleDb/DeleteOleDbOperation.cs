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
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class DeleteOleDbOperation<T> : OleDbOperation
   {
      // variáveis locais
      private readonly T obj;

      public DeleteOleDbOperation(T obj, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.obj = obj;
      }

      public override void Execute()
      {
         try
         {
            // faz a leitura das tabelas com permissão para exclusão (cruD - DELETE)
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Delete)).ToArray();

            foreach (var t in tables)
            {
               var delCommand = GetDeleteCommand(obj, t);
               ExecuteCommand(delCommand);
            }
         }
         catch { throw; }
      }

      private OleDbCommand GetDeleteCommand(T obj, Table t)
      {
         // possui permissão para exclusão (cruD - DELETE)?
         if (!t.CRUD.HasFlag(CrudOptions.Delete))
            throw new Exception(string.Format("A tabela {0} não possui permissão para exclusão de registros.", t.TableName));

         // atributos personalizados
         var prop = GetProperties(obj);

         // obtém a chave primária e os dados do campo
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);

         // comando para exclusão física
         string sql = "DELETE FROM {0} WHERE ({1} = ?);";

         // exclusão do registro é virtual
         if (t.VirtualDelete)
            sql = "UPDATE {0} SET ativo = ? WHERE ({1} = ?);";

         // cria o comando
         var cmd = new OleDbCommand();
         cmd.CommandText = string.Format(sql, t.TableName, pf.FieldName);
         if (t.VirtualDelete) cmd.Parameters.Add("@ativo", OleDbType.Boolean).Value = false;
         cmd.Parameters.Add(pf.FieldName, (OleDbType)pf.DatabaseType).Value = pk.GetValue(obj, null);
         return cmd;
      }
   }
}