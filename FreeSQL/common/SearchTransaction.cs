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
using System.Data;

namespace FreeSQL.Common
{
   public abstract class SearchTransaction : Transaction
   {
      protected readonly SearchEngine _engine;
      private object[] _result;

      public SearchTransaction(SearchEngine searchEngine, FreeSQLDatabase database)
         : base(database)
      {
         _engine = searchEngine;
         _result = new object[0];
      }

      protected string CommandText { get; set; }

      protected SearchParam[] Parameters { get; set; }

      protected string LogAction { get; set; }

      protected string ExtraInfo { get; set; }

      public virtual object[] Result
      {
         get { return _result; }
      }

      public override void Execute()
      {
         // open connection
         _db.OpenConnection();

         try
         {
            // runs search
            _result = _db.CustomSelect<object>(CommandText, Parameters);

            // closes connection
            _db.CloseConnection();

            // save log
            SaveLog(LogAction, ExtraInfo);
         }
         catch
         {
            _db.CloseConnection();
            _result = new object[0];
         }
      }
   }
}
