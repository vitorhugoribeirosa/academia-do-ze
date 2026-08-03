// VITOR HUGO RIBEIRO SA
namespace AcademiaDoZe.Domain.ValueObjects;

public sealed record Senha
{
    public string Valor { get; }

    public Senha(string valor)
    {
        Valor = valor;
    }
}
