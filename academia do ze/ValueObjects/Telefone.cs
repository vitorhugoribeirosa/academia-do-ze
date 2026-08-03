// VITOR HUGO RIBEIRO SA
namespace AcademiaDoZe.Domain.ValueObjects;

public sealed record Telefone
{
    public string Valor { get; }

    public Telefone(string valor)
    {
        Valor = valor;
    }
}
