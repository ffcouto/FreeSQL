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

namespace FreeSQL.Common
{
   public class SqlSearchEngine : SearchEngine
   {
      public SqlSearchEngine(FreeSQLDatabase database)
         : base(database)
      { }

      public override void AddFilter(string name, string text, SearchComparison comparison, object dataType)
      {
         _filters.Add(new SqlSearchParam(name, text, name, comparison, dataType, true));
      }

      public override void AddFilter(string name, string text, string param, SearchComparison comparison, object dataType)
      {
         _filters.Add(new SqlSearchParam(name, text, param, comparison, dataType, true));
      }

      public override void AddFilter(string name, string text, SearchComparison comparison, object dataType, bool needCriteria)
      {
         _filters.Add(new SqlSearchParam(name, text, name, comparison, dataType, needCriteria));
      }

      public override void AddFilter(string name, string text, string param, SearchComparison comparison, object dataType, bool needCriteria)
      {
         _filters.Add(new SqlSearchParam(name, text, param, comparison, dataType, needCriteria));
      }

      public override void AddFilter(string name, string text, SearchComparison comparison, object dataType, object value, bool needCriteria)
      {
         _filters.Add(new SqlSearchParam(name, text, name, comparison, dataType, needCriteria) { Value = value });
      }

      public override void AddFilter(string name, string text, string param, SearchComparison comparison, object dataType, object value, bool needCriteria)
      {
         _filters.Add(new SqlSearchParam(name, text, param, comparison, dataType, needCriteria) { Value = value });
      }

      public override void AddHiddenFilter(string name, string text, SearchComparison comparison, object dataType, object value)
      {
         _filters.Add(new SqlSearchParam(name, text, name, comparison, dataType, true) { Hidden = true, Value = value });
      }

      public override void AddHiddenFilter(string name, string text, string param, SearchComparison comparison, object dataType, object value)
      {
         _filters.Add(new SqlSearchParam(name, text, param, comparison, dataType, true) { Hidden = true, Value = value });
      }

      public override void AddHiddenFilter(string name, string text, SearchComparison comparison, object dataType, object value, bool needCriteria)
      {
         _filters.Add(new SqlSearchParam(name, text, name, comparison, dataType, needCriteria) { Hidden = true, Value = value });
      }

      public override void AddHiddenFilter(string name, string text, string param, SearchComparison comparison, object dataType, object value, bool needCriteria)
      {
         _filters.Add(new SqlSearchParam(name, text, param, comparison, dataType, needCriteria) { Hidden = true, Value = value });
      }

      public static SearchParam NullParam()
      {
         return new SqlSearchParam("", "", "", SearchComparison.None, null, false);
      }

      public static SearchParam CreateParam(string name, string text, string param, SearchComparison comparison, object dataType)
      {
         return new SqlSearchParam(name, text, param, comparison, dataType, true);
      }

      public static SearchParam CreateParam(string name, string text, string param, SearchComparison comparison, object dataType, object value)
      {
         return new SqlSearchParam(name, text, param, comparison, dataType, true) { Value = value };
      }

      public static SearchParam CreateParam(string name, string text, string param, SearchComparison comparison, object dataType, bool needCriteria)
      {
         return new SqlSearchParam(name, text, param, comparison, dataType, needCriteria);
      }

      public static SearchParam CreateParam(string name, string text, string param, SearchComparison comparison, object dataType, bool needCriteria, object value)
      {
         return new SqlSearchParam(name, text, param, comparison, dataType, needCriteria) { Value = value };
      }
   }
}
