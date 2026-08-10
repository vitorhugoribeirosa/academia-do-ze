// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public sealed class Logradouro : Entity
{
    public Cep Cep { get; }
    public string Nome { get; }
    public string Bairro { get; }
    public string Cidade { get; }
    public string Estado { get; }
    public string Pais { get; }

    private Logradouro(
        int id,
        Cep cep,
        string nome,
        string bairro,
        string cidade,
        string estado,
        string pais) : base(id)
    {
        Cep = cep;
        Nome = nome;
        Bairro = bairro;
        Cidade = cidade;
        Estado = estado;
        Pais = pais;
    }

    public static Result<Logradouro> Criar(
        int id,
        string cep,
        string nome,
        string bairro,
        string cidade,
        string estado,
        string pais)
    {
        var notifications = new List<Notification>();

        var cepResult = Cep.Criar(cep);
        if (cepResult.IsFailure)
            notifications.AddRange(cepResult.Notifications);

        ValidarTextoObrigatorio("Nome", "NOME_OBRIGATORIO", ref nome, notifications);
        ValidarTextoObrigatorio("Bairro", "BAIRRO_OBRIGATORIO", ref bairro, notifications);
        ValidarTextoObrigatorio("Cidade", "CIDADE_OBRIGATORIA", ref cidade, notifications);
        ValidarTextoObrigatorio("Estado", "ESTADO_OBRIGATORIO", ref estado, notifications);
        ValidarTextoObrigatorio("Pais", "PAIS_OBRIGATORIO", ref pais, notifications);

        estado = NormalizadoService.ParaMaiusculo(
            NormalizadoService.LimparTodosEspacos(estado));

        if (notifications.Count != 0)
            return Result<Logradouro>.Failure(notifications);

        return Result<Logradouro>.Success(
            new Logradouro(id, cepResult.Value!, nome, bairro, cidade, estado, pais));
    }

    private static void ValidarTextoObrigatorio(
        string propriedade,
        string mensagem,
        ref string valor,
        ICollection<Notification> notifications)
    {
        if (NormalizadoService.TextoVazioOuNulo(valor))
            notifications.Add(new Notification(propriedade, mensagem));
        else
            valor = NormalizadoService.LimparEspacos(valor);
    }
}
