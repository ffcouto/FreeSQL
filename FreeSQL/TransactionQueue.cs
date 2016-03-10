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

namespace FreeSQL
{
   public class TransactionQueue
   {
      private readonly IDictionary<string, Transaction> transactions;

      public TransactionQueue()
      {
         this.transactions = new Dictionary<string, Transaction>();
      }

      public void Add(string Key, Transaction transaction)
      {
         if (transactions.ContainsKey(Key))
            transactions[Key] = transaction;
         else
            transactions.Add(Key, transaction);
      }

      public void Clear()
      {
         transactions.Clear();
      }

      public void Remove(string Key)
      {
         transactions.Remove(Key);
      }

      public void ExecuteAll()
      {
         foreach (KeyValuePair<string, Transaction> t in transactions)
            (t.Value as Transaction).Execute();
      }
   }
}
