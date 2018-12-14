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

      private SqlConnection _conn;
      private SqlTransaction _trans;

      public FreeSQLMsSQL(string dbConnString, ILogger dbLogger)
      {
         ConnectionString = dbConnString;
         LogDatabase = dbLogger;
      }

      // stores the connection string
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
         get { return _conn; }
      }

      internal protected SqlTransaction CurrentTransaction
      {
         get { return _trans; }
      }

      public virtual void OpenConnection()
      {
         // when there is a transaction in use,
         // does not allow opening new connections
         if (_trans != null) return;

         // when the KeepAlive property is true,
         // new connections are not allowed
         if (KeepAlive && _conn != null && _conn.State == ConnectionState.Open) return;

         try
         {
            // starts a new connection and opens it
            _conn = new SqlConnection(ConnectionString);
            _conn.Open();
         }
         catch { throw; }
      }

      public virtual void CloseConnection()
      {
         // when there is a transaction in use,
         // ignores the close command
         if (_trans != null) return;

         // when the KeepAlive property is true,
         // it is not allowed to close the connection
         if (KeepAlive && _conn != null && _conn.State == ConnectionState.Open) return;

         try
         {
            // closes the connection and releases resources
            if (_conn != null) _conn.Close();
            _conn = null;
         }
         catch { throw; }
      }

      public virtual IDbConnection GetCurrentConnection()
      {
         return _conn;
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
         // opens a new connection if there is no open connection
         if (_conn == null) OpenConnection();

         // when the connection state is not opened
         // close the connection and reopen
         if (_conn.State != ConnectionState.Open)
         {
            CloseConnection();
            OpenConnection();
         }

         // allows to start a transaction only
         // when no other transaction is in use
         if (_trans == null) _trans = _conn.BeginTransaction();

         // assigns the option to keep the connection open
         KeepAlive = keepAlive;
      }

      public void CommitTransaction()
      {
         CommitTransaction(false);
      }

      public void CommitTransaction(bool keepAlive)
      {
         // assigns the option to keep the connection open
         KeepAlive = keepAlive;

         // there is a valid connection
         if (_conn != null)
         {
            // a transaction is in use AND is not necessarary to keep it active
            if (_trans != null && !KeepAlive)
            {
               // save the changes and releases resources
               _trans.Commit();
               _trans = null;
            }
         }
      }

      public void RollbackTransaction()
      {
         RollbackTransaction(false);
      }

      public void RollbackTransaction(bool keepAlive)
      {
         // assigns the option to keep the connection open
         KeepAlive = keepAlive;

         // there is a valid connection
         if (_conn != null)
         {
            // a transaction is in use AND is not necessary to keep it active
            if (_trans != null && !KeepAlive)
            {
               // undo changes and releases resources
               _trans.Rollback();
               _trans = null;
            }
         }
      }

      public IDbTransaction GetCurrentTransaction()
      {
         return _trans;
      }

      #endregion

      #region ICRUD

      public virtual object[] Insert<T>(T obj)
      {
         var operation = new InsertSqlOperation<T>(obj, _conn, _trans);
         operation.Execute();
         return operation.NewIDs;
      }

      public virtual int Insert<T>(T obj, int table)
      {
         var operation = new InsertOneTableSqlOperation<T>(obj, table, _conn, _trans);
         operation.Execute();
         return operation.NewID;
      }

      public virtual void Update<T>(T obj)
      {
         var operation = new UpdateSqlOperation<T>(obj, _conn, _trans);
         operation.Execute();
      }

      public virtual void UpdateSpecial<T>(T obj, string column)
      {
         var operation = new UpdateSpecialSqlOperation<T>(obj, column, _conn, _trans);
         operation.Execute();
      }

      public virtual void Delete<T>(T obj)
      {
         var operation = new DeleteSqlOperation<T>(obj, false, _conn, _trans);
         operation.Execute();
      }

      public virtual void Delete<T>(T obj, bool ignoreVirtual)
      {
         var operation = new DeleteSqlOperation<T>(obj, ignoreVirtual, _conn, _trans);
         operation.Execute();
      }

      public virtual void DeleteSpecial<T>(string column, object value)
      {
         var operation = new DeleteSpecialSqlOperation<T>(column, value, false, _conn, _trans);
         operation.Execute();
      }

      public virtual void DeleteSpecial<T>(string column, object value, bool ignoreVirtual)
      {
         var operation = new DeleteSpecialSqlOperation<T>(column, value, ignoreVirtual, _conn, _trans);
         operation.Execute();
      }

      public virtual void DeleteSpecial<T>(string[] columns, object[] values)
      {
         var operation = new DeleteSpecialSqlOperation<T>(columns, values, false, _conn, _trans);
         operation.Execute();
      }

      public virtual void DeleteSpecial<T>(string[] columns, object[] values, bool ignoreVirtual)
      {
         var operation = new DeleteSpecialSqlOperation<T>(columns, values, ignoreVirtual, _conn, _trans);
         operation.Execute();
      }

      public virtual T Select<T>(int codObj)
      {
         var operation = new SelectSqlOperation<T>(codObj, _conn, _trans);
         operation.Execute();
         return operation.ReturnObject;
      }

      public virtual T[] SelectAll<T>(string order)
      {
         var operation = new SelectAllSqlOperation<T>(order, _conn, _trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectSpecial<T>(string column, object value)
      {
         return SelectSpecial<T>(column, "=", value);
      }

      public virtual T[] SelectSpecial<T>(string column, string comparison, object value)
      {
         var operation = new SelectSpecialSqlOperation<T>(column, comparison, value, _conn, _trans);
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
         var operation = new SelectSpecialSqlOperation<T>(columns, comparison, values, _conn, _trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectTop<T>(int rows, string column, bool desc)
      {
         var operation = new SelectTopSqlOperation<T>(rows, column, desc, _conn, _trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectTop<T>(int rows, string fColumn, object value, string sColumn, bool desc)
      {
         return SelectTop<T>(rows, fColumn, "=", value, sColumn, desc);
      }

      public virtual T[] SelectTop<T>(int rows, string fColumn, string comparison, object value, string sColumn, bool desc)
      {
         var operation = new SelectTopWhereSqlOperation<T>(rows, fColumn, comparison, value, sColumn, desc, _conn, _trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] SelectTop<T>(int rows, string[] columns, bool[] descs)
      {
         var operation = new SelectTopSqlOperation<T>(rows, columns, descs, _conn, _trans);
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
         var operation = new SelectTopWhereSqlOperation<T>(rows, fColumns, comparison, values, sColumns, desc, _conn, _trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T[] CustomSelect<T>(string commandText, SearchParam[] parameters)
      {
         var operation = new CustomSelectSqlOperation<T>(commandText, parameters, _conn, _trans);
         operation.Execute();
         return operation.ReturnObjects;
      }

      public virtual T SelectValue<T>(string commandText, SearchParam[] parameters)
      {
         var operation = new SelectValueSqlOperation<T>(commandText, parameters, _conn, _trans);
         operation.Execute();
         return operation.ReturnValue;
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
