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
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using FreeSQL;
using FreeSQL.Backwork;
using FreeSQL.Database;

namespace FreeSQL.Database.MsSQL
{
   internal abstract class SqlOperation : Operation
   {
      public SqlOperation(SqlConnection connection)
         : this(connection, null)
      { }

      public SqlOperation(SqlConnection connection, SqlTransaction transaction)
         : base(connection, transaction)
      { }

      protected override DataRow[] LoadDataFromCommand(IDbCommand command)
      {
         command.Connection = (SqlConnection)conn;
         command.Transaction = (SqlTransaction)trans;
         var adapter = new SqlDataAdapter((SqlCommand)command);
         var dataset = new DataSet();
         adapter.Fill(dataset);
         var table = dataset.Tables["table"];
         return table.Select();
      }

      protected override DataRow LoadDataRowFromCommand(IDbCommand command)
      {
         command.Connection = (SqlConnection)conn;
         command.Transaction = (SqlTransaction)trans;
         var adapter = new SqlDataAdapter((SqlCommand)command);
         var dataset = new DataSet();
         adapter.Fill(dataset);
         var table = dataset.Tables["table"];

         if (table.Rows.Count == 0)
            return null;

         return table.Rows[0];
      }

      protected override string[] GetColumnsAndParametersForFilters(IDbCommand command, Field[] fields, string[] columns, string[] alias, string[] operators, object[] values)
      {
         // armazena os filtros
         var filter = new List<string>();

         // cria os filtros da cláusula where conforme colunas e valores especificados
         for (int i = 0; i < columns.Length; i++)
         {
            // obtém os atributos da coluna
            var pf = (SqlField)fields.Where(a => a.FieldName.ToLower() == columns[i].ToLower()).ToList()[0];

            // casos especiais da clásula where
            if (operators[i].ToUpper() == "IS" || operators[i].ToUpper() == "!IS")
            {
               // adiciona na lista temporária
               filter.Add(string.Format("({4}t{0}.{1} {3} {2})", pf.TableIndex, pf.FieldName, values[i],
                  operators[i], (operators[i].StartsWith("!") ? "NOT " : "")));
            }

            // caso especial IN / NOT IN; é necessário informar para item da lista um parâmetro
            else if (operators[i].ToUpper() == "IN" || operators[i].ToUpper() == "!IN")
            {
               // converte o conteúdo de values[i] em um tipo Enumarable
               var val = values[i] as IEnumerable;

               // o parametro passado não é uma matriz
               if (val == null)
                  throw new Exception("Valor do parâmetro para o operador IN não é uma matriz de objetos válida.");

               // obtém a matriz a partir do valor
               var o = val.Cast<object>().ToArray();

               // lista de campos para cada valor da matriz
               var inFlds = new List<string>();
               for (int p = 0; p < o.Length; p++)
                  inFlds.Add(string.Format("@{0}_{1}", alias[i], p));

               // adiciona na lista temporária
               filter.Add(string.Format("({3}t{0}.{1} IN ({2}))", pf.TableIndex, pf.FieldName,
                  string.Join(", ", inFlds), (operators[i].StartsWith("!") ? "NOT " : "")));

               // adiciona o parâmetro para cada valor da matriz
               for (int p = 0; p < o.Length; p++)
                  ((SqlCommand)command).Parameters.Add(inFlds[p], (SqlDbType)pf.DatabaseType).Value = ParseValue(o[p]);
            }

            // outros casos
            else
            {
               // adiciona na lista temporária
               filter.Add(string.Format("(t{0}.{1} {3} @{2})", pf.TableIndex, pf.FieldName, alias[i], operators[i]));
               // adiciona o parâmetro correspondente a coluna
               ((SqlCommand)command).Parameters.Add(string.Format("@{0}", alias[i]), (SqlDbType)pf.DatabaseType).Value = ParseValue(values[i], operators[i]);
            }
         }

         // retorna a lista de filtros
         return filter.ToArray();
      }

      protected override Field[] GetFieldAttributes<T>()
      {
         var fields = new List<Field>();
         var properties = typeof(T).GetProperties();

         // faz a leitura das propriedades do objeto
         foreach (var info in properties)
         {
            var field = info.GetCustomAttributes(true).Where(a => a.GetType() == typeof(SqlField))
               .Cast<SqlField>()
               .OrderBy(b => b.TableIndex)
               .ToArray();

            if (field != null && field.Length > 0)
               fields.Add(field[0]);
         }

         return fields.ToArray();
      }

      protected override Field GetField(PropertyInfo property, int tableIndex)
      {
         return (SqlField)property.GetCustomAttributes(true)
            .FirstOrDefault(a => a.GetType() == typeof(SqlField) && ((SqlField)a).TableIndex == tableIndex);
      }

      internal protected SqlCommand GetInsertCommand<T>(T obj, Table t)
      {
         // possui permissão para inclusão (Crud - CREATE)?
         if (!t.CRUD.HasFlag(CrudOptions.Create))
            throw new Exception(string.Format("A tabela {0} não possui permissão para inclusão de registros.", t.TableName));

         // faz a leitura dos atributos personalizados
         var prop = GetProperties(obj);

         // variaveis locais
         var fList = new List<string>();
         var vList = new List<string>();

         // cria o comando
         var cmd = new SqlCommand();

         // faz a leituras das propriedades
         foreach (PropertyInfo item in prop)
         {
            if (item.GetCustomAttributes(true).GetLength(0) > 0)
            {
               // obtém os atributos associados ao campo
               var f = GetField(item, t.Index);
               var k = GetKeyAttribute(item, t);

               // é o campo chave/identidade?
               bool keyId = (k != null && k.IsPrimary);

               if (!keyId && f != null && f.FieldName != "" && f.TableIndex == t.Index)
               {
                  // obtém o valor da propriedade da entidade
                  var value = item.GetValue(obj, null);

                  // preenche a lista de campos e valores
                  fList.Add(f.FieldName);
                  vList.Add(string.Format("@{0}", f.FieldName));

                  // inclui um parametro ao comando
                  cmd.Parameters.Add(f.FieldName, (SqlDbType)f.DatabaseType).Value = ParseValue(value);
               }
            }
         }

         // adiciona o campo marcador de exclusão virtual
         if (t.VirtualDelete)
         {
            // verifica se o campo "Ativo" já está na lista;
            // isso evita duplicação do campo e do parâmetro e
            // consequentemente, erro na instrução
            if (!fList.Contains("ativo"))
            {
               fList.Add("ativo");
               vList.Add("@ativo");
               cmd.Parameters.Add("@ativo", SqlDbType.Bit).Value = true;
            }
         }

         // converte as listas
         string fields = string.Join(", ", fList);
         string values = string.Join(", ", vList);

         // define o comando e o atribui
         string query = "INSERT INTO {0} ({1}) VALUES ({2});";
         cmd.CommandText = string.Format(query, t.TableName, fields, values);

         // retorna o comando
         return cmd;
      }

      internal protected SqlCommand GetNextIDCommand<T>(Table t)
      {
         // obtém a chave primária e os dados do campo
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);

         // comando de consulta
         string query = "SELECT ISNULL(MAX({0}), 0) + 1 AS next_id FROM {1};";

         // cria o comando
         var cmd = new SqlCommand();
         cmd.CommandText = string.Format(query, pf.FieldName, t.TableName);
         return cmd;
      }

      internal protected SqlCommand GetLastIDCommand(Table t)
      {
         string query = string.Format("SELECT ident_current('{0}') AS last_id;", t.TableName);
         var cmd = new SqlCommand(query);
         return cmd;
      }
   }
}