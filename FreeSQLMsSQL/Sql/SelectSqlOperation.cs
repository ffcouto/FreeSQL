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
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.MsSQL
{
   internal class SelectSqlOperation<T> : SqlOperation
   {
      // variáveis locais
      private readonly int idObj;
      private T retObj = default(T);

      public SelectSqlOperation(int codObj, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         this.idObj = codObj;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectCommand(idObj);
            var row = LoadDataRowFromCommand(readCommand);
            retObj = GetEntity<T>(row, GetFieldAttributes<T>());
         }
         catch { throw; }
      }
      
      public T ReturnObject
      {
         get { return retObj; }
      }

      private SqlCommand GetSelectCommand(object value)
      {
         // atributos personalizados com permissão para leitura (cRud - Read)
         var tabAttr = GetTableAttributes<T>().Where(a => a.CRUD.HasFlag(CrudOptions.Read)).ToArray();
         var propAttr = GetProperties(Activator.CreateInstance<T>());
         var joinAttr = GetJoinAttributeProperties<T>();

         // armazena os campos das tabelas
         var cols = new List<string>(GetColumnsFromEntity(tabAttr, propAttr));

         // armazena os comandos join existentes
         var joins = new List<string>(GetJoinsFromEntity(tabAttr, joinAttr));

         // obtém a chave primária da tabela principal
         // e faz a leitura da propriedade
         var pk = GetPrimaryKeyProperty<T>(tabAttr[0]);
         var pf = GetField(pk, tabAttr[0].Index);

         // comando de consulta
         string query = "SELECT {0} FROM {1} WHERE {2};";
         string columns = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();
         string filters = (tabAttr[0].VirtualDelete) ? "(t{1}.{0} = @{0}) AND (t{1}.ativo = @ativo)" : "(t{1}.{0} = @{0})";
         string where = string.Format(filters, pf.FieldName, tabAttr[0].Index);

         // cria comando
         var cmd = new SqlCommand();
         cmd.CommandText = string.Format(query, columns, tables, where);
         cmd.Parameters.Add(pf.FieldName, (SqlDbType)pf.DatabaseType).Value = ParseValue(value);
         if (tabAttr[0].VirtualDelete) cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;
         return cmd;
      }
   }
}