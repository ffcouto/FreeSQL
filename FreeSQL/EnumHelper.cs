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
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;

namespace FreeSQL
{
   public static class EnumHelper
   {
      public static string GetDescription(this Enum Enumeration)
      {
         string value = Enumeration.ToString();
         Type enumType = Enumeration.GetType();
         var descAttribute = (DescriptionAttribute[])enumType
             .GetField(value)
             .GetCustomAttributes(typeof(DescriptionAttribute), false);

         return descAttribute.Length > 0 ? descAttribute[0].Description : value;
      }

      public static bool IsDefined(this Enum Enumeration)
      {
         return Enum.IsDefined(Enumeration.GetType(), Enumeration);
      }

      public static T[] ToArray<T>() where T : struct
      {
         return Enum.GetValues(typeof(T)).Cast<T>().ToArray();
      }

      public static Dictionary<int, string> ToDictionary(this Type enumType)
      {
         return Enum.GetValues(enumType)
            .Cast<object>()
            .ToDictionary(k => (int)k, v => ((Enum)v).GetDescription());
      }
   }
}