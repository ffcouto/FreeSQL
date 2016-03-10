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
using System.Text;
using FreeSQL;
using FreeSQL.Common;

namespace FreeSQL.Database.MsSQL
{
   public class FreeSQLMsSQL : FreeSQLDatabase
   {
      private readonly ILogger LogDatabase;

      private SqlConnection conn;
      private SqlTransaction trans;

      public FreeSQLMsSQL(string dbConnString, ILogger dbLogger)
      {
         this.ConnectionString = dbConnString;
         this.LogDatabase = dbLogger;
      }

      // armazena a string de conexão
      public virtual string ConnectionString { get; set; }

      public virtual string GetUserLogged()
      {
         try
         {
            var cn = new SqlConnection(ConnectionString);
            cn.Open();
            string sql = "SELECT SYSTEM_USER AS usuario;";
            var cmd = new SqlCommand(sql, cn);
            string ret = cmd.ExecuteScalar().ToString();
            cn.Close();
            cn = null;
            return ret;
         }
         catch { return ""; }
      }

      public virtual string GetIpAddress()
      {
         try
         {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            return host.AddressList.FirstOrDefault(a => a.AddressFamily.ToString() == "InterNetwork").ToString();
         }
         catch
         {
            return "0.0.0.0";
         }
      }

      #region IDatabase

      internal protected SqlConnection CurrentConnection
      {
         get { return this.conn; }
      }

      internal protected SqlTransaction CurrentTransaction
      {
         get { return this.trans; }
      }

      public virtual void OpenConnection()
      {
         // quando há uma transação em uso,
         // não permite abrir novas conexões
         if (trans != null)
            return;

         // quando a propriedade KeepAlive é true,
         // novas conexões não são permitidas
         if (this.KeepAlive && conn != null && conn.State == ConnectionState.Open)
            return;

         try
         {
            // inicia uma nova conexão e abre
            conn = new SqlConnection(this.ConnectionString);
            conn.Open();
         }
         catch { throw; }
      }

      public virtual void CloseConnection()
      {
         // quando há uma transação em uso,
         // não permite fechar
         if (trans != null)
            return;

         // quando a propriedade KeepAlive é true,
         // não é permitido fechar a conexão
         if (this.KeepAlive && conn != null && conn.State == ConnectionState.Open)
            return;

         try
         {
            // fecha a conexão e libera os recursos
            if (conn != null) conn.Close();
            conn = null;
         }
         catch { throw; }
      }

      public virtual IDbConnection GetCurrentConnection()
      {
         return this.conn;
      }

      public virtual DateTime GetCurrentDatetime()
      {
         try
         {
            var cn = new SqlConnection(ConnectionString);
            cn.Open();
            string sql = "SELECT GETDATE() AS dh;";
            var cmd = new SqlCommand(sql, cn);
            DateTime ret = Convert.ToDateTime(cmd.ExecuteScalar());
            cn.Close();
            cn = null;
            return ret;
         }
         catch { return DateTime.Now; }
      }

      #endregion

      #region ITransaction

      public bool KeepAlive { get; set; }

      public void BeginTransaction()
      {
         BeginTransaction(false);
      }

      public void BeginTransaction(bool keepAlive)
      {
         // abre uma nova conexão se não há nenhuma aberta
         if (conn == null)
            OpenConnection();

         // quando o estado da conexão não é aberto
         // fecha a conexão e reabre
         if (conn.State != ConnectionState.Open)
         {
            CloseConnection();
            OpenConnection();
         }

         // permite iniciar uma transação somente
         // quando nehuma outra transação está em uso
         if (trans == null)
            trans = conn.BeginTransaction();

         // atribui a opção de manter a conexão aberta
         KeepAlive = keepAlive;
      }

      public void CommitTransaction()
      {
         // há uma conexão válida
         if (conn != null)
         {
            // uma transação está em uso e não é necessário mantê-la ativa
            if (trans != null && !KeepAlive)
            {
               // faz a gravação
               trans.Commit();
               // libera os recursos da transação
               trans = null;
            }
         }
      }

      public void RollbackTransaction()
      {
         // há uma conexão válida
         if (conn != null)
         {
            // uma transação está em uso E não é necessário mantê-la ativa
            if (trans != null && !KeepAlive)
            {
               // defaz as alterações
               trans.Rollback();
               // libera os recursos da transação
               trans = null;
            }
         }
      }

      public IDbTransaction GetCurrentTransaction()
      {
         return this.trans;
      }

      #endregion

      #region ICRUD

      public virtual object[] Insert<T>(T obj)
      {
         var operation = new InsertSqlOperation<T>(obj, conn, trans);
         operation.Execute();
         return operation.NewIDs;
      }

      public virtual int Insert<T>(T obj, int table)
      {
         var operation = new InsertOneTableSqlOperation<T>(obj, table, conn, trans);
         operation.Execute();
         return operation.NewID;
      }

      public virtual void Update<T>(T obj)
      {
         var operation = new UpdateSqlOperation<T>(obj, conn, trans);
         operation.Execute();
      }

      public virtual void UpdateSpecial<T>(T obj, string column)
      {
         var operation = new UpdateSpecialSqlOperation<T>(obj, column, conn, trans);
         operation.Execute();
      }

      public virtual void Delete<T>(T obj)
      {
         var operation = new DeleteSqlOperation<T>(obj, conn, trans);
         operation.Execute();
      }

      public virtual void DeleteSpecial<T>(string column, object value)
      {
         var operation = new DeleteSpecialSqlOperation<T>(column, value, conn, trans);
         operation.Execute();
      }

      public virtual void DeleteSpecial<T>(string[] columns, object[] values)
      {
         var operation = new DeleteSpecialSqlOperation<T>(columns, values, conn, trans);
         operation.Execute();
      }

      public virtual T Select<T>(int codObj)
      {
         var operation = new SelectSqlOperation<T>(codObj, conn, trans);
         operation.Execute();
         return operation.ReturnObject;
      }

      public virtual T[] SelectAll<T>(string order)
      {
         var operation = new SelectAllSqlOperation<T>(order, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectSpecial<T>(string column, object value)
      {
         return SelectSpecial<T>(column, "=", value);
      }

      public virtual T[] SelectSpecial<T>(string column, string comparison, object value)
      {
         var operation = new SelectSpecialSqlOperation<T>(column, comparison, value, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectSpecial<T>(string[] columns, object[] values)
      {
         string[] opers = Enumerable.Repeat("=", columns.Length).ToArray();
         return SelectSpecial<T>(columns, opers, values);
      }

      public virtual T[] SelectSpecial<T>(string[] columns, string[] comparison, object[] values)
      {
         var operation = new SelectSpecialSqlOperation<T>(columns, comparison, values, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectTop<T>(int rows, string column, bool desc)
      {
         var operation = new SelectTopSqlOperation<T>(rows, column, desc, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectTop<T>(int rows, string fColumn, object value, string sColumn, bool desc)
      {
         return SelectTop<T>(rows, fColumn, "=", value, sColumn, desc);
      }

      public virtual T[] SelectTop<T>(int rows, string fColumn, string comparison, object value, string sColumn, bool desc)
      {
         var operation = new SelectTopWhereSqlOperation<T>(rows, fColumn, comparison, value, sColumn, desc, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectTop<T>(int rows, string[] columns, bool[] descs)
      {
         var operation = new SelectTopSqlOperation<T>(rows, columns, descs, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectTop<T>(int rows, string[] fColumns, object[] values, string[] sColumns, bool[] desc)
      {
         string[] opers = Enumerable.Repeat("=", fColumns.Length).ToArray();
         return SelectTop<T>(rows, fColumns, opers, values, sColumns, desc);
      }

      public virtual T[] SelectTop<T>(int rows, string[] fColumns, string[] comparison, object[] values, string[] sColumns, bool[] desc)
      {
         var operation = new SelectTopWhereSqlOperation<T>(rows, fColumns, comparison, values, sColumns, desc, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual DataRow[] CustomSelect(string SqlCommand, SearchParam[] Parameters)
      {
         var operation = new CustomSelectSqlOperation(SqlCommand, Parameters, conn, trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      #endregion

      #region ILogger

      public virtual void CreateLog(DateTime DateTime, string UserName, string Workstation, string Action, string ExtraInfo)
      {
         if (LogDatabase != null)
            LogDatabase.CreateLog(DateTime, UserName, Workstation, Action, ExtraInfo);
      }

      public virtual void SaveErr(DateTime DateTime, string UserName, string Workstation, string Project, string Module, string FunctionName, int Number, string Description, int Line, int ErrType)
      {
         if (LogDatabase != null)
            LogDatabase.SaveErr(DateTime, UserName, Workstation, Project, Module, FunctionName, Number, Description, Line, ErrType);
      }

      #endregion
   }
}
