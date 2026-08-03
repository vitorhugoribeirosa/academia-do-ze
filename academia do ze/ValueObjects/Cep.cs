// VITOR HUGO RIBEIRO SA
namespace AcademiaDoZe.Domain.ValueObjects;

public sealed record Cep
{
    public string Valor { get; }

    public Cep(string valor)
    {
        Valor = valor;
    }
}
