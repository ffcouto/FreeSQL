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
using System.Reflection;
using FreeSQL.Backwork;

namespace FreeSQL
{
   public abstract class Transaction
   {
      // instance for the NULL pattern
      public static Transaction Null = new NullTransaction();

      protected readonly FreeSQLDatabase _db;

      public Transaction(FreeSQLDatabase database)
      {
         _db = database;
      }

      public abstract void Execute();

      protected void SaveLog(string action, string extrainfo)
      {
         // gets user logged in and workstation
         string user = _db.GetUserLogged();
         string wks = _db.GetIpAddress();

         // save log
         _db.CreateLog(DateTime.UtcNow, user, wks, action, extrainfo);
      }

      protected string GetRecordChanges(object current, object old)
      {
         var logInfo = new List<string>();
         var propertyInfos = current.GetType().GetProperties();

         foreach (var p in propertyInfos)
         {
            object v1 = p.GetValue(current, null);
            object v2 = old.GetType().GetProperty(p.Name).GetValue(old, null);

            if (v1 != null)
            {
               var attr = p.GetCustomAttributes(typeof(Ignore), true);

               if (attr == null && !v1.Equals(v2))
                  logInfo.Add(string.Format("COLUMN: [{0}] FROM: [{1}] TO: [{2}]", p.Name, v2, v1));
            }
         }

         return string.Join("\n", logInfo);
      }

      // private class for the NULL pattern designer 
      private class NullTransaction : Transaction
      {
         public NullTransaction()
            : base(null)
         { }

         public override void Execute()
         {
            // nothing to do
         }
      }
   }
}
