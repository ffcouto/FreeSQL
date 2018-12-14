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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace Teste
{
   // index => table sequence index
   // tableName => table name in the database
   // virtualDelete => sets if the record will have their physical exclusion or a field (ativo) to mark as deleted
   [Table(0, "clientes", true)]
   class Customer
   {
      public Customer()
      {
         Active = true;
      }

      // OleDbField attribute sets the table field
      // tableIndex => identify which table the field belongs
      // fieldName => table field name
      // dataType => field data type
      // visible => sets if to appear in the SQL command (default true)

      // Key attribute sets the primary key of the table
      // pk => sets if the table field is the primary key or not
      // autoIncrement => sets if the field is auto-increment or not
      // table => identifying which table the key belongs

      [OleDbField(0, "codigo", System.Data.OleDb.OleDbType.Integer)]
      [Key(true, false, 0)]
      public int ID { get; set; }

      [OleDbField(0, "nome", System.Data.OleDb.OleDbType.VarChar)]
      public string Name { get; set; }

      [OleDbField(0, "data_nascimento", System.Data.OleDb.OleDbType.Date)]
      public DateTime BirthDate { get; set; }

      // the 'ativo' field is not required when the virtualDelete option of Table
      // attribute is true, however, if you want to know the status, is recommended use it
      [OleDbField(0, "ativo", System.Data.OleDb.OleDbType.Boolean)]
      public bool Active { get; set; }
   }
}
