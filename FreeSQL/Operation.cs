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
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using FreeSQL;
using FreeSQL.Backwork;

namespace FreeSQL.Database
{
   public abstract class Operation
   {
      protected readonly IDbConnection _conn;
      protected readonly IDbTransaction _trans;

      public Operation(IDbConnection connection)
         : this(connection, null)
      { }

      public Operation(IDbConnection connection, IDbTransaction transaction)
      {
         _conn = connection;
         _trans = transaction;
      }

      public abstract void Execute();

      protected abstract DataRow[] LoadDataFromCommand(IDbCommand command);

      protected abstract DataRow LoadDataRowFromCommand(IDbCommand command);

      protected int ExecuteCommand(IDbCommand command)
      {
         int recordsAffected = 0;

         if (command != null)
         {
            command.Connection = _conn;
            command.Transaction = _trans;
            recordsAffected = command.ExecuteNonQuery();
         }

         return recordsAffected;
      }

      protected object ExecuteCommandAndReturn(IDbCommand command)
      {
         if (command == null)
            return 0;

         command.Connection = _conn;
         command.Transaction = _trans;
         var ret = command.ExecuteScalar();

         if (ret == null || ret == DBNull.Value)
            return null;
         else
            return ret;
      }

      protected virtual Table[] GetTableAttributes<T>()
      {
         var tables = typeof(T).GetCustomAttributes(true)
            .Where(a => a.GetType() == typeof(Table))
            .Cast<Table>()
            .OrderBy(b => b.Index)
            .ToArray();

         return tables;
      }

      protected abstract Field[] GetFieldAttributes<T>();

      protected virtual Key GetKeyAttribute(PropertyInfo property, Table t)
      {
         return property.GetCustomAttributes(true)
           .Where(a => a.GetType() == typeof(Key))
           .Cast<Key>()
           .FirstOrDefault(a => a.TableIndex == t.Index);
      }

      protected abstract Field GetField(PropertyInfo property, int tableIndex);

      protected PropertyInfo[] GetProperties<T>(T obj)
      {
         return obj.GetType().GetProperties();
      }

      protected PropertyInfo GetProperty<T>(string column)
      {
         var flds = new List<PropertyInfo>();
         var prop = typeof(T).GetProperties();

         // read the properties and returns the list
         // that contains the Key attribute
         foreach (var p in prop)
         {
            var attr = p.GetCustomAttributes(true).ToArray();
            var pi = attr.Where(a => a is Field).ToArray();
            if (pi != null && pi.Length > 0) flds.Add(p);
         }

         var field = flds.FirstOrDefault(a => a.GetCustomAttributes(true)
           .Where(b => b is Field)
           .Cast<Field>()
           .FirstOrDefault(c => c.FieldName.ToLower() == column.ToLower()) != null);

         return field;
      }

      protected virtual PropertyInfo GetPrimaryKeyProperty<T>(Table t)
      {
         var keys = new List<PropertyInfo>();
         var prop = typeof(T).GetProperties();

         // read the properties and returns the list
         // that contains the Key attribute
         foreach (var p in prop)
         {
            var attr = p.GetCustomAttributes(true).ToArray();
            var pi = attr.Where(a => a.GetType() == typeof(Key)).ToArray();
            if (pi != null && pi.Length > 0) keys.Add(p);
         }

         // returns the property that contains the Key attribute associated with the indicated table
         var key = keys.FirstOrDefault(a => a.GetCustomAttributes(true)
            .Where(b => b.GetType() == typeof(Key))
            .Cast<Key>()
            .FirstOrDefault().TableIndex == t.Index);

         return key;
      }

      protected virtual PropertyInfo[] GetJoinAttributeProperties<T>()
      {
         // get properties list
         var prop = typeof(T).GetProperties();

         // returns the list of properties that contains the Join attribute
         var joins = prop.Where(a => a.GetCustomAttributes(true)
            .FirstOrDefault(b => b.GetType() == typeof(Join)) is Join)
            .ToArray();

         return joins;
      }

      protected string[] GetColumnsFromEntity(Table[] tables, PropertyInfo[] properties)
      {
         // stores the field list
         var cols = new List<string>();

         // read the tables
         foreach (var t in tables)
         {
            // read the fields corresponding to each table
            foreach (var p in properties)
            {
               var f = GetField(p, t.Index);
               if (f != null) cols.Add(string.Format("t{0}.{1}", t.Index, f.FieldName));
            }
         }

         // returns the field list
         return cols.ToArray();
      }

      protected string[] GetJoinsFromEntity(Table[] tables, PropertyInfo[] properties)
      {
         // stores list of join commands
         var joins = new List<string>();

         // read properties
         foreach (var pj in properties)
         {
            // get the existing joins
            var js = pj.GetCustomAttributes(true).Where(a => a.GetType() == typeof(Join))
               .Cast<Join>().OrderBy(a => a.Table1).ThenBy(a => a.Table2).ToList();

            // read the joins list
            foreach (var j in js)
            {
               var f1 = GetField(pj, j.Table1);
               var f2 = GetField(pj, j.Table2);

               // generates the join command
               var temp = string.Format("{0} JOIN {1} AS t{2} ON t{3}.{4} = t{2}.{5}",
                  j.Command, tables[j.Table2].TableName, j.Table2, j.Table1, f1.FieldName, f2.FieldName);

               joins.Add(temp);
            }
         }

         // returns the list of commands
         return joins.ToArray();
      }

      protected string[] GetAliasForColumns(string[] columns)
      {
         // stores the field list
         var alias = new List<string>();

         // groups the columns
         var gs = columns.GroupBy(a => a).ToList();

         // read the groups
         foreach (var g in gs)
         {
            int count = g.Count();  // count the number of columns in each group
            int j = 1;              // start the counter

            // there is duplicity
            if (count > 1)
            {
               // substitutes the same names using index
               foreach (var c in g)
               {
                  alias.Add(string.Format("{0}{1}", c, j));
                  j++;
               }
            }
            // includes current name
            else { alias.Add(g.Key); }
         }

         // returns the field list
         return alias.ToArray();
      }

      protected abstract string[] GetColumnsAndParametersForFilters(IDbCommand command, Field[] fields, string[] columns, string[] alias, string[] operators, object[] values);

      protected string[] GetColumnsForSort(Field[] fields, string[] columns, bool[] descs)
      {
         // stores the fields for sorting
         var sort = new List<string>();

         // creates the sort sequence according to the specified columns
         for (int i = 0; i < columns.Length; i++)
         {
            // get the attributes of the column
            var f = fields.Where(a => a.FieldName.ToLower() == columns[i].ToLower()).ToList()[0];

            // add to temporary list
            sort.Add(string.Format("t{0}.{1}{2}", f.TableIndex, f.FieldName, (descs[i] ? " DESC" : "")));
         }

         // returns the list of fields for sorting
         return sort.ToArray();
      }

      protected virtual T GetEntity<T>(DataRow dr, Field[] fields)
      {
         // datasource is invalid; returns the object's default value
         if (dr == null)
            return default(T);

         // local variables
         var columns = GetPropertiesInfo<T>();

         // creates an instance of the object
         var newObject = Activator.CreateInstance<T>();

         // read the record
         try
         {
            for (int index = 0; index < fields.Length; index++)
            {
               var field = fields[index];
               var info = (PropertyInfo)columns[field.FieldName.ToLower()];
               object valor = dr[field.FieldName];

               if (field.FieldName != "" && info.CanWrite)
               {
                  // get the data type of the properties
                  // convert value and assign
                  var t = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;
                  object safeValue = (valor == DBNull.Value || valor == null) ? null : Convert.ChangeType(valor, t);
                  info.SetValue(newObject, safeValue, null);
               }
            }
         }
         catch (Exception ex)
         {
            throw ex;
         }

         // returns the new record
         return newObject;
      }

      protected virtual T[] GetEntities<T>(DataRow[] dr, Field[] fields)
      {
         // datasource is invalid; returns the object's default value
         if (dr == null)
            return new T[] { };

         // local variables
         var columns = GetPropertiesInfo<T>();

         // stores the return list
         var entities = new List<T>();

         // read the records
         foreach (var item in dr)
         {
            // creates an instance of the object
            var newObject = Activator.CreateInstance<T>();

            try
            {
               for (int index = 0; index < columns.Count; index++)
               {
                  var field = fields[index];
                  var info = (PropertyInfo)columns[field.FieldName.ToLower()];
                  object valor = item[field.FieldName];

                  if (field.FieldName != "" && info.CanWrite)
                  {
                     // get the data type of the properties
                     // convert value and assign
                     var t = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;
                     object safeValue = (valor == DBNull.Value || valor == null) ? null : Convert.ChangeType(valor, t);
                     info.SetValue(newObject, safeValue, null);
                  }
               }
               entities.Add(newObject);
            }
            catch { throw; }
         }

         // returns the list of records
         return entities.ToArray();
      }

      protected object ParseValue(object value)
      {
         return ParseValue(value, "");
      }

      protected object ParseValue(object value, string oper)
      {
         if (value == null) 
            return DBNull.Value;
         else
            return (oper.ToUpper() == "CONTAINS") ? "%" + value + "%" : value;
      }

      private Hashtable GetPropertiesInfo<T>()
      {
         var hashtable = new Hashtable();
         var properties = typeof(T).GetProperties();

         // read the properties of the object
         foreach (var info in properties)
         {
            var field = info.GetCustomAttributes(true).Where(a => a is Field)
               .Cast<Field>()
               .OrderBy(b => b.TableIndex)
               .ToArray();

            if (field != null && field.Length > 0)
               hashtable[field[0].FullName.ToLower()] = info;
         }

         return hashtable;
      }
   }
}