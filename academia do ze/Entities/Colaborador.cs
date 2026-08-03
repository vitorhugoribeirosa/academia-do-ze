// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public sealed class Colaborador : Pessoa
{
    public DateOnly DataAdmissao { get; private set; }
    public ColaboradorTipo Tipo { get; private set; }
    public ColaboradorVinculo Vinculo { get; private set; }

    public Colaborador(
        int id,
        string nomeCompleto,
        Cpf cpf,
        DateOnly dataNascimento,
        Telefone telefone,
        Email? email,
        Senha senha,
        Arquivo? foto,
        Endereco endereco,
        DateOnly dataAdmissao,
        ColaboradorTipo tipo,
        ColaboradorVinculo vinculo)
        : base(id, nomeCompleto, cpf, dataNascimento, telefone, email, senha, foto, endereco)
    {
        DataAdmissao = dataAdmissao;
        Tipo = tipo;
        Vinculo = vinculo;
    }
}
