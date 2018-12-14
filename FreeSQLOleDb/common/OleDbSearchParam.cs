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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;

namespace FreeSQL.Common
{
   public class OleDbSearchParam : SearchParam
   {
      public OleDbSearchParam(string fieldName, string fieldText, string paramName, SearchComparison comparison, object dataType, bool needCriteria)
      {
         FieldName = fieldName;
         FieldText = fieldText;
         ParamName = paramName;
         Comparison = comparison;
         DataType = dataType;
         NoCriteria = !needCriteria;
         Hidden = false;
      }

      public string FieldName { get; }

      public string FieldText { get; }

      public string ParamName { get; }

      public SearchComparison Comparison { get; }

      public object DataType { get; }

      public bool NoCriteria { get; }

      public bool Hidden { get; internal set; }

      public object Value { get; set; }

      public virtual object ParseValue
      {
         get
         {
            if (Value.GetType() == typeof(string))
               return (Comparison == SearchComparison.Like) ? "%" + Value.ToString().Replace("*", "%") + "%" : Value;
            else
               return (Comparison == SearchComparison.Like) ? "%" + Value + "%" : Value;
         }
      }

      public string FormattedExpression
      {
         get
         {
            // there is no comparison
            if (Comparison == SearchComparison.None) return string.Empty;

            // the criteria is a list
            if (Comparison == SearchComparison.OneOf || Comparison == SearchComparison.NotOneOf)
            {
               string[] optList = this.ParseValue.ToString().Split(',');
               var inList = ArrayList.Repeat("?", optList.Length);
               return string.Format("({0} {1} ({2}))", FieldName, Comparison.GetDescription().Replace("!", "NOT "), string.Join(", ", inList));
            }

            // the criteria is nullable
            else if (Comparison == SearchComparison.IsNull || Comparison == SearchComparison.NotIsNull)
            {
               bool notOperator = (Comparison == SearchComparison.NotIsNull);
               return string.Format("({1}{0} IS NULL)", FieldName, (notOperator ? "NOT " : ""));
            }

            // others cases
            else
            {
               return string.Format("({0} {1} ?)", FieldName, Comparison.GetDescription());
            }
         }
      }

      public override string ToString()
      {
         return FieldText;
      }
   }
}
