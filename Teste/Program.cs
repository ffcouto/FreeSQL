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
using System.IO;
using System.Linq;
using System.Text;
using FreeSQL.Database.OleDb;

namespace Teste
{
   class Program
   {
      static void Main(string[] args)
      {
         // *******************************************************************
         // Testing for the OleDb provider through the FreeSQLOleDb library
         // *******************************************************************

         // sets the connection command
         string connString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};User ID=admin;Password=;Jet OLEDB:Database Password=;",
            Path.Combine(Directory.GetCurrentDirectory(), "teste.mdb"));

         // Instance access to the database through the FreeSQL library
         var db = new FreeSQLOleDb(connString, null);

         try
         {
            // open connection
            db.OpenConnection();
            Console.WriteLine("The connection is open.");

            // creates a new customer
            var c = new Customer();
            c.Name = "Zezinho";
            c.BirthDate = new DateTime(2016, 1, 1);

            // Note: because the ID is auto-increment, it's not necessary to enter the number;
            // as the 'Active' property, it's recommended to set to true in the class constructor,
            // otherwise it will be necessary to inform each instance of the object, since the
            // record will be written and marked as deleted in the database.
            c.Active = true;

            // inserts the record and retrieves the new code
            var newID = (int)db.Insert(c)[0];
            Console.WriteLine("Customer registered successfully.");

            // reads the new inserted record
            var readCustomer = db.Select<Customer>(newID);
            Console.WriteLine("Consulting customer data registered.");
            Console.WriteLine("   ID    : " + readCustomer.ID);
            Console.WriteLine("   Name  : " + readCustomer.Name);
            Console.WriteLine("   Birth date: " + readCustomer.BirthDate.ToString());
            Console.WriteLine("   Active:" + (readCustomer.Active ? "Sim" : "Não"));
            Console.WriteLine("");

            Console.WriteLine("Enter a new name for the customer: ");
            string newNome = Console.ReadLine();

            // change the customer name
            readCustomer.Name = newNome;

            // updates customer record
            db.Update(readCustomer);
            Console.WriteLine("Customer successfully changed.");

            // customer search
            // the function requires the name of the field, the operator and the search value
            // when the operator is suppressed the search by default uses "="
            var findName = db.SelectSpecial<Customer>("nome", "LIKE", "ze%");
            Console.WriteLine(string.Format("Customer search that the name contain 'ze' {0} returned result (s).", findName.Length));

            // for use of the IN operator it is necessary to assign an array of values,
            // this value can be of any data type
            var findID = db.SelectSpecial<Customer>("codigo", "IN", new int[] { 1, 2, 3 });
            Console.WriteLine(string.Format("Customer search with id's 1, 2 and 3 returned {0} result (s).", findID.Length));

            // use the SelectAll method to return all active records in the table
            var all = db.SelectAll<Customer>("nome");
            Console.WriteLine(string.Format("Query from all clients returned {0} result (s).", all.Length));

            // excluding customer
            db.Delete(readCustomer);
            Console.WriteLine(string.Format("Customer '{0}' deleted successfully.", readCustomer.Name));

            // clean resources
            c = null;
            readCustomer = null;

            Environment.Exit(0);
         }
         catch (Exception ex)
         {
            Console.WriteLine("The following error occurred while opening the connection:\n" + ex.Message);
            db.CloseConnection();
            db = null;
            Environment.Exit(1);
         }
      }
   }
}
