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
   public class SqlSearchParam : SearchParam
   {
      public SqlSearchParam(string fieldName, string fieldText, SearchComparison comparison, object dataType)
      {
         this.FieldName = fieldName;
         this.FieldText = fieldText;
         this.Comparison = comparison;
         this.DataType = dataType;
      }

      public string FieldName { get; private set; }

      public string FieldText { get; private set; }

      public SearchComparison Comparison { get; private set; }

      public object DataType { get; private set; }

      public object Value { get; set; }

      public virtual object ParseValue
      {
         get
         {
            if (this.Value.GetType() == typeof(string))
               return (Comparison == SearchComparison.Like) ? "%" + this.Value.ToString().Replace("*", "%") + "%" : this.Value;
            else
               return (Comparison == SearchComparison.Like) ? "%" + this.Value + "%" : this.Value;
         }
      }

      public string FormattedExpression
      {
         get
         {
            if (this.Comparison == SearchComparison.OneOf || this.Comparison == SearchComparison.NotOneOf)
            {
               string[] optList = this.ParseValue.ToString().Split(',');
               var inList = new List<string>();

               for (int i = 0; i < optList.Length; i++)
                  inList.Add(string.Format("@{0}{1}", this.FieldName.Replace(".", "_"), i));
               
               return string.Format("({0} {1} ({2}))", this.FieldName, this.Comparison.GetDescription().Replace("!", "NOT "), string.Join(", ", inList));
            }
            else if (this.Comparison == SearchComparison.IsNull)
            {
               return string.Format("({0} IS NULL)", this.FieldName);
            }
            else if (this.Comparison == SearchComparison.NotIsNull)
            {
               return string.Format("(NOT {0} IS NULL)", this.FieldName);
            }
            else
            {
               return string.Format("({0} {1} @{2})", this.FieldName, this.Comparison.GetDescription(), this.FieldName.Replace(".", "_"));
            }
         }
      }

      public override string ToString()
      {
         return this.FieldText;
      }
   }
}
