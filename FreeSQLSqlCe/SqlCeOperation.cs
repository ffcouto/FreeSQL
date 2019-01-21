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
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FreeSQL;
using FreeSQL.Backwork;
using FreeSQL.Database;

namespace FreeSQL.Database.SqlCe
{
   internal abstract class SqlCeOperation : Operation
   {
      public SqlCeOperation(SqlCeConnection connection)
         : this(connection, null)
      { }

      public SqlCeOperation(SqlCeConnection connection, SqlCeTransaction transaction)
         : base(connection, transaction)
      { }

      protected override DataRow[] LoadDataFromCommand(IDbCommand command)
      {
         command.Connection = (SqlCeConnection)_conn;
         command.Transaction = (SqlCeTransaction)_trans;
         var adapter = new SqlCeDataAdapter((SqlCeCommand)command);
         var dataset = new DataSet();
         adapter.Fill(dataset);
         var table = dataset.Tables["table"];
         return table.Select();
      }

      protected override DataRow LoadDataRowFromCommand(IDbCommand command)
      {
         command.Connection = (SqlCeConnection)_conn;
         command.Transaction = (SqlCeTransaction)_trans;
         var adapter = new SqlCeDataAdapter((SqlCeCommand)command);
         var dataset = new DataSet();
         adapter.Fill(dataset);
         var table = dataset.Tables["table"];

         if (table.Rows.Count == 0)
            return null;

         return table.Rows[0];
      }

      protected override string[] GetColumnsAndParametersForFilters(IDbCommand command, Field[] fields, string[] columns, string[] alias, string[] operators, object[] values)
      {
         // stores the filters
         var filter = new List<string>();

         // creates Where clause filters according to columns and values specified
         for (int i = 0; i < columns.Length; i++)
         {
            // get the attributes of the column
            var pf = (SqlCeField)fields.Where(a => a.FieldName.ToLower() == columns[i].ToLower()).ToList()[0];

            // special cases of the where clause
            if (operators[i].ToUpper() == "IS" || operators[i].ToUpper() == "!IS")
            {
               // add to temporary list
               filter.Add(string.Format("({4}t{0}.{1} {3} {2})", pf.TableIndex, pf.FieldName, values[i],
                  operators[i], (operators[i].StartsWith("!") ? "NOT " : "")));
            }

            // special case IN / NOT IN; it is necessary to inform a list item of a parameter
            else if (operators[i].ToUpper() == "IN" || operators[i].ToUpper() == "!IN")
            {
               // converts the contents of values [i] to an IEnumerable type
               var val = values[i] as IEnumerable;

               // parameter is not an array
               if (val == null)
                  throw new Exception("Valor do parâmetro para o operador IN não é uma matriz de objetos válida.");

               // gets the array from the value
               var o = val.Cast<object>().ToArray();

               // list of fields for each array value
               var inFlds = new List<string>();
               for (int p = 0; p < o.Length; p++)
                  inFlds.Add(string.Format("@{0}_{1}", alias[i], p));

               // add to temporary list
               filter.Add(string.Format("({3}t{0}.{1} IN ({2}))", pf.TableIndex, pf.FieldName,
                  string.Join(", ", inFlds), (operators[i].StartsWith("!") ? "NOT " : "")));

               // adds the parameter to each array value
               for (int p = 0; p < o.Length; p++)
                  ((SqlCeCommand)command).Parameters.Add(inFlds[p], (SqlDbType)pf.DatabaseType).Value = ParseValue(o[p]);
            }

            // other cases
            else
            {
               // add to temporary list
               filter.Add(string.Format("(t{0}.{1} {3} @{2})", pf.TableIndex, pf.FieldName, alias[i], operators[i]));
               // adds the parameter corresponding to column
               ((SqlCeCommand)command).Parameters.Add(string.Format("@{0}", alias[i]), (SqlDbType)pf.DatabaseType).Value = ParseValue(values[i], operators[i]);
            }
         }

         // returns filter list
         return filter.ToArray();
      }

      protected override Field[] GetFieldAttributes<T>()
      {
         var fields = new List<Field>();
         var properties = typeof(T).GetProperties();

         // read the properties of the object
         foreach (var info in properties)
         {
            var field = info.GetCustomAttributes(true).Where(a => a.GetType() == typeof(SqlCeField))
               .Cast<SqlCeField>()
               .OrderBy(b => b.TableIndex)
               .ToArray();

            if (field != null && field.Length > 0)
               fields.Add(field[0]);
         }

         return fields.ToArray();
      }

      protected override Field GetField(PropertyInfo property, int tableIndex)
      {
         return (SqlCeField)property.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType() == typeof(SqlCeField) && ((SqlCeField)a).TableIndex == tableIndex);
      }

      internal protected SqlCeCommand GetInsertCommand<T>(T obj, Table t)
      {
         // allowed to include (Crud - CREATE)?
         if (!t.CRUD.HasFlag(CrudOptions.Create))
            throw new Exception(string.Format("A tabela {0} não possui permissão para inclusão de registros.", t.TableName));

         // reads custom attributes
         var prop = GetProperties(obj);

         // local variables
         var fList = new List<string>();
         var vList = new List<string>();

         // creates command
         var cmd = new SqlCeCommand();

         // read the properties
         foreach (PropertyInfo item in prop)
         {
            if (item.GetCustomAttributes(true).GetLength(0) > 0)
            {
               // gets the attributes associated with the field
               var f = GetField(item, t.Index);
               var k = GetKeyAttribute(item, t);

               // is the key/identity field?
               bool keyId = (k != null && k.IsIdentity);

               if (!keyId && f != null && f.FieldName != "" && f.TableIndex == t.Index)
               {
                  // gets the value of the entity's property
                  var value = item.GetValue(obj, null);

                  // fill list of fields and values
                  fList.Add(f.FieldName);
                  vList.Add(string.Format("@{0}", f.FieldName));

                  // includes a parameter to the command
                  cmd.Parameters.Add(f.FieldName, (SqlDbType)f.DatabaseType).Value = ParseValue(value);
               }
            }
         }

         // adds the virtual exclusion marker field
         if (t.VirtualDelete)
         {
            // verifies that the "Active" field is already in the list;
            // this avoids duplication of field and parameter e
            // consequently, error in the statement
            if (!fList.Contains("ativo"))
            {
               fList.Add("ativo");
               vList.Add("@ativo");
               cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;
            }
         }

         // convert lists
         string fields = string.Join(", ", fList);
         string values = string.Join(", ", vList);

         // define the command and assign it
         string query = "INSERT INTO {0} ({1}) VALUES ({2});";
         cmd.CommandText = string.Format(query, t.TableName, fields, values);

         // returns the command
         return cmd;
      }

      internal protected SqlCeCommand GetNextIDCommand<T>(Table t)
      {
         // obtains the primary key and field data
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);

         // query command
         string query = "SELECT ISNULL(MAX({0}), 0) + 1 AS next_id FROM {1};";

         // creates the command
         var cmd = new SqlCeCommand();
         cmd.CommandText = string.Format(query, pf.FieldName, t.TableName);
         return cmd;
      }

      internal protected SqlCeCommand GetLastIDCommand(Table t)
      {
         string query = string.Format("SELECT ident_current('{0}') AS last_id;", t.TableName);
         var cmd = new SqlCeCommand(query);
         return cmd;
      }
   }
}