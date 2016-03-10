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
using System.Linq;
using System.Data;
using System.Text;

namespace FreeSQL.Common
{
   public class SearchPresenter
   {
      private readonly SearchView view;
      private readonly SearchEngine searcher;

      public SearchPresenter(SearchView view, SearchEngine searchEngine)
      {
         this.view = view;
         this.searcher = searchEngine;
         view.SelectEnabled = false;
         view.SetSearch(searchEngine);
      }

      public DataRow[] Search(SearchParam param, object find)
      {
         // desativa o comando Selecionar
         view.SelectEnabled = false;

         // limpa os resultados anteriores
         view.ClearResults();

         // atribui o parametro selecionado pelo usuário e o valor de pesquisa
         param.Value = find;
         searcher.SelectedFilter = param;

         // não há uma transação definida para realizar a pesquisa
         // dispara uma exceção
         if (searcher.RunFunction == null)
            throw new Exception("A rotina de pesquisa não foi definida para o filtro selecionado.");

         // executa a pesquisa
         searcher.RunFunction.Execute();

         // retorna o resultado da pesquisa
         return searcher.RunFunction.Result;
      }
   }
}
