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
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.MsSQL
{
   internal class UpdateSpecialSqlOperation<T> : SqlOperation
   {
      // local variables
      private readonly T wObj;
      private readonly string wColumn;

      public UpdateSpecialSqlOperation(T obj, string column, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         wObj = obj;
         wColumn = column;
      }

      public override void Execute()
      {
         try
         {
            // reads the fields corresponding to the specified column
            var pk = GetProperty<T>(wColumn);
            var sf = pk.GetCustomAttributes(true).Where(a => a.GetType() == typeof(SqlField)).Cast<SqlField>().ToArray();
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Update)).ToArray();

            foreach (var f in sf)
            {
               var updCommand = GetUpdateSpecialCommand(wObj, tables[f.TableIndex], wColumn);
               ExecuteCommand(updCommand);
            }
         }
         catch { throw; }
      }

      private SqlCommand GetUpdateSpecialCommand(T obj, Table t, string column)
      {
         // allowed to update (crUd - UPDATE)?
         if (!t.CRUD.HasFlag(CrudOptions.Update))
            throw new Exception(string.Format("A tabela {0} não possui permissão para atualização de registros.", t.TableName));

         // gets the primary key and field data
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);
         var pv = pk.GetValue(obj, null);

         // gets the attributes of the field to be updated
         var uk = GetProperty<T>(column);
         var uf = GetField(uk, t.Index);
         var uv = uk.GetValue(obj, null);

         // update command
         string sql = "UPDATE {0} SET {1} = @{1} WHERE ({2} = @{2});";

         // creates the command
         var cmd = new SqlCommand();
         cmd.CommandText = string.Format(sql, t.TableName, uf.FieldName, pf.FieldName);
         cmd.Parameters.Add(string.Format("@{0}", uf.FieldName), (SqlDbType)uf.DatabaseType).Value = ParseValue(uv);
         cmd.Parameters.Add(string.Format("@{0}", pf.FieldName), (SqlDbType)pf.DatabaseType).Value = ParseValue(pv);
         return cmd;
      }
   }
}