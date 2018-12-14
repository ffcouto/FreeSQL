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

namespace FreeSQL.Common
{
   public class SearchField
   {
      public SearchField(string fieldName, string text, string type, int width, object alignment, string format)
         : this(fieldName, fieldName, text, type, width, alignment, format)
      { }

      public SearchField(string fieldName, string fieldAlias, string text, string type, int width, object alignment, string format)
      {
         FieldName = fieldName;
         FieldAlias = fieldAlias;
         Text = text;
         Type = type;
         Width = width;
         Alignment = alignment;
         Format = format;
      }

      public string FieldName { get; }

      public string FieldAlias { get; }

      public string Text { get; }

      public string Type { get; }

      public int Width { get; }

      public object Alignment { get; }

      public string Format { get; }

      public override string ToString()
      {
         return string.Format("{0} [{1}]", FieldName, Text);
      }
   }
}
