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
using FreeSQL.Common;

namespace FreeSQL.Database.OleDb
{
   internal class CustomSelectOleDbOperation : OleDbOperation
   {
      // variáveis locais
      private readonly string wCommand;
      private readonly SearchParam[] wParams;

      private DataRow[] retObjs = new DataRow[0];

      public CustomSelectOleDbOperation(string sqlCommand, SearchParam[] parameters, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         this.wCommand = sqlCommand;
         this.wParams = parameters;
      }

      public override void Execute()
      {
         try
         {
            // define o comando
            var readCommand = new OleDbCommand(wCommand, (OleDbConnection)conn, (OleDbTransaction)trans);

            // há um parâmetro de pesquisa definido
            if (wParams != null && wParams.Length > 0)
            {
               // define a lista de parâmetros necessários
               var pList = new List<OleDbParameter>();

               foreach (var wParam in wParams)
               {
                  // operador IN ou NOT IN; utiliza uma lista separada por vírgula
                  if (wParam.Comparison == SearchComparison.OneOf || wParam.Comparison == SearchComparison.NotOneOf)
                  {
                     string[] optList = wParam.ParseValue.ToString().Split(',');
                     for (int i = 0; i < optList.Length; i++)
                        pList.Add(new OleDbParameter(string.Format("@{0}{1}", wParam.FieldName.Replace(".", "_"), i), (OleDbType)wParam.DataType) { Value = optList[i] });
                  }
                  else
                  {
                     pList.Add(new OleDbParameter(string.Format("@{0}", wParam.FieldName.Replace(".", "_")), (OleDbType)wParam.DataType) { Value = wParam.ParseValue });
                  }
               }

               // adiciona os parâmetros ao comando
               readCommand.Parameters.AddRange(pList.ToArray());
            }

            // executa o comando e retorna os registros
            retObjs = LoadDataFromCommand(readCommand);
         }
         catch { throw; }
      }

      public DataRow[] ReturnObjects
      {
         get { return retObjs; }
      }
   }
}