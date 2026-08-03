// VITOR HUGO RIBEIRO SA
namespace AcademiaDoZe.Domain.ValueObjects;

public sealed record Arquivo
{
    public string Nome { get; }
    public byte[] Conteudo { get; }

    public Arquivo(string nome, byte[] conteudo)
    {
        Nome = nome;
        Conteudo = conteudo;
    }
}
