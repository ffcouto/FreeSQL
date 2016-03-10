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
using System.Reflection;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class UpdateOleDbOperation<T> : OleDbOperation
   {
      // variáveis locais
      private readonly T obj;

      public UpdateOleDbOperation(T obj, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.obj = obj;
      }

      public override void Execute()
      {
         try
         {
            // faz a leitura das tabelas com permissão para alteração (crUd - Update)
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Update)).ToArray();

            foreach (var t in tables)
            {
               var updCommand = GetUpdateCommand(obj, t);
               ExecuteCommand(updCommand);
            }
         }
         catch { throw; }
      }

      private OleDbCommand GetUpdateCommand(T obj, Table t)
      {
         // possui permissão para atualização (crUd - UPDATE)?
         if (!t.CRUD.HasFlag(CrudOptions.Update))
            throw new Exception(string.Format("A tabela {0} não possui permissão para atualização de registros.", t.TableName));

         // atributos personalizados
         var prop = GetProperties(obj);

         // obtém a chave primária e os dados do campo
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);

         // variaveis locais
         var fList = new List<string>();
         var vList = new List<string>();

         // cria o command
         var cmd = new OleDbCommand();

         // faz a leitura das propriedades
         foreach (PropertyInfo item in prop)
         {
            if (item.GetCustomAttributes(true).ToList().Count > 0)
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
                  fList.Add(string.Format("{0} = ?", f.FieldName));

                  // inclui o parametro ao comando
                  cmd.Parameters.Add(f.FieldName, (OleDbType)f.DatabaseType).Value = ParseValue(value);
               }
            }
         }

         // inclui a clásula where
         cmd.Parameters.Add("@id_table", (OleDbType)pf.DatabaseType).Value = ParseValue(pk.GetValue(obj, null));

         // converte as listas
         string fields = string.Join(", ", fList);
         string where = string.Format("{0} = ?", pf.FieldName);

         // define o comando e o atribui
         string query = "UPDATE {0} SET {1} WHERE ({2});";
         cmd.CommandText = string.Format(query, t.TableName, fields, where);

         // return command
         return cmd;
      }
   }
}