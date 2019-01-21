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
   internal class SelectSqlCeOperation<T> : SqlCeOperation
   {
      // local variables
      private readonly int idObj;
      private T retObj = default(T);

      public SelectSqlCeOperation(int codObj, SqlCeConnection connection, SqlCeTransaction transaction)
         : base(connection, transaction)
      {
         idObj = codObj;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectCommand(idObj);
            var row = LoadDataRowFromCommand(readCommand);
            retObj = GetEntity<T>(row, GetFieldAttributes<T>());
         }
         catch { throw; }
      }
      
      public T ReturnObject
      {
         get { return retObj; }
      }

      private SqlCeCommand GetSelectCommand(object value)
      {
         // custom attributes with read permission (cRud - Read)
         var tabAttr = GetTableAttributes<T>().Where(a => a.CRUD.HasFlag(CrudOptions.Read)).ToArray();
         var propAttr = GetProperties(Activator.CreateInstance<T>());
         var joinAttr = GetJoinAttributeProperties<T>();

         // stores the tables fields
         var cols = new List<string>(GetColumnsFromEntity(tabAttr, propAttr));

         // stores the existing join commands
         var joins = new List<string>(GetJoinsFromEntity(tabAttr, joinAttr));

         // gets primary key from main table
         // and read the property
         var pk = GetPrimaryKeyProperty<T>(tabAttr[0]);
         var pf = GetField(pk, tabAttr[0].Index);

         // query command
         string query = "SELECT {0} FROM {1} WHERE {2};";
         string columns = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();
         string filters = (tabAttr[0].VirtualDelete) ? "(t{1}.{0} = @{0}) AND (t{1}.ativo = @ativo)" : "(t{1}.{0} = @{0})";
         string where = string.Format(filters, pf.FieldName, tabAttr[0].Index);

         // creates the command
         var cmd = new SqlCeCommand();
         cmd.CommandText = string.Format(query, columns, tables, where);
         cmd.Parameters.Add(pf.FieldName, (SqlDbType)pf.DatabaseType).Value = ParseValue(value);
         if (tabAttr[0].VirtualDelete) cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;
         return cmd;
      }
   }
}