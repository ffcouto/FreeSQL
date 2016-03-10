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
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class SelectTopWhereOleDbOperation<T> : OleDbOperation
   {
      // variáveis locais
      private readonly int wRows;
      private readonly string[] wFilterColumns;
      private readonly string[] wOperators;
      private readonly object[] wValues;
      private readonly string[] wSortColumns;
      private readonly bool[] wDesc;

      private T[] retObjs = new T[] { };

      public SelectTopWhereOleDbOperation(int rows, string fColumn, string comparison, object value, string sColumn, bool desc, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.wRows = rows;
         this.wFilterColumns = new string[] { fColumn };
         this.wOperators = new string[] { comparison };
         this.wValues = new object[] { value };
         this.wSortColumns = new string[] { sColumn };
         this.wDesc = new bool[] { desc };
      }

      public SelectTopWhereOleDbOperation(int rows, string[] fColumns, string[] comparison, object[] values, string[] sColumns, bool[] desc, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.wRows = rows;
         this.wFilterColumns = fColumns;
         this.wOperators = comparison;
         this.wValues = values;
         this.wSortColumns = sColumns;
         this.wDesc = desc;
      }

      public override void Execute()
      {
         try
         {
            var readCommand = GetSelectTopCommand(wRows, wFilterColumns, wOperators, wValues, wSortColumns, wDesc);
            var rows = LoadDataFromCommand(readCommand);
            retObjs = GetEntities<T>(rows, GetFieldAttributes<T>());
         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }

      private OleDbCommand GetSelectTopCommand(int topRows, string[] fColumns, string[] comparison, object[] values, string[] sColumns, bool[] descs)
      {
         // verifica se o número de colunas e valores de pesquisa são iguais
         if ((fColumns.Length != comparison.Length) || (fColumns.Length != values.Length))
            throw new Exception("O número de colunas e valores de pesquisa são inconsistentes.");

         // verifica se o número de colunas e sequência de ordenação são iguais
         if (sColumns.Length != descs.Length)
            throw new Exception("O número de colunas e sequências de ordenação são inconsistentes.");

         // o numero de linhas de retorno deve ser maior que 0
         if (topRows < 1)
            throw new Exception("O número de retorno de linhas deve ser maior que 0 (zero).");

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
         var alias = new List<string>(GetAliasForColumns(fColumns));

         // cria um novo comando
         var cmd = new OleDbCommand();

         // armazena a lista de filtros do comando
         var filter = new List<string>(GetColumnsAndParametersForFilters(cmd, fldAttr, fColumns, alias.ToArray(), comparison, values));

         // quando a tabela possui exclusão virtual
         // deve haver um filtro apenas dos registrso ativos
         if (tabAttr[0].VirtualDelete)
         {
            filter.Add(string.Format("(t{0}.ativo = ?)", tabAttr[0].Index));
            cmd.Parameters.Add("@ativo", OleDbType.Boolean).Value = true;
         }

         // armazena os campos para ordenação
         var sort = new List<string>(GetColumnsForSort(fldAttr, sColumns, descs));

         // Provedor OleDb requer uso de parenteses na expressão de joins
         for (int i = 0; i < joins.Count; i++)
            joins[i] = string.Concat(joins[i], ")");

         string parentesis = string.Join("", ArrayList.Repeat("(", joins.Count).ToArray());

         // comando de consulta
         string query = "SELECT TOP {0} {1} FROM {2} WHERE {3} ORDER BY {4};";
         string fields = string.Join(", ", cols);
         string tables = string.Format("{0} AS t{1} {2}", tabAttr[0].TableName, 0, ((joins.Count == 0) ? "" : string.Join(" ", joins))).Trim();
         string where = string.Join(" AND ", filter);
         string order = string.Join(", ", sort);

         // cria comando
         cmd.CommandText = string.Format(query, topRows, fields, tables, where, order);
         return cmd;
      }
   }
}