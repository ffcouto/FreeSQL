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
   public enum CrudOptions : int
   {
      Create = 1,
      Read = 2,
      Update = 4,
      Delete = 8,
      All = 15
   }

   [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
   public sealed class Table : Attribute
   {
      public Table(int index, string tableName)
         : this(index, tableName, false, CrudOptions.All)
      { }

      public Table(int index, string tableName, bool virtualDelete)
         : this(index, tableName, virtualDelete, false, CrudOptions.All)
      { }

      public Table(int index, string tableName, CrudOptions crud)
         : this(index, tableName, false, crud)
      { }

      public Table(int index, string tableName, bool virtualDelete, CrudOptions crud)
         : this(index, tableName, virtualDelete, false, crud)
      { }

      public Table(int index, string tableName, bool virtualDelete, bool relationship)
         : this(index, tableName, virtualDelete, relationship, CrudOptions.All)
      { }

      public Table(int index, string tableName, bool virtualDelete, bool relationship, CrudOptions crud)
      {
         this.Index = index;
         this.TableName = tableName;
         this.VirtualDelete = virtualDelete;
         this.Relationship = relationship;
         this.CRUD = crud;
      }

      public int Index { get; private set; }

      public string TableName { get; private set; }

      public bool VirtualDelete { get; private set; }

      public bool Relationship { get; private set; }

      public CrudOptions CRUD { get; private set; }
   }
}