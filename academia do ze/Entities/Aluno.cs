// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public sealed class Aluno : Pessoa
{
    public Aluno(
        int id,
        string nomeCompleto,
        Cpf cpf,
        DateOnly dataNascimento,
        Telefone telefone,
        Email? email,
        Senha senha,
        Arquivo? foto,
        Endereco endereco)
        : base(id, nomeCompleto, cpf, dataNascimento, telefone, email, senha, foto, endereco)
    {
    }
}
