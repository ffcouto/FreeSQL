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
   [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
   public sealed class Ignore : Attribute
   {
      // This class is used to ignore a property decorated with it,
      // so that the engine does not use it in generating the SQL command
   }
}
