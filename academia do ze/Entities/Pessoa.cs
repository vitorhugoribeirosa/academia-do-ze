// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public abstract class Pessoa : Entity
{
    public string NomeCompleto { get; protected set; }
    public Cpf Cpf { get; protected set; }
    public DateOnly DataNascimento { get; protected set; }
    public Telefone Telefone { get; protected set; }
    public Email? Email { get; protected set; }
    public Senha Senha { get; protected set; }
    public Arquivo? Foto { get; protected set; }
    public Endereco Endereco { get; protected set; }

    protected Pessoa(
        int id,
        string nomeCompleto,
        Cpf cpf,
        DateOnly dataNascimento,
        Telefone telefone,
        Email? email,
        Senha senha,
        Arquivo? foto,
        Endereco endereco) : base(id)
    {
        NomeCompleto = nomeCompleto;
        Cpf = cpf;
        DataNascimento = dataNascimento;
        Telefone = telefone;
        Email = email;
        Senha = senha;
        Foto = foto;
        Endereco = endereco;
    }
}
