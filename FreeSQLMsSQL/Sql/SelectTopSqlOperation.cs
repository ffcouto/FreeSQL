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
   internal class SelectTopSqlOperation<T> : SqlOperation
   {
      // local variables
      private readonly int wRows;
      private readonly string[] wColumns;
      private readonly bool[] wDesc;

      private T[] retObjs = new T[] { };

      public SelectTopSqlOperation(int rows, string column, bool desc, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         wRows = rows;
         wColumns = new string[] { column };
         wDesc = new bool[] { desc };
      }

      public SelectTopSqlOperation(int rows, string[] columns, bool[] desc, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         wRows = rows;
         wColumns = columns;
         wDesc = desc;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectTopCommand(wRows, wColumns, wDesc);
            var rows = LoadDataFromCommand(readCommand);
            retObjs = GetEntities<T>(rows, GetFieldAttributes<T>());
         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }

      private SqlCommand GetSelectTopCommand(int topRows, string[] columns, bool[] descs)
      {
         // checks whether the number of columns and values are equal
         if (columns.Length != descs.Length)
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

         // stores the fields for sorting
         var sort = new List<string>(GetColumnsForSort(fldAttr, columns, descs));

         // query command
         string query = "SELECT TOP {0} {1} FROM {2} {3}ORDER BY {4};";
         string fields = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();
         string order = string.Join(", ", sort);
         string where = (!tabAttr[0].VirtualDelete) ? "" : string.Format("WHERE (t{0}.ativo = @ativo) ", tabAttr[0].Index);

         // creates the command
         var cmd = new SqlCommand();
         cmd.CommandText = string.Format(query, topRows, fields, tables, where, order);
         if (tabAttr[0].VirtualDelete) cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;
         return cmd;
      }
   }
}