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
   public abstract class SearchEngine
   {
      protected readonly FreeSQLDatabase _database;
      protected List<SearchField> _fields = new List<SearchField>();
      protected List<SearchParam> _filters = new List<SearchParam>();
      protected readonly Dictionary<string, object> _types;

      public SearchEngine(FreeSQLDatabase database)
      {
         _database = database;
      }

      public virtual SearchField[] Fields
      {
         get { return _fields.ToArray(); }
      }

      public virtual SearchParam[] Filters
      {
         get { return _filters.ToArray(); }
      }

      public ISearchProvider GetCustomType(string name)
      {
         if (_types.ContainsKey(name))
            return (ISearchProvider)_types[name];
         else
            return null;
      }

      public virtual int DefaultFilter { get; set; }

      public virtual int FieldResult { get; set; }

      public virtual string FormTitle { get; set; }

      public virtual SearchParam SelectedFilter { get; set; }

      public virtual SearchTransaction RunFunction { get; set; }

      public virtual void AddField(string name, string text, string type, int width, object alignment, string format)
      {
         _fields.Add(new SearchField(name, text, type, width, alignment, format));
      }

      public virtual void AddField(string name, string alias, string text, string type, int width, object alignment, string format)
      {
         _fields.Add(new SearchField(name, alias, text, type, width, alignment, format));
      }
      
      public abstract void AddFilter(string name, string text, SearchComparison comparison, object dataType);

      public abstract void AddFilter(string name, string text, string param, SearchComparison comparison, object dataType);

      public abstract void AddFilter(string name, string text, SearchComparison comparison, object dataType, bool needCriteria);

      public abstract void AddFilter(string name, string text, string param, SearchComparison comparison, object dataType, bool needCriteria);

      public abstract void AddFilter(string name, string text, SearchComparison comparison, object dataType, object value, bool needCriteria);

      public abstract void AddFilter(string name, string text, string param, SearchComparison comparison, object dataType, object value, bool needCriteria);

      public abstract void AddHiddenFilter(string name, string text, SearchComparison comparison, object dataType, object value);

      public abstract void AddHiddenFilter(string name, string text, string param, SearchComparison comparison, object dataType, object value);

      public abstract void AddHiddenFilter(string name, string text, SearchComparison comparison, object dataType, object value, bool needCriteria);

      public abstract void AddHiddenFilter(string name, string text, string param, SearchComparison comparison, object dataType, object value, bool needCriteria);

      public void AddCustomType(Type newType)
      {
         if (!_types.ContainsKey(newType.Name))
         {
            _types.Add(newType.Name, Activator.CreateInstance(newType));
         }
      }

      public virtual string GetListFields()
      {
         // no field has been added; consider all
         if (_fields.Count == 0)
            return "*";
         
         try
         {
            int ub = _fields.Count;
            string[] list = new string[ub];

            for (int i = 0; i < ub; i++)
               list[i] = string.Format("{0} AS {1}", _fields[i].FieldName, _fields[i].FieldAlias);

            return string.Join(", ", list);
         }
         catch
         {
            return "*";
         }
      }

      public static SearchField CreateField(string name, string text, string type, int width, object alignment, string format)
      {
         return new SearchField(name, text, type, width, alignment, format);
      }

      public static SearchField CreateField(string name, string alias, string text, string type, int width, object alignment, string format)
      {
         return new SearchField(name, alias, text, type, width, alignment, format);
      }
   }
}
