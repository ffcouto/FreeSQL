/*
FreeSQL
Copyright (C) 2016-2019 Fabiano Couto

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class DeleteOleDbOperation<T> : OleDbOperation
   {
      // local variables
      private readonly T wObj;
      private readonly bool wIgnore;

      public DeleteOleDbOperation(T obj, bool ignoreVirtual, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         wObj = obj;
         wIgnore = ignoreVirtual;
      }

      public override void Execute()
      {
         try
         {
            // read tables with delete permission (cruD - DELETE)
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Delete)).ToArray();

            foreach (var t in tables)
            {
               var delCommand = GetDeleteCommand(wObj, t);
               ExecuteCommand(delCommand);
            }
         }
         catch { throw; }
      }

      private OleDbCommand GetDeleteCommand(T obj, Table t)
      {
         // allowed to delete (cruD - DELETE)?
         if (!t.CRUD.HasFlag(CrudOptions.Delete))
            throw new Exception(string.Format("A tabela {0} não possui permissão para exclusão de registros.", t.TableName));

         // custom attributes
         var prop = GetProperties(obj);

         // obtains the primary key and field data
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);

         // command for physical exclusion
         string sql = "DELETE FROM {0} WHERE ({1} = ?);";

         // deletion of the record is virtual
         if (!wIgnore && t.VirtualDelete)
            sql = "UPDATE {0} SET ativo = ? WHERE ({1} = ?);";

         // creates the command
         var cmd = new OleDbCommand();
         cmd.CommandText = string.Format(sql, t.TableName, pf.FieldName);
         if (!wIgnore && t.VirtualDelete) cmd.Parameters.Add("@ativo", OleDbType.Boolean).Value = false;
         cmd.Parameters.Add(pf.FieldName, (OleDbType)pf.DatabaseType).Value = pk.GetValue(obj, null);
         return cmd;
      }
   }
}