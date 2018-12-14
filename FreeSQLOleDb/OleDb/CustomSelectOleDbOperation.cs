/*
FreeSQL
Copyright (C) 2016-2019 Fabiano Couto

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <https://www.gnu.org/licenses/>.
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
   internal class CustomSelectOleDbOperation<T> : OleDbOperation
   {
      // local variables
      private readonly string wCommand;
      private readonly SearchParam[] wParams;

      private T[] retObjs = new T[0];

      public CustomSelectOleDbOperation(string sqlCommand, SearchParam[] parameters, OleDbConnection connection, OleDbTransaction transaction)
         : base(connection, transaction)
      {
         wCommand = sqlCommand;
         wParams = parameters;
      }

      public override void Execute()
      {
         try
         {
            // creates the command
            var readCommand = new OleDbCommand(wCommand, (OleDbConnection)_conn, (OleDbTransaction)_trans);

            // there is a definite search parameter
            if (wParams != null && wParams.Length > 0)
            {
               // sets the list of required parameters
               var pList = new List<OleDbParameter>();

               foreach (var wParam in wParams)
               {
                  // operator IN or NOT IN; uses a comma-separated list
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

               // adds the parameters to the command
               readCommand.Parameters.AddRange(pList.ToArray());
            }

            // executes the command and returns the records
            var objs = LoadDataFromCommand(readCommand);


            // TODO: match table fields to object properties

         }
         catch { throw; }
      }

      public T[] ReturnObjects
      {
         get { return retObjs; }
      }
   }
}