using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Application.Mappings;

public static class AlunoMappingExtensions
{
    public static AlunoDto ToDto(this Aluno aluno, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(aluno);

        return new AlunoDto
        {
            Id = aluno.Id,
            Nome = aluno.Nome,
            Cpf = aluno.Cpf.Valor,
            DataNascimento = aluno.DataNascimento,
            Telefone = aluno.Telefone.Valor,
            Email = aluno.Email.Valor,
            Endereco = logradouro?.ToDto(),
            Numero = aluno.Endereco.Numero,
            Complemento = aluno.Endereco.Complemento,
            Senha = null,
            Foto = new ArquivoDto { Conteudo = aluno.Foto.Conteudo }
        };
    }

    public static Aluno ToEntity(this AlunoDto alunoDto, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(alunoDto);

        var logradouroEntidade = ObterLogradouro(alunoDto, logradouro);
        var foto = CriarArquivo(alunoDto.Foto?.Conteudo ?? []);
        var result = Aluno.Criar(
            alunoDto.Id,
            alunoDto.Nome,
            alunoDto.Cpf,
            alunoDto.DataNascimento,
            alunoDto.Telefone,
            alunoDto.Email ?? string.Empty,
            logradouroEntidade,
            alunoDto.Numero,
            alunoDto.Complemento ?? string.Empty,
            alunoDto.Senha ?? string.Empty,
            foto);

        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação ao converter Aluno: {FormatarErros(result.Notifications)}");

        return result.Value!;
    }

    public static Aluno UpdateFromDto(this Aluno aluno, AlunoDto alunoDto, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(aluno);
        ArgumentNullException.ThrowIfNull(alunoDto);

        var logradouroEntidade = ObterLogradouro(alunoDto, logradouro);
        var foto = alunoDto.Foto?.Conteudo != null ? CriarArquivo(alunoDto.Foto.Conteudo) : aluno.Foto;
        var senha = string.IsNullOrWhiteSpace(alunoDto.Senha) ? aluno.Senha.Valor : alunoDto.Senha;
        var result = Aluno.Criar(
            aluno.Id,
            alunoDto.Nome,
            aluno.Cpf.Valor,
            alunoDto.DataNascimento,
            alunoDto.Telefone,
            alunoDto.Email ?? aluno.Email.Valor,
            logradouroEntidade,
            alunoDto.Numero,
            alunoDto.Complemento ?? string.Empty,
            senha,
            foto);

        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação ao atualizar Aluno: {FormatarErros(result.Notifications)}");

        return result.Value!;
    }

    private static Logradouro ObterLogradouro(AlunoDto alunoDto, Logradouro? logradouro) =>
        logradouro ?? alunoDto.Endereco?.ToEntity()
        ?? throw new InvalidOperationException("Logradouro/Endereço é obrigatório para o Aluno.");

    private static Arquivo CriarArquivo(byte[] conteudo)
    {
        var result = Arquivo.Criar(conteudo);
        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação do arquivo: {FormatarErros(result.Notifications)}");
        return result.Value!;
    }

    private static string FormatarErros(IEnumerable<Domain.Common.Notification> notifications) =>
        string.Join(", ", notifications.Select(notification => notification.Mensagem));
}
