// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public sealed class Logradouro : Entity
{
    public Cep Cep { get; private set; }
    public string Pais { get; private set; }
    public string Estado { get; private set; }
    public string Cidade { get; private set; }
    public string Bairro { get; private set; }
    public string Nome { get; private set; }

    public Logradouro(
        int id,
        Cep cep,
        string pais,
        string estado,
        string cidade,
        string bairro,
        string nome) : base(id)
    {
        Cep = cep;
        Pais = pais;
        Estado = estado;
        Cidade = cidade;
        Bairro = bairro;
        Nome = nome;
    }
}
