// VITOR HUGO RIBEIRO SA
namespace AcademiaDoZe.Domain.ValueObjects;

public sealed record Cpf
{
    public string Valor { get; }

    public Cpf(string valor)
    {
        Valor = valor;
    }
}
