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
using System.Data.SqlServerCe;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.SqlCe
{
   internal class DeleteSpecialSqlOperation<T> : SqlCeOperation
   {
      // local variables
      private readonly string[] wColumns;
      private readonly object[] wValues;
      private readonly bool wIgnore;

      public DeleteSpecialSqlOperation(string column, object value, bool ignoreVirtual, SqlCeConnection connection, SqlCeTransaction transaction)
         : base(connection, transaction)
      {
         wColumns = new string[] { column };
         wValues = new object[] { value };
         wIgnore = ignoreVirtual;
      }

      public DeleteSpecialSqlOperation(string[] columns, object[] values, bool ignoreVirtual, SqlCeConnection connection, SqlCeTransaction transaction)
         : base(connection, transaction)
      {
         wColumns = columns;
         wValues = values;
         wIgnore = ignoreVirtual;
      }

      public override void Execute()
      {
         try
         {
            var delCommand = GetDeleteSpecialCommand(wColumns, wValues);
            ExecuteCommand(delCommand);
         }
         catch { throw; }
      }

      private SqlCeCommand GetDeleteSpecialCommand(string[] columns, object[] values)
      {
         // checks whether the number of columns and values are equal
         if (columns.Length != values.Length)
            throw new Exception("O número de colunas e valores são inconsistentes.");

         // custom attributes
         var t = GetTableAttributes<T>()[0];

         // allowed to delete (cruD - DELETE)?
         if (!t.CRUD.HasFlag(CrudOptions.Delete))
            throw new Exception(string.Format("A tabela {0} não possui permissão para exclusão de registros.", t.TableName));

         // table properties
         var filter = new List<string>();

         // creates the command
         var cmd = new SqlCeCommand();

         // creates where clause filters according to columns and values specified
         for (int i = 0; i < columns.Length; i++)
         {
            // get the attributes of the column
            var pf = (SqlCeField)GetFieldAttributes<T>().Where(a => a.FieldName.ToLower() == columns[i].ToLower()).ToList()[0];

            // add to temporary list
            filter.Add(string.Format("({0} = @{0})", pf.FieldName));

            // adds the parameter corresponding to column
            cmd.Parameters.Add(string.Format("@{0}", pf.FieldName), (SqlDbType)pf.DatabaseType).Value = ParseValue(values[i]);
         }

         // command for physical exclusion
         string sql = "DELETE FROM {0} WHERE {1};";
         string where = string.Join(" AND ", filter);

         // deletion of the record is virtual
         if (!wIgnore && t.VirtualDelete)
            sql = "UPDATE {0} SET ativo = @ativo WHERE {1};";

         // define the command to execute
         cmd.CommandText = string.Format(sql, t.TableName, where);
         if (!wIgnore && t.VirtualDelete) cmd.Parameters.Insert(0, new SqlCeParameter("@ativo", SqlDbType.Bit) { Value = false });
         return cmd;
      }
   }
}