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
   internal class SelectTopWhereSqlOperation<T> : SqlOperation
   {
      // local variables
      private readonly int wRows;
      private readonly string[] wFilterColumns;
      private readonly string[] wOperators;
      private readonly object[] wValues;
      private readonly string[] wSortColumns;
      private readonly bool[] wDesc;

      private T[] retObjs = new T[] { };

      public SelectTopWhereSqlOperation(int rows, string fColumn, string comparison, object value, string sColumn, bool desc, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         wRows = rows;
         wFilterColumns = new string[] { fColumn };
         wOperators = new string[] { comparison };
         wValues = new object[] { value };
         wSortColumns = new string[] { sColumn };
         wDesc = new bool[] { desc };
      }

      public SelectTopWhereSqlOperation(int rows, string[] fColumns, string[] comparison, object[] values, string[] sColumns, bool[] desc, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         wRows = rows;
         wFilterColumns = fColumns;
         wOperators = comparison;
         wValues = values;
         wSortColumns = sColumns;
         wDesc = desc;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectTopCommand(wRows, wFilterColumns, wOperators, wValues, wSortColumns, wDesc);
            var rows = LoadDataFromCommand(readCommand);
            retObjs = GetEntities<T>(rows, GetFieldAttributes<T>());
         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }

      private SqlCommand GetSelectTopCommand(int topRows, string[] fColumns, string[] comparison, object[] values, string[] sColumns, bool[] descs)
      {
         // checks whether the number of columns and search values are equal
         if ((fColumns.Length != comparison.Length) || (fColumns.Length != values.Length))
            throw new Exception("O número de colunas e valores de pesquisa são inconsistentes.");

         // checks whether the number of columns and sort order are equal
         if (sColumns.Length != descs.Length)
            throw new Exception("O número de colunas e sequências de ordenação são inconsistentes.");

         // the number of return lines must be greater than 0
         if (topRows < 1)
            throw new Exception("O número de retorno de linhas deve ser maior que 0 (zero).");

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
         var alias = new List<string>(GetAliasForColumns(fColumns));

         // creates the command
         var cmd = new SqlCommand();

         // stores the filter list of the command
         var filter = new List<string>(GetColumnsAndParametersForFilters(cmd, fldAttr, fColumns, alias.ToArray(), comparison, values));

         // when the table has virtual exclusion
         // there should be a filter only of active records
         if (tabAttr[0].VirtualDelete)
         {
            filter.Add(string.Format("(t{0}.ativo = @ativo)", tabAttr[0].Index));
            cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;
         }

         // stores the fields for sorting
         var sort = new List<string>(GetColumnsForSort(fldAttr, sColumns, descs));

         // query command
         string query = "SELECT TOP {0} {1} FROM {2} WHERE {3} ORDER BY {4};";
         string fields = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();
         string where = string.Join(" AND ", filter);
         string order = string.Join(", ", sort);

         // sets the command to execute
         cmd.CommandText = string.Format(query, topRows, fields, tables, where, order);
         return cmd;
      }
   }
}