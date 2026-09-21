using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Application.Mappings;

public static class ColaboradorMappingExtensions
{
    public static ColaboradorDto ToDto(this Colaborador colaborador, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(colaborador);

        return new ColaboradorDto
        {
            Id = colaborador.Id,
            Nome = colaborador.Nome,
            Cpf = colaborador.Cpf.Valor,
            DataNascimento = colaborador.DataNascimento,
            Telefone = colaborador.Telefone.Valor,
            Email = colaborador.Email.Valor,
            Endereco = logradouro?.ToDto(),
            Numero = colaborador.Endereco.Numero,
            Complemento = colaborador.Endereco.Complemento,
            Senha = null,
            Foto = new ArquivoDto { Conteudo = colaborador.Foto.Conteudo },
            DataAdmissao = colaborador.DataAdmissao,
            Tipo = colaborador.Tipo.ToApplication(),
            Vinculo = colaborador.Vinculo.ToApplication()
        };
    }

    public static Colaborador ToEntity(this ColaboradorDto colaboradorDto, Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(colaboradorDto);

        var logradouroEntidade = ObterLogradouro(colaboradorDto, logradouro);
        var foto = CriarArquivo(colaboradorDto.Foto?.Conteudo ?? []);
        var result = Colaborador.Criar(
            colaboradorDto.Id,
            colaboradorDto.Nome,
            colaboradorDto.Cpf,
            colaboradorDto.DataNascimento,
            colaboradorDto.Telefone,
            colaboradorDto.Email ?? string.Empty,
            logradouroEntidade,
            colaboradorDto.Numero,
            colaboradorDto.Complemento ?? string.Empty,
            colaboradorDto.Senha ?? string.Empty,
            foto,
            colaboradorDto.DataAdmissao,
            colaboradorDto.Tipo.ToDomain(),
            colaboradorDto.Vinculo.ToDomain());

        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação ao converter Colaborador: {FormatarErros(result.Notifications)}");

        return result.Value!;
    }

    public static Colaborador UpdateFromDto(
        this Colaborador colaborador,
        ColaboradorDto colaboradorDto,
        Logradouro? logradouro = null)
    {
        ArgumentNullException.ThrowIfNull(colaborador);
        ArgumentNullException.ThrowIfNull(colaboradorDto);

        var logradouroEntidade = ObterLogradouro(colaboradorDto, logradouro);
        var foto = colaboradorDto.Foto?.Conteudo != null
            ? CriarArquivo(colaboradorDto.Foto.Conteudo)
            : colaborador.Foto;
        var senha = string.IsNullOrWhiteSpace(colaboradorDto.Senha)
            ? colaborador.Senha.Valor
            : colaboradorDto.Senha;
        var result = Colaborador.Criar(
            colaborador.Id,
            colaboradorDto.Nome,
            colaborador.Cpf.Valor,
            colaboradorDto.DataNascimento,
            colaboradorDto.Telefone,
            colaboradorDto.Email ?? colaborador.Email.Valor,
            logradouroEntidade,
            colaboradorDto.Numero,
            colaboradorDto.Complemento ?? string.Empty,
            senha,
            foto,
            colaboradorDto.DataAdmissao,
            colaboradorDto.Tipo.ToDomain(),
            colaboradorDto.Vinculo.ToDomain());

        if (result.IsFailure)
            throw new InvalidOperationException($"Erro de validação ao atualizar Colaborador: {FormatarErros(result.Notifications)}");

        return result.Value!;
    }

    private static Logradouro ObterLogradouro(ColaboradorDto dto, Logradouro? logradouro) =>
        logradouro ?? dto.Endereco?.ToEntity()
        ?? throw new InvalidOperationException("Logradouro/Endereço é obrigatório para o Colaborador.");

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
