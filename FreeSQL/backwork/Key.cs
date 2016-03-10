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

namespace FreeSQL.Backwork
{
   [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
   public sealed class Key : Attribute
   {
      public Key(bool pk, int table)
         : this(pk, true, table)
      { }

      public Key(bool pk, bool autoIncrement, int table)
      {
         this.IsPrimary = pk;
         this.AutoIncrement = autoIncrement;
         this.TableIndex = table;
      }

      public bool IsPrimary { get; private set; }

      public bool AutoIncrement { get; private set; }

      public int TableIndex { get; private set; }
   }
}