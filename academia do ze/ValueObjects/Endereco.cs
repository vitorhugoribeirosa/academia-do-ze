// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Entities;

namespace AcademiaDoZe.Domain.ValueObjects;

public sealed record Endereco
{
    public Logradouro Logradouro { get; }
    public string Numero { get; }
    public string? Complemento { get; }

    public Endereco(Logradouro logradouro, string numero, string? complemento)
    {
        Logradouro = logradouro;
        Numero = numero;
        Complemento = complemento;
    }
}
