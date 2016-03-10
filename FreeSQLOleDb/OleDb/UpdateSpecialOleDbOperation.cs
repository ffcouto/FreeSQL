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
   internal class UpdateSpecialOleDbOperation<T> : OleDbOperation
   {
      // variáveis locais
      private readonly T obj;
      private readonly string fColumn;

      public UpdateSpecialOleDbOperation(T obj, string column, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.obj = obj;
         this.fColumn = column;
      }

      public override void Execute()
      {
         try
         {
            // faz a leitura dos campos correspondentes a coluna especificada
            var pk = GetProperty<T>(fColumn);
            var sf = pk.GetCustomAttributes(true).Where(a => a.GetType() == typeof(OleDbField)).Cast<OleDbField>().ToArray();
            var tables = GetTableAttributes<T>().Where(a => !a.Relationship && a.CRUD.HasFlag(CrudOptions.Update)).ToArray();

            foreach (var f in sf)
            {
               var updCommand = GetUpdateSpecialCommand(obj, tables[f.TableIndex], fColumn);
               ExecuteCommand(updCommand);
            }
         }
         catch { throw; }
      }

      private OleDbCommand GetUpdateSpecialCommand(T obj, Table t, string column)
      {
         // possui permissão para atualização (crUd - UPDATE)?
         if (!t.CRUD.HasFlag(CrudOptions.Update))
            throw new Exception(string.Format("A tabela {0} não possui permissão para atualização de registros.", t.TableName));

         // obtém a chave primária e os dados do campo
         var pk = GetPrimaryKeyProperty<T>(t);
         var pf = GetField(pk, t.Index);
         var pv = pk.GetValue(obj, null);

         // obtém os atributos do campo a ser atualizado
         var uk = GetProperty<T>(column);
         var uf = GetField(uk, t.Index);
         var uv = uk.GetValue(obj, null);

         // comando para atualização
         string sql = "UPDATE {0} SET {1} = ? WHERE ({2} = ?);";

         // cria o comando
         var cmd = new OleDbCommand();
         cmd.CommandText = string.Format(sql, t.TableName, uf.FieldName, pf.FieldName);
         cmd.Parameters.Add(string.Format("@{0}", uf.FieldName), (OleDbType)uf.DatabaseType).Value = ParseValue(uv);
         cmd.Parameters.Add(string.Format("@{0}", pf.FieldName), (OleDbType)pf.DatabaseType).Value = ParseValue(pv);
         return cmd;
      }
   }
}