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
using System.Linq;
using System.Reflection;
using System.Text;
using FreeSQL;
using FreeSQL.Backwork;

namespace FreeSQL.Database
{
   public abstract class Operation
   {
      protected readonly IDbConnection conn;
      protected readonly IDbTransaction trans;

      public Operation(IDbConnection connection)
         : this(connection, null)
      { }

      public Operation(IDbConnection connection, IDbTransaction transaction)
      {
         this.conn = connection;
         this.trans = transaction;
      }

      public abstract void Execute();

      protected abstract DataRow[] LoadDataFromCommand(IDbCommand command);

      protected abstract DataRow LoadDataRowFromCommand(IDbCommand command);

      protected int ExecuteCommand(IDbCommand command)
      {
         int recordsAffected = 0;

         if (command != null)
         {
            command.Connection = conn;
            command.Transaction = trans;
            recordsAffected = command.ExecuteNonQuery();
         }

         return recordsAffected;
      }

      protected object ExecuteCommandAndReturn(IDbCommand command)
      {
         if (command == null)
            return 0;

         command.Connection = conn;
         command.Transaction = trans;
         var ret = command.ExecuteScalar();

         if (ret == null || ret == DBNull.Value)
            return null;
         else
            return ret;
      }

      protected virtual Table[] GetTableAttributes<T>()
      {
         var tables = typeof(T).GetCustomAttributes(true)
            .Where(a => a.GetType() == typeof(Table))
            .Cast<Table>()
            .OrderBy(b => b.Index)
            .ToArray();

         return tables;
      }

      protected abstract Field[] GetFieldAttributes<T>();

      protected virtual Key GetKeyAttribute(PropertyInfo property, Table t)
      {
         return property.GetCustomAttributes(true)
           .Where(a => a.GetType() == typeof(Key))
           .Cast<Key>()
           .FirstOrDefault(a => a.TableIndex == t.Index);
      }

      protected abstract Field GetField(PropertyInfo property, int tableIndex);

      protected PropertyInfo[] GetProperties<T>(T obj)
      {
         return obj.GetType().GetProperties();
      }

      protected PropertyInfo GetProperty<T>(string column)
      {
         return (PropertyInfo)this.GetPropertiesInfo<T>()[column.ToLower()];
      }

      protected virtual PropertyInfo GetPrimaryKeyProperty<T>(Table t)
      {
         var keys = new List<PropertyInfo>();
         var prop = typeof(T).GetProperties();

         // faz a leitura das propriedades e retorna a lista
         // que contém o atributo Key
         foreach (var p in prop)
         {
            var attr = p.GetCustomAttributes(true).ToArray();
            var pi = attr.Where(a => a.GetType() == typeof(Key)).ToArray();
            if (pi != null && pi.Length > 0) keys.Add(p);
         }

         // retorna a propriedade que contém o atributo Key associado a tabela indicada
         var key = keys.FirstOrDefault(a => a.GetCustomAttributes(true)
            .Where(b => b.GetType() == typeof(Key))
            .Cast<Key>()
            .FirstOrDefault().TableIndex == t.Index);

         return key;
      }

      protected virtual PropertyInfo[] GetJoinAttributeProperties<T>()
      {
         // obtém a lista de propriedades
         var prop = typeof(T).GetProperties();

         // retorna a lista de propriedades que contém o atributo Join
         var joins = prop.Where(a => a.GetCustomAttributes(true)
            .FirstOrDefault(b => b.GetType() == typeof(Join)) is Join)
            .ToArray();

         return joins;
      }

      protected string[] GetColumnsFromEntity(Table[] tables, PropertyInfo[] properties)
      {
         // armazena a lista de campos
         var cols = new List<string>();

         // faz a leitura das tabelas
         foreach (var t in tables)
         {
            // faz a leitura dos campos correspondentes a cada tabela
            foreach (var p in properties)
            {
               var f = GetField(p, t.Index);
               if (f != null) cols.Add(string.Format("t{0}.{1}", t.Index, f.FieldName));
            }
         }

         // retorna a lista de campos
         return cols.ToArray();
      }

      protected string[] GetJoinsFromEntity(Table[] tables, PropertyInfo[] properties)
      {
         // armazena a lista de comandos join
         var joins = new List<string>();

         // faz a leitura das propriedades
         foreach (var pj in properties)
         {
            // obtém os joins existentes
            var js = pj.GetCustomAttributes(true).Where(a => a.GetType() == typeof(Join))
               .Cast<Join>().OrderBy(a => a.Table1).ThenBy(a => a.Table2).ToList();

            // faz a leitura da lista de joins
            foreach (var j in js)
            {
               var f1 = GetField(pj, j.Table1);
               var f2 = GetField(pj, j.Table2);

               // gera o comando join
               var temp = string.Format("{0} JOIN {1} AS t{2} ON t{3}.{4} = t{2}.{5}",
                  j.Command, tables[j.Table2].TableName, j.Table2, j.Table1, f1.FieldName, f2.FieldName);

               joins.Add(temp);
            }
         }

         // retorna a lista de comandos
         return joins.ToArray();
      }

      protected string[] GetAliasForColumns(string[] columns)
      {
         // armazena a lista de campos
         var alias = new List<string>();

         // agrupa as colunas
         var gs = columns.GroupBy(a => a).ToList();

         // faz a leitura dos grupos
         foreach (var g in gs)
         {
            int count = g.Count();  // conta o número de colunas em cada grupo
            int j = 1;              // inicia o contador

            // há duplicidade
            if (count > 1)
            {
               // substitui os nomes iguais usando índice
               foreach (var c in g)
               {
                  alias.Add(string.Format("{0}{1}", c, j));
                  j++;
               }
            }
            // inclui o nome atual
            else { alias.Add(g.Key); }
         }

         // retorna a lista de campos
         return alias.ToArray();
      }

      protected abstract string[] GetColumnsAndParametersForFilters(IDbCommand command, Field[] fields, string[] columns, string[] alias, string[] operators, object[] values);

      protected string[] GetColumnsForSort(Field[] fields, string[] columns, bool[] descs)
      {
         // armazena os campos para ordenação
         var sort = new List<string>();

         // cria a sequência de ordenção conforme as colunas especificados
         for (int i = 0; i < columns.Length; i++)
         {
            // obtém os atributos da coluna
            var f = fields.Where(a => a.FieldName.ToLower() == columns[i].ToLower()).ToList()[0];

            // adiciona na lista temporária
            sort.Add(string.Format("t{0}.{1}{2}", f.TableIndex, f.FieldName, (descs[i] ? " DESC" : "")));
         }

         // retorna a lista de campos para ordenação
         return sort.ToArray();
      }

      protected virtual T GetEntity<T>(DataRow dr, Field[] fields)
      {
         // datasource é inválido; retorna o valor padrão do objeto
         if (dr == null)
            return default(T);

         // variáveis locais
         var columns = GetPropertiesInfo<T>();

         // cria uma instância do objeto
         var newObject = Activator.CreateInstance<T>();

         // faz leitura do registro
         try
         {
            for (int index = 0; index < fields.Length; index++)
            {
               var field = fields[index];
               var info = (PropertyInfo)columns[field.FieldName.ToLower()];
               object valor = dr[field.FieldName];

               if (field.FieldName != "" && info.CanWrite)
               {
                  // obtém o tipo de dados da propriedades
                  // converte o valor e atribui
                  var t = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;
                  object safeValue = (valor == DBNull.Value || valor == null) ? null : Convert.ChangeType(valor, t);
                  info.SetValue(newObject, safeValue, null);
               }
            }
         }
         catch (Exception ex)
         {
            throw ex;
         }

         // retorna o novo registro
         return newObject;
      }

      protected virtual T[] GetEntities<T>(DataRow[] dr, Field[] fields)
      {
         // datasource é inválido; retorna o valor padrão do objeto
         if (dr == null)
            return new T[] { };

         // variáveis locais
         var columns = GetPropertiesInfo<T>();

         // armazena a lista de retorno
         var entities = new List<T>();

         // faz a leitura dos registros
         foreach (var item in dr)
         {
            // cria uma instância do objeto
            var newObject = Activator.CreateInstance<T>();

            try
            {
               for (int index = 0; index < columns.Count; index++)
               {
                  var field = fields[index];
                  var info = (PropertyInfo)columns[field.FieldName.ToLower()];
                  object valor = item[field.FieldName];

                  if (field.FieldName != "" && info.CanWrite)
                  {
                     // obtém o tipo de dados da propriedades
                     // converte o valor e atribui
                     var t = Nullable.GetUnderlyingType(info.PropertyType) ?? info.PropertyType;
                     object safeValue = (valor == DBNull.Value || valor == null) ? null : Convert.ChangeType(valor, t);
                     info.SetValue(newObject, safeValue, null);
                  }
               }
               entities.Add(newObject);
            }
            catch { throw; }
         }

         // retorna a lista de registros
         return entities.ToArray();
      }

      protected object ParseValue(object value)
      {
         return ParseValue(value, "");
      }

      protected object ParseValue(object value, string oper)
      {
         if (value == null) 
            return DBNull.Value;
         else
            return (oper.ToUpper() == "CONTAINS") ? "%" + value + "%" : value;
      }

      private Hashtable GetPropertiesInfo<T>()
      {
         var hashtable = new Hashtable();
         var properties = typeof(T).GetProperties();

         // faz a leitura das propriedades do objeto
         foreach (var info in properties)
         {
            var field = info.GetCustomAttributes(true).Where(a => a is Field)
               .Cast<Field>()
               .OrderBy(b => b.TableIndex)
               .ToArray();

            if (field != null && field.Length > 0)
               hashtable[field[0].FieldName.ToLower()] = info;
         }

         return hashtable;
      }
   }
}