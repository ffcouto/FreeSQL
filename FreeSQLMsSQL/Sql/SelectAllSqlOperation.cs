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
   internal class SelectAllSqlOperation<T> : SqlOperation
   {
      // variáveis locais
      private readonly string orderField;
      private T[] retObjs = new T[] { };

      public SelectAllSqlOperation(string orderField, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         this.orderField = orderField;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectAllCommand(orderField);
            var rows = LoadDataFromCommand(readCommand);
            retObjs = GetEntities<T>(rows, GetFieldAttributes<T>());
         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }

      private SqlCommand GetSelectAllCommand(string sortField)
      {
         // atributos personalizados com permissão para leitura (cRud - Read)
         var tabAttr = GetTableAttributes<T>().Where(a => a.CRUD.HasFlag(CrudOptions.Read)).ToArray();
         var propAttr = GetProperties(Activator.CreateInstance<T>());
         var joinAttr = GetJoinAttributeProperties<T>();

         // armazena os campos das tabelas
         var cols = new List<string>(GetColumnsFromEntity(tabAttr, propAttr));

         // armazena os comandos join existentes
         var joins = new List<string>(GetJoinsFromEntity(tabAttr, joinAttr));

         // comando de consulta
         string query = "SELECT {0} FROM {1} ORDER BY {2};";

         // quando a tabela possui exclusão virtual
         // deve haver um filtro apenas dos registrso ativos
         var tab = tabAttr.FirstOrDefault(a => a.VirtualDelete == true);

         if (tab != null)
            query = "SELECT {0} FROM {1} WHERE (t0.ativo = @ativo) ORDER BY {2};";

         string columns = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();

         // cria o comando
         var cmd = new SqlCommand();
         cmd.CommandText = string.Format(query, columns, tables, sortField);

         // define o parâmetro do filtro
         if (tab != null)
            cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;

         return cmd;
      }
   }
}