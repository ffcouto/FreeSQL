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
using System.ComponentModel;

namespace FreeSQL.Common
{
   public enum SearchComparison : int
   {
      [Description("=")]
      Equal = 1,
      [Description("<>")]
      NotEqual = 2,
      [Description(">")]
      GreaterThan = 3,
      [Description(">=")]
      GreaterThanOrEqual = 4,
      [Description("<")]
      LessThan = 5,
      [Description("<=")]
      LessThanOrEqual = 6,
      [Description("LIKE")]
      Like = 7,
      [Description("IN")]
      OneOf = 8,
      [Description("!IN")]
      NotOneOf = 9,
      [Description("NULL")]
      IsNull = 10,
      [Description("!NULL")]
      NotIsNull = 11
   }

   public interface SearchParam
   {
      string FieldName { get; }

      string FieldText { get; }

      SearchComparison Comparison { get; }

      object DataType { get; }

      object Value { get; set; }

      object ParseValue { get; }

      string FormattedExpression { get; }

      string ToString();
   }
}
