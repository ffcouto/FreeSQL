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
   public class SqlSearchEngine : SearchEngine
   {
      public SqlSearchEngine(FreeSQLDatabase database)
         : base(database)
      { }

      public override void AddFilter(string name, string text, SearchComparison comparison, object dataType)
      {
         // cria e adiciona o novo filtro na lista
         var p = new SqlSearchParam(name, text, comparison, dataType);
         filters.Add(p);
      }

      public static SearchParam CreateParam(string name, string text, SearchComparison comparison, object dataType)
      {
         return new SqlSearchParam(name, text, comparison, dataType);
      }
      
      public static SearchParam CreateParam(string name, string text, SearchComparison comparison, object dataType, object value)
      {
         return new SqlSearchParam(name, text, comparison, dataType) { Value = value };
      }
   }
}
