/*
FreeSQL
Copyright (C) 2016 Fabiano Couto

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Suite 500, Boston, MA 02110-1335, USA.
*/

using System;
using System.Collections.Generic;

namespace FreeSQL.Common
{
   public abstract class SearchEngine
   {
      protected readonly FreeSQLDatabase database;
      protected List<SearchField> fields = new List<SearchField>();
      protected List<SearchParam> filters = new List<SearchParam>();

      public SearchEngine(FreeSQLDatabase database)
      {
         this.database = database;
      }

      public virtual SearchField[] Fields
      {
         get { return fields.ToArray(); }
      }

      public virtual SearchParam[] Filters
      {
         get { return filters.ToArray(); }
      }

      public virtual int DefaultFilter { get; set; }

      public virtual int FieldResult { get; set; }

      public virtual string FormTitle { get; set; }

      public virtual SearchParam SelectedFilter { get; set; }

      public virtual SearchTransaction RunFunction { get; set; }

      public virtual void AddField(string name, string text, string type, int width, object alignment, string format)
      {
         // cria o novo campo e adiciona na lista
         fields.Add(new SearchField(name, text, type, width, alignment, format));
      }

      public virtual void AddField(string name, string alias, string text, string type, int width, object alignment, string format)
      {
         // cria o novo campo e adiciona na lista
         fields.Add(new SearchField(name, alias, text, type, width, alignment, format));
      }
      
      public abstract void AddFilter(string name, string text, SearchComparison comparison, object dataType);

      public virtual string GetListFields()
      {
         // nenhum campo foi adicionado; considera todos
         if (fields.Count == 0)
            return "*";
         
         try
         {
            int ub = fields.Count;
            string[] list = new string[ub];

            for (int i = 0; i < ub; i++)
               list[i] = string.Format("{0} AS {1}", fields[i].FieldName, fields[i].FieldAlias);

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
