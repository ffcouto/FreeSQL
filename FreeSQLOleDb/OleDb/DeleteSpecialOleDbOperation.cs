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
using System.Data.OleDb;
using System.Linq;
using System.Text;
using FreeSQL.Backwork;

namespace FreeSQL.Database.OleDb
{
   internal class DeleteSpecialOleDbOperation<T> : OleDbOperation
   {
      // variáveis locais
      private readonly string[] wColumns;
      private readonly object[] wValues;

      public DeleteSpecialOleDbOperation(string column, object value, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.wColumns = new string[] { column };
         this.wValues = new object[] { value };
      }

      public DeleteSpecialOleDbOperation(string[] columns, object[] values, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.wColumns = columns;
         this.wValues = values;
      }

      public override void Execute()
      {
         try
         {
            var delCommand = GetDeleteSpecialCommand(wColumns, wValues);
            ExecuteCommand(delCommand);
         }
         catch { throw; }
      }

      private OleDbCommand GetDeleteSpecialCommand(string[] columns, object[] values)
      {
         // verifica se o número de colunas e valores são iguais
         if (columns.Length != values.Length)
            throw new Exception("O número de colunas e valores são inconsistentes.");

         // atributos personalizados
         var t = GetTableAttributes<T>()[0];

         // possui permissão para exclusão (cruD - DELETE)?
         if (!t.CRUD.HasFlag(CrudOptions.Delete))
            throw new Exception(string.Format("A tabela {0} não possui permissão para exclusão de registros.", t.TableName));

         // propriedades da tabela
         var filter = new List<string>();

         // cria um novo comando
         var cmd = new OleDbCommand();

         // cria os filtros da cláusula where conforme colunas e valores especificados
         for (int i = 0; i < columns.Length; i++)
         {
            // obtém os atributos da coluna
            var pf = (OleDbField)GetFieldAttributes<T>().Where(a => a.FieldName.ToLower() == columns[i].ToLower()).ToList()[0];

            // adiciona na lista temporária
            filter.Add(string.Format("({0} = ?)", pf.FieldName));

            // adiciona o parâmetro correspondente a coluna
            cmd.Parameters.Add(string.Format("@{0}", pf.FieldName), (OleDbType)pf.DatabaseType).Value = ParseValue(values[i]);
         }

         // comando para exclusão física
         string sql = "DELETE FROM {0} WHERE {1};";
         string where = string.Join(" AND ", filter);

         // exclusão do registro é virtual
         if (t.VirtualDelete)
            sql = "UPDATE {0} SET ativo = ? WHERE {1};";

         // define o comando a ser executado
         cmd.CommandText = string.Format(sql, t.TableName, where);
         if (t.VirtualDelete) cmd.Parameters.Insert(0, new OleDbParameter("@ativo", OleDbType.Boolean) { Value = false });
         return cmd;
      }
   }
}