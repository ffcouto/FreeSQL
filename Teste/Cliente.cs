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
using System.Text;
using FreeSQL.Backwork;

namespace Teste
{
   // index => ordem das tabelas
   // tableName => nome da tabela no banco de dados
   // virtualDelete => define se o registro terá sua exclusão física ou um campo (ativo) para marcar como excluído
   [Table(0, "clientes", true)]
   class Cliente
   {
      public Cliente()
      {
         this.Ativo = true;
      }

      // o atributo OleDbField define o campo da tabela
      // tableIndex => identificação de qual tabela o campo pertence
      // fieldName => nome do campo da tabela
      // dataType => tipo de dados do campo
      // visible => define se aparecerá no comando SQL (padrão true)

      // o atributo Key define a chave primária da tabela
      // pk => define se o campo é ou não a chave primária da tabela
      // autoIncrement => define se o campo é ou não auto-numeração
      // table => identificação de qual tabela a chave pertence

      [OleDbField(0, "codigo", System.Data.OleDb.OleDbType.Integer)]
      [Key(true, false, 0)]
      public int Codigo { get; set; }

      [OleDbField(0, "nome", System.Data.OleDb.OleDbType.VarChar)]
      public string Nome { get; set; }

      [OleDbField(0, "data_nascimento", System.Data.OleDb.OleDbType.Date)]
      public DateTime DataNascimento { get; set; }

      // o campo ativo não é necessário quando a opção virtualDelete do atributo
      // Table é true, no entanto, caso queira saber o status é recomendado seu uso
      [OleDbField(0, "ativo", System.Data.OleDb.OleDbType.Boolean)]
      public bool Ativo { get; set; }


   }
}
