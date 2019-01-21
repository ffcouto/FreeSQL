﻿/*
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
using System.Reflection;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.SqlCe
{
   internal class InsertSqlOperation<T> : SqlCeOperation
   {
      // local variables
      private readonly T wObj;
      private object[] newIDs;

      public InsertSqlOperation(T obj, SqlCeConnection connection, SqlCeTransaction transaction)
         : base(connection, transaction)
      {
         wObj = obj;
      }

      public override void Execute()
      {
         try
         {
            // read the tables with permission for insert (Crud - Create)
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Create)).ToArray();

            // defines the number of returns of the id's of the tables
            newIDs = new object[tables.Length];

            foreach (var t in tables)
            {
               // reads the property that contains the primary key
               var pk = GetPrimaryKeyProperty<T>(t);
               var key = GetKeyAttribute(pk, t);

               // is not an identity field and is auto-incremente; retrieves next table ID
               if (!key.IsIdentity && key.AutoIncrement)
               {
                  var idCommand = GetNextIDCommand<T>(t);
                  newIDs[t.Index] = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
                  pk.SetValue(wObj, newIDs[t.Index], null);
               }

               // is not an identity field and is not auto-incremente; returns the property's own value
               else if (!key.IsIdentity && !key.AutoIncrement)
               {
                  newIDs[t.Index] = pk.GetValue(wObj, null);
               }

               // runs insert
               var readCommand = GetInsertCommand(wObj, t);
               ExecuteCommand(readCommand);

               // is an identity field; retrieve the ID generated by the database
               if (key.IsIdentity)
               {
                  var idCommand = GetLastIDCommand(t);
                  newIDs[t.Index] = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
                  pk.SetValue(wObj, newIDs[t.Index], null);
               }
            }
         }
         catch { throw; }
      }

      public object[] NewIDs
      {
         get { return newIDs; }
      }
   }
}