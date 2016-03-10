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
   internal class SelectSpecialSqlOperation<T> : SqlOperation
   {
      // variáveis locais
      private readonly string[] wColumns;
      private readonly string[] wOperators;
      private readonly object[] wValues;

      private T[] retObjs = new T[] { };

      public SelectSpecialSqlOperation(string column, string comparison, object value, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         this.wColumns = new string[] { column };
         this.wOperators = new string[] { comparison };
         this.wValues = new object[] { value };
      }

      public SelectSpecialSqlOperation(string[] columns, string[] comparison, object[] values, SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      {
         this.wColumns = columns;
         this.wOperators = comparison;
         this.wValues = values;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectSpecialCommand(wColumns, wOperators, wValues);
            var rows = LoadDataFromCommand(readCommand);
            retObjs = GetEntities<T>(rows, GetFieldAttributes<T>());
         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }

      private SqlCommand GetSelectSpecialCommand(string[] columns, string[] comparison, object[] values)
      {
         // verifica se o número de colunas e valores são iguais
         if ((columns.Length != comparison.Length) || (columns.Length != values.Length))
            throw new Exception("O número de colunas e valores são inconsistentes.");

         // atributos personalizados com permissão para leitura (cRud - Read)
         var tabAttr = GetTableAttributes<T>().Where(a => a.CRUD.HasFlag(CrudOptions.Read)).ToArray();
         var propAttr = GetProperties(Activator.CreateInstance<T>());
         var joinAttr = GetJoinAttributeProperties<T>();
         var fldAttr = GetFieldAttributes<T>();

         // armazena os campos das tabelas
         var cols = new List<string>(GetColumnsFromEntity(tabAttr, propAttr));

         // armazena os comandos join existentes
         var joins = new List<string>(GetJoinsFromEntity(tabAttr, joinAttr));

         // armazena os campos substituindo por um alias para evitar duplicidade
         var alias = new List<string>(GetAliasForColumns(columns));

         // cria um novo comando
         var cmd = new SqlCommand();

         // armazena a lista de filtros do comando
         var filter = new List<string>(GetColumnsAndParametersForFilters(cmd, fldAttr, columns, alias.ToArray(), comparison, values));

         // quando a tabela possui exclusão virtual
         // deve haver um filtro apenas dos registrso ativos
         if (tabAttr[0].VirtualDelete)
         {
            filter.Add(string.Format("(t{0}.ativo = @ativo)", tabAttr[0].Index));
            cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;
         }

         // comando de consulta
         string query = "SELECT {0} FROM {1} WHERE {2};";
         string fields = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();
         string where = string.Join(" AND ", filter);

         // define o comando a ser executado
         cmd.CommandText = string.Format(query, fields, tables, where);
         return cmd;
      }
   }
}