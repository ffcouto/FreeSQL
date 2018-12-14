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
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class InsertOneTableOleDbOperation<T> : OleDbOperation
   {
      // local variables
      private readonly T wObj;
      private readonly int wTabIndex;
      private int newID;

      public InsertOneTableOleDbOperation(T obj, int tableIndex, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         wObj = obj;
         wTabIndex = tableIndex;
      }

      public override void Execute()
      {
         try
         {
            // reads the specified table which has permission for insert (Crud - Create)
            var t = GetTableAttributes<T>().FirstOrDefault(a => a.Index == wTabIndex && a.CRUD.HasFlag(CrudOptions.Create));

            if (t == null)
               throw new IndexOutOfRangeException("O índice informado não pertence a nenhuma tabela definida.");

            // reads the property that contains the primary key
            var pk = GetPrimaryKeyProperty<T>(t);
            var key = GetKeyAttribute(pk, t);

            // is a primary field and is not auto-increment; retrieves next table ID
            if (key.IsIdentity && !key.AutoIncrement)
            {
               var idCommand = GetNextIDCommand<T>(t);
               newID = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
               pk.SetValue(wObj, newID, null);
            }

            // is not a primary field and is not auto-increment; returns the property's own value
            else if (!key.IsIdentity && !key.AutoIncrement)
            {
               newID = (int)pk.GetValue(wObj, null);
            }

            // runs insert
            var readCommand = GetInsertCommand<T>(wObj, t);
            ExecuteCommand(readCommand);

            // is an auto-incremente field; retrieve the ID generated by the database
            if (key.AutoIncrement)
            {
               var idCommand = GetLastIDCommand(t);
               newID = Convert.ToInt32(ExecuteCommandAndReturn(idCommand));
               pk.SetValue(wObj, newID, null);
            }
         }
         catch { throw; }
      }

      public int NewID
      {
         get { return newID; }
      }
   }
}