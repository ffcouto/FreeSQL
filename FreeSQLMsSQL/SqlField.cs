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
using System.Data;

namespace FreeSQL.Backwork
{
   [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
   public sealed class SqlField : Field
   {
      private readonly int tabIndex;
      private readonly string fldName;
      private readonly SqlDbType dbType;
      private readonly bool visible;

      public SqlField(int tableIndex, string fieldName, SqlDbType dataType)
         : this(tableIndex, fieldName, dataType, true)
      { }

      public SqlField(int tableIndex, string fieldName, SqlDbType dataType, bool visible)
      {
         this.tabIndex = tableIndex;
         this.fldName = fieldName;
         this.dbType = dataType;
         this.visible = visible;
      }

      public override int TableIndex
      {
         get { return this.tabIndex; }
      }

      public override string FieldName
      {
         get { return this.fldName; }
      }

      public override object DatabaseType
      {
         get { return this.dbType; }
      }

      public override bool Visible
      {
         get { return this.visible; }
      }
   }
}