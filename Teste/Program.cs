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
         // Teste para o provedor OleDb através da biblioteca FreeSQLOleDb
         // *******************************************************************

         // define o comando de conexão
         string connString = string.Format("Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};User ID=admin;Password=;Jet OLEDB:Database Password=;",
            Path.Combine(Directory.GetCurrentDirectory(), "teste.mdb"));

         // instancia o acesso ao banco de dados através da biblioteca FreeSQL
         var db = new FreeSQLOleDb(connString, null);

         try
         {
            // abre a conexão
            db.OpenConnection();
            Console.WriteLine("Conexão aberta.");

            // cria um novo cliente
            var cli = new Cliente();
            cli.Nome = "Zezinho";
            cli.DataNascimento = new DateTime(2016, 1, 1);

            // Observação: como o código é auto-numerado não é necessário informar o número;
            // quanto a propriedade ativo, é recomendado setar para true no construtor da classe,
            // do contrário será necessário informar a cada instância do objeto, uma vez que o 
            // registro será gravado e marcado como excluído no banco de dados
            cli.Ativo = true;

            // insere o registro e recupera o novo código
            var novoCod = (int)db.Insert(cli)[0];
            Console.WriteLine("Cliente cadastrado com sucesso.");

            // faz a leitura do novo registro inserido
            var lerCli = db.Select<Cliente>(novoCod);
            Console.WriteLine("Consultando os dados do cliente cadastrado.");
            Console.WriteLine("   Código: " + lerCli.Codigo);
            Console.WriteLine("   Nome  : " + lerCli.Nome);
            Console.WriteLine("   Data nasc: " + lerCli.DataNascimento.ToString());
            Console.WriteLine("   Ativo: " + (lerCli.Ativo ? "Sim" : "Não"));
            Console.WriteLine("");

            Console.WriteLine("Digite um novo nome para o cliente: ");
            string novoNome = Console.ReadLine();

            // altera o nome do cliente
            lerCli.Nome = novoNome;

            // salva o cliente
            db.Update(lerCli);
            Console.WriteLine("Cliente alterado com sucesso.");

            // pesquisa de clientes
            // a função requer o nome do campo, o operador e o valor de pesquisa
            // quando o operador é suprimido a pesquisa por padrão utiliza "="
            var pesqNome = db.SelectSpecial<Cliente>("nome", "LIKE", "ze%");
            Console.WriteLine(string.Format("Consulta de clientes que no nome contenham 'ze' retornou {0} resultado(s).", pesqNome.Length));
            
            // para uso do operador IN é necessário atribuir uma matriz (array) de valores,
            // esse valor pode ser de qualquer tipo de dados
            var pesqCod = db.SelectSpecial<Cliente>("codigo", "IN", new int[] { 1, 2, 3 });
            Console.WriteLine(string.Format("Consulta de clientes com os códigos 1, 2 e 3 retornou {0} resultado(s).", pesqCod.Length));

            // use o método SelectAll para retornar todos os registros ativos da tabela
            var todos = db.SelectAll<Cliente>("nome");
            Console.WriteLine(string.Format("Consulta de todos os clientes retornou {0} resultado(s).", todos.Length));

            // excluindo o cliente
            db.Delete(lerCli);
            Console.WriteLine(string.Format("Cliente '{0}' excluído com sucesso.", lerCli.Nome));

            // limpa os recursos
            cli = null;
            lerCli = null;

            Environment.Exit(0);
         }
         catch (Exception ex)
         {
            Console.WriteLine("Ocorreu o seguinte erro ao abrir a conexão:\n" + ex.Message);
            db.CloseConnection();
            db = null;
            Environment.Exit(1);
         }
      }
   }
}
