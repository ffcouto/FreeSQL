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
using System.Linq;
using System.Data;
using System.Text;

namespace FreeSQL.Common
{
   public class SearchPresenter
   {
      private readonly SearchView _view;
      private readonly SearchEngine _searcher;

      public SearchPresenter(SearchView searchView, SearchEngine searchEngine)
      {
         _view = searchView;
         _searcher = searchEngine;
         searchView.SelectEnabled = false;
         searchView.SetSearch(searchEngine);
      }

      public object[] Search(SearchParam param, object find)
      {
         // disables the Select command
         _view.SelectEnabled = false;

         // clean previous results
         _view.ClearResults();

         // assigns the parameter selected by the user and the search value
         param.Value = find;
         _searcher.SelectedFilter = param;

         // there is no set transaction to perform the search
         // throws an exception
         if (_searcher.RunFunction == null)
            throw new Exception("A rotina de pesquisa não foi definida para o filtro selecionado.");

         // runs the search
         _searcher.RunFunction.Execute();

         // returns search result
         return _searcher.RunFunction.Result;
      }
   }
}
