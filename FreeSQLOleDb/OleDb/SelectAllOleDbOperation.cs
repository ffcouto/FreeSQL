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
   internal class SelectAllOleDbOperation<T> : OleDbOperation
   {
      // local variables
      private readonly string wOrderField;
      private T[] retObjs = new T[] { };

      public SelectAllOleDbOperation(string orderField, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         wOrderField = orderField;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectAllCommand(wOrderField);
            var rows = LoadDataFromCommand(readCommand);
            retObjs = GetEntities<T>(rows, GetFieldAttributes<T>());
         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }

      private OleDbCommand GetSelectAllCommand(string sortField)
      {
         // custom attributes with read permission (cRud - Read)
         var tabAttr = GetTableAttributes<T>().Where(a => a.CRUD.HasFlag(CrudOptions.Read)).ToArray();
         var propAttr = GetProperties(Activator.CreateInstance<T>());
         var joinAttr = GetJoinAttributeProperties<T>();

         // stores the tables fields
         var cols = new List<string>(GetColumnsFromEntity(tabAttr, propAttr));

         // stores the existing join commands
         var joins = new List<string>(GetJoinsFromEntity(tabAttr, joinAttr));

         // query command
         string query = "SELECT {0} FROM {1} ORDER BY {2};";

         // when the table has virtual exclusion
         // there should be a filter only of active records
         var tab = tabAttr.FirstOrDefault(a => a.VirtualDelete == true);

         if (tab != null)
            query = "SELECT {0} FROM {1} WHERE (t{3}.ativo = ?) ORDER BY {2};";

         // OleDb provider requires use of parentheses in expression of joins
         for (int i = 0; i < joins.Count; i++)
            joins[i] = string.Concat(joins[i], ")");

         string parentesis = string.Join("", ArrayList.Repeat("(", joins.Count).ToArray());

         string columns = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();

         // creates the command
         var cmd = new OleDbCommand();
         
         if (tab == null)
            cmd.CommandText = string.Format(query, columns, tables, sortField);
         else
            cmd.CommandText = string.Format(query, columns, tables, sortField, tab.Index);

         // sets the filter parameter
         if (tab != null)
            cmd.Parameters.Add("@ativo", OleDbType.Boolean).Value = true;

         return cmd;
      }
   }
}