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
using System.Reflection;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class UpdateOleDbOperation<T> : OleDbOperation
   {
      // local variables
      private readonly T obj;

      public UpdateOleDbOperation(T obj, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.obj = obj;
      }

      public override void Execute()
      {
         try
         {
            // read tables with permission to update (crUd - Update)
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Update)).ToArray();

            foreach (var t in tables)
            {
               var updCommand = GetUpdateCommand(obj, t);
               ExecuteCommand(updCommand);
            }
         }
         catch { throw; }
      }

      private OleDbCommand GetUpdateCommand(T obj, Table t)
      {
         // allowed to update (crUd - UPDATE)?
         if (!t.CRUD.HasFlag(CrudOptions.Update))
            throw new Exception(string.Format("A tabela {0} não possui permissão para atualização de registros.", t.TableName));

         // custom attributes
         var prop = GetProperties(obj);

         // gets the primary key and field data
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);

         // local variables
         var fList = new List<string>();
         var vList = new List<string>();

         // creates the command
         var cmd = new OleDbCommand();

         // read properties
         foreach (PropertyInfo item in prop)
         {
            if (item.GetCustomAttributes(true).ToList().Count > 0)
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
                  fList.Add(string.Format("{0} = ?", f.FieldName));

                  // includes the parameter to the command
                  cmd.Parameters.Add(f.FieldName, (OleDbType)f.DatabaseType).Value = ParseValue(value);
               }
            }
         }

         // includes the where clause
         cmd.Parameters.Add("@id_table", (OleDbType)pf.DatabaseType).Value = ParseValue(pk.GetValue(obj, null));

         // convert lists
         string fields = string.Join(", ", fList);
         string where = string.Format("{0} = ?", pf.FieldName);

         // sets the command and assign it
         string query = "UPDATE {0} SET {1} WHERE ({2});";
         cmd.CommandText = string.Format(query, t.TableName, fields, where);

         // return command
         return cmd;
      }
   }
}