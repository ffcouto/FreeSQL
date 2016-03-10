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

namespace FreeSQL.Common
{
   public class SearchField
   {
      internal SearchField(string fieldName, string text, string type, int width, object alignment, string format)
         : this(fieldName, fieldName, text, type, width, alignment, format)
      { }

      internal SearchField(string fieldName, string fieldAlias, string text, string type, int width, object alignment, string format)
      {
         this.FieldName = fieldName;
         this.FieldAlias = fieldAlias;
         this.Text = text;
         this.Type = type;
         this.Width = width;
         this.Alignment = alignment;
         this.Format = format;
      }

      public string FieldName { get; private set; }

      public string FieldAlias { get; private set; }

      public string Text { get; private set; }

      public string Type { get; private set; }

      public int Width { get; private set; }

      public object Alignment { get; private set; }

      public string Format { get; private set; }

      public override string ToString()
      {
         return string.Format("{0} [{1}]", this.FieldName, this.Text);
      }
   }
}
