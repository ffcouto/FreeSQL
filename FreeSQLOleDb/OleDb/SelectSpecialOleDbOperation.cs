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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class SelectSpecialOleDbOperation<T> : OleDbOperation
   {
      // variáveis locais
      private readonly string[] wColumns;
      private readonly string[] wOperators;
      private readonly object[] wValues;

      private T[] retObjs = new T[] { };

      public SelectSpecialOleDbOperation(string column, string comparison, object value, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         wColumns = new string[] { column };
         wOperators = new string[] { comparison };
         wValues = new object[] { value };
      }

      public SelectSpecialOleDbOperation(string[] columns, string[] comparison, object[] values, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         wColumns = columns;
         wOperators = comparison;
         wValues = values;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectSpecialCommand(wColumns, wOperators, wValues);
            var rows = LoadDataFromCommand(readCommand);
            retObjs = GetEntities<T>(rows, GetFieldAttributes<T>());
         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }

      private OleDbCommand GetSelectSpecialCommand(string[] columns, string[] comparison, object[] values)
      {
         // checks whether the number of columns and values are equal
         if ((columns.Length != comparison.Length) || (columns.Length != values.Length))
            throw new Exception("O número de colunas e valores são inconsistentes.");

         // custom attributes with read permission (cRud - Read)
         var tabAttr = GetTableAttributes<T>().Where(a => a.CRUD.HasFlag(CrudOptions.Read)).ToArray();
         var propAttr = GetProperties(Activator.CreateInstance<T>());
         var joinAttr = GetJoinAttributeProperties<T>();
         var fldAttr = GetFieldAttributes<T>();

         // stores the tables fields
         var cols = new List<string>(GetColumnsFromEntity(tabAttr, propAttr));

         // stores the existing join commands
         var joins = new List<string>(GetJoinsFromEntity(tabAttr, joinAttr));

         // stores the fields by replacing an alias to avoid duplicity
         var alias = new List<string>(GetAliasForColumns(columns));

         // creates command
         var cmd = new OleDbCommand();

         // stores the filter list of the command
         var filter = new List<string>(GetColumnsAndParametersForFilters(cmd, fldAttr, columns, alias.ToArray(), comparison, values));

         // when the table has virtual exclusion
         // there should be a filter only of active records
         if (tabAttr[0].VirtualDelete)
         {
            filter.Add(string.Format("(t{0}.ativo = ?)", tabAttr[0].Index));
            cmd.Parameters.Add("@ativo", OleDbType.Boolean).Value = true;
         }

         // OleDb provider requires use of parentheses in expression of joins
         for (int i = 0; i < joins.Count; i++)
            joins[i] = string.Concat(joins[i], ")");

         string parentesis = string.Join("", ArrayList.Repeat("(", joins.Count).ToArray());

         // query command
         string query = "SELECT {0} FROM {1} WHERE {2};";
         string fields = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();
         string where = string.Join(" AND ", filter);

         // sets the command to execute
         cmd.CommandText = string.Format(query, fields, tables, where);
         return cmd;
      }
   }
}