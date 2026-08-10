// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Services;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Entities;

public sealed class Aluno : Pessoa, IAggregateRoot
{
    private Aluno(
        int id,
        string nome,
        Cpf cpf,
        DateOnly dataNascimento,
        Telefone telefone,
        Email? email,
        Endereco endereco,
        Senha senha,
        Arquivo? foto)
        : base(id, nome, cpf, dataNascimento, telefone, email, endereco, senha, foto)
    {
    }

    public static Result<Aluno> Criar(
        int id,
        string nome,
        string cpf,
        DateOnly dataNascimento,
        string telefone,
        string? email,
        Logradouro? logradouro,
        string numero,
        string? complemento,
        string senha,
        Arquivo? foto)
    {
        var notifications = new List<Notification>();

        if (NormalizadoService.TextoVazioOuNulo(nome))
            notifications.Add(new Notification("Nome", "NOME_OBRIGATORIO"));
        else
            nome = NormalizadoService.LimparEspacos(nome);

        if (dataNascimento == default)
            notifications.Add(new Notification("DataNascimento", "DATA_NASCIMENTO_OBRIGATORIA"));
        else if (dataNascimento > DateOnly.FromDateTime(DateTime.Today))
            notifications.Add(new Notification("DataNascimento", "DATA_NASCIMENTO_FUTURA"));

        var cpfResult = Cpf.Criar(cpf);
        if (cpfResult.IsFailure) notifications.AddRange(cpfResult.Notifications);

        var telefoneResult = Telefone.Criar(telefone);
        if (telefoneResult.IsFailure) notifications.AddRange(telefoneResult.Notifications);

        Result<Email>? emailResult = null;
        if (!NormalizadoService.TextoVazioOuNulo(email))
        {
            emailResult = Email.Criar(email!);
            if (emailResult.IsFailure) notifications.AddRange(emailResult.Notifications);
        }

        var enderecoResult = Endereco.Criar(logradouro, numero, complemento);
        if (enderecoResult.IsFailure) notifications.AddRange(enderecoResult.Notifications);

        var senhaResult = Senha.Criar(senha);
        if (senhaResult.IsFailure) notifications.AddRange(senhaResult.Notifications);

        if (notifications.Count != 0)
            return Result<Aluno>.Failure(notifications);

        return Result<Aluno>.Success(new Aluno(
            id,
            nome,
            cpfResult.Value!,
            dataNascimento,
            telefoneResult.Value!,
            emailResult?.Value,
            enderecoResult.Value!,
            senhaResult.Value!,
            foto));
    }
}
