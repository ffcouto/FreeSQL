﻿/*
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

namespace FreeSQL.Backwork
{
   [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
   public sealed class Key : Attribute
   {
      public Key(bool isIdentity, int table)
         : this(isIdentity, true, table)
      { }

      public Key(bool isIdentity, bool autoIncrement, int table)
      {
         IsIdentity = isIdentity;
         AutoIncrement = autoIncrement;
         TableIndex = table;
      }

      public bool IsIdentity { get; }

      public bool AutoIncrement { get; }

      public int TableIndex { get; }
   }
}