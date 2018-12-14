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

namespace FreeSQL
{
   public class TransactionQueue
   {
      private readonly IDictionary<string, Transaction> _transactions;

      public TransactionQueue()
      {
         _transactions = new Dictionary<string, Transaction>();
      }

      public void Add(string Key, Transaction transaction)
      {
         if (_transactions.ContainsKey(Key))
            _transactions[Key] = transaction;
         else
            _transactions.Add(Key, transaction);
      }

      public void Clear()
      {
         _transactions.Clear();
      }

      public void Remove(string Key)
      {
         _transactions.Remove(Key);
      }

      public void ExecuteAll()
      {
         foreach (KeyValuePair<string, Transaction> t in _transactions)
            (t.Value as Transaction).Execute();
      }
   }
}
