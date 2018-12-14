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

namespace FreeSQL.Backwork
{
   [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = false)]
   public sealed class SqlField : Field
   {
      public SqlField(int tableIndex, string fieldName, SqlDbType dataType)
         : this(tableIndex, fieldName, dataType, true)
      { }

      public SqlField(int tableIndex, string fieldName, SqlDbType dataType, bool visible)
      {
         TableIndex = tableIndex;
         FieldName = fieldName;
         DatabaseType = dataType;
         Visible = visible;
      }

      public override int TableIndex { get; }

      public override string FieldName { get; }

      public override object DatabaseType { get; }

      public override bool Visible { get; }

      public override string FullName
      {
         get { return string.Format("t{0}_{1}", TableIndex, FieldName); }
      }
   }
}