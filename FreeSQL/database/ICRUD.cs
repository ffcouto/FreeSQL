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
using FreeSQL.Common;

namespace FreeSQL.Database
{
   public interface ICRUD
   {
      object[] Insert<T>(T obj);
      int Insert<T>(T obj, int table);
      
      void Update<T>(T obj);
      void UpdateSpecial<T>(T obj, string column);

      void Delete<T>(T obj);
      void Delete<T>(T obj, bool ignoreVirtual);
      void DeleteSpecial<T>(string column, object value);
      void DeleteSpecial<T>(string column, object value, bool ignoreVirtual);
      void DeleteSpecial<T>(string[] columns, object[] values);
      void DeleteSpecial<T>(string[] columns, object[] values, bool ignoreVirtual);

      T Select<T>(int codObj);
      T[] SelectAll<T>(string order);
      
      T[] SelectSpecial<T>(string column, object value);
      T[] SelectSpecial<T>(string column, string comparison, object value);
      T[] SelectSpecial<T>(string[] columns, object[] values);
      T[] SelectSpecial<T>(string[] columns, string[] comparison, object[] values);
      
      T[] SelectTop<T>(int rows, string column, bool desc);
      T[] SelectTop<T>(int rows, string fColumn, object value, string sColumn, bool desc);
      T[] SelectTop<T>(int rows, string fColumn, string comparison, object value, string sColumn, bool desc);
      
      T[] SelectTop<T>(int rows, string[] columns, bool[] descs);
      T[] SelectTop<T>(int rows, string[] fColumns, object[] values, string[] sColumns, bool[] descs);
      T[] SelectTop<T>(int rows, string[] fColumns, string[] comparison, object[] values, string[] sColumns, bool[] descs);

      T[] CustomSelect<T>(string commandText, SearchParam[] parameters);

      T SelectValue<T>(string commandText, SearchParam[] parameters);
   }
}