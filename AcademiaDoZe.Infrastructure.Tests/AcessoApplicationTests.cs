using AcademiaDoZe.Application.DependencyInjection;
using AcademiaDoZe.Application.DTOs;
using AcademiaDoZe.Application.Interfaces;
using AcademiaDoZe.Application.Mappings;
using AcademiaDoZe.Application.Services;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoApplicationTests
{
    [Fact(DisplayName = "Application: resolve serviços de acesso pela injeção")]
    public void DependencyInjection_Deve_Resolver_Servicos_De_Acesso()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new RepositoryConfig
        {
            ConnectionString = "Data Source=:memory:",
            DatabaseType = DatabaseType.Sqlite
        });
        services.AddApplicationServices();

        using var provider = services.BuildServiceProvider();

        Assert.IsType<AcessoAlunoService>(
            provider.GetRequiredService<IAcessoAlunoService>());
        Assert.IsType<AcessoColaboradorService>(
            provider.GetRequiredService<IAcessoColaboradorService>());
    }

    [Fact(DisplayName = "Application: gerencia DTO de acesso do aluno")]
    public async Task AcessoAlunoService_Deve_Gerenciar_Dto()
    {
        var banco = CriarCaminhoTemporario();
        var connectionString = $"Data Source={banco};Pooling=False;";

        try
        {
            await using var logradouroRepository =
                new LogradouroRepository(connectionString, DatabaseType.Sqlite);
            await using var alunoRepository =
                new AlunoRepository(connectionString, DatabaseType.Sqlite);
            await using var acessoRepository =
                new AcessoAlunoRepository(connectionString, DatabaseType.Sqlite);
            await using var matriculaRepository =
                new MatriculaRepository(connectionString, DatabaseType.Sqlite);

            var logradouro = await logradouroRepository.Adicionar(CriarLogradouro());
            var aluno = await alunoRepository.Adicionar(CriarAluno(logradouro));
            var service = new AcessoAlunoService(
                () => new AcessoAlunoRepository(connectionString, DatabaseType.Sqlite),
                () => alunoRepository,
                () => matriculaRepository);
            var entrada = DateTime.Today.AddHours(10);

            var semMatricula = await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegistrarEntradaAsync(aluno.Id, entrada));
            Assert.Contains("matrícula ativa", semMatricula.Message);

            await matriculaRepository.Adicionar(Matricula.Criar(
                0,
                aluno,
                MatriculaPlano.Mensal,
                DateOnly.FromDateTime(DateTime.Today),
                "Condicionamento físico",
                MatriculaRestricoes.None,
                null).Value!);

            var adicionado = await service.RegistrarEntradaAsync(aluno.Id, entrada);

            Assert.True(adicionado.Id > 0);
            Assert.Equal(aluno.Nome, adicionado.AlunoAcesso.Nome);
            Assert.True(adicionado.EstaEmAberto);
            Assert.NotNull(adicionado.DataFimPlano);
            Assert.True(adicionado.DiasRestantesPlano > 0);
            Assert.True(await service.EstaNaAcademiaAsync(aluno.Id));

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegistrarEntradaAsync(aluno.Id, entrada.AddMinutes(1)));

            var atualizado = await service.RegistrarSaidaAsync(
                aluno.Id, entrada.AddHours(2));

            Assert.False(atualizado.EstaEmAberto);
            Assert.Equal(TimeSpan.FromHours(2), atualizado.Permanencia);
            Assert.False(await service.EstaNaAcademiaAsync(aluno.Id));
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.RegistrarSaidaAsync(aluno.Id, entrada.AddHours(3)));
            Assert.Equal(atualizado.Id, (await service.ObterPorIdAsync(atualizado.Id))?.Id);
            Assert.Single(await service.ObterPorAlunoPeriodoAsync(
                aluno.Id,
                DateOnly.FromDateTime(entrada),
                DateOnly.FromDateTime(entrada)));
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ObterPorAlunoPeriodoAsync(
                    aluno.Id,
                    DateOnly.FromDateTime(entrada),
                    DateOnly.FromDateTime(entrada).AddDays(-1)));

            var novaEntrada = await service.RegistrarMovimentacaoAsync(
                aluno.Id, entrada.AddHours(3));
            Assert.True(novaEntrada.EstaEmAberto);
        }
        finally
        {
            ExcluirBancoTemporario(banco);
        }
    }

    [Fact(DisplayName = "Application: gerencia DTO de acesso do colaborador")]
    public async Task AcessoColaboradorService_Deve_Gerenciar_Dto()
    {
        var banco = CriarCaminhoTemporario();
        var connectionString = $"Data Source={banco};Pooling=False;";

        try
        {
            await using var logradouroRepository =
                new LogradouroRepository(connectionString, DatabaseType.Sqlite);
            await using var colaboradorRepository =
                new ColaboradorRepository(connectionString, DatabaseType.Sqlite);
            await using var acessoRepository =
                new AcessoColaboradorRepository(connectionString, DatabaseType.Sqlite);

            var logradouro = await logradouroRepository.Adicionar(CriarLogradouro());
            var colaborador = await colaboradorRepository.Adicionar(
                CriarColaborador(logradouro));
            var service = new AcessoColaboradorService(
                () => new AcessoColaboradorRepository(connectionString, DatabaseType.Sqlite),
                () => colaboradorRepository);
            var entrada = DateTime.Today.AddHours(9);

            var adicionado = await service.RegistrarEntradaAsync(
                colaborador.Id, entrada);

            Assert.Equal(colaborador.Nome, adicionado.ColaboradorAcesso.Nome);
            Assert.True(adicionado.EstaEmAberto);
            Assert.True(await service.EstaNaAcademiaAsync(colaborador.Id));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RegistrarEntradaAsync(colaborador.Id, entrada.AddMinutes(1)));

            var saida = await service.RegistrarMovimentacaoAsync(
                colaborador.Id, entrada.AddHours(3));
            Assert.False(saida.EstaEmAberto);
            Assert.False(await service.EstaNaAcademiaAsync(colaborador.Id));
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RegistrarSaidaAsync(colaborador.Id, entrada.AddHours(4)));
            Assert.Equal(
                TimeSpan.FromHours(3),
                await service.ObterHorasTrabalhadasNoDiaAsync(
                    colaborador.Id, DateOnly.FromDateTime(entrada)));
            Assert.Equal(TimeSpan.FromHours(3), saida.PermanenciaTotalDia);
            Assert.Equal(TimeSpan.FromHours(8), saida.LimiteJornada);
            Assert.Equal(
                saida.Id,
                (await service.ObterUltimoAcessoAsync(colaborador.Id))?.Id);

            var complemento = AcessoColaborador.Criar(
                0, colaborador, entrada.AddHours(4)).Value!;
            complemento.RegistrarSaida(colaborador, entrada.AddHours(9));
            await acessoRepository.Adicionar(complemento);

            var limite = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.RegistrarEntradaAsync(colaborador.Id, entrada.AddHours(10)));
            Assert.Contains("limite diário de 8 horas", limite.Message);
        }
        finally
        {
            ExcluirBancoTemporario(banco);
        }
    }

    private static Logradouro CriarLogradouro() => Logradouro.Criar(
        0, "12345678", "Rua Teste", "Centro", "Lages", "SC", "Brasil").Value!;

    private static Aluno CriarAluno(Logradouro logradouro) => Aluno.Criar(
        0,
        "Aluno Application Completo",
        "52998224725",
        new DateOnly(1998, 8, 15),
        "49991234567",
        "aluno.application@example.com",
        logradouro,
        "123",
        string.Empty,
        "Abcdef",
        Arquivo.Criar([1, 2, 3]).Value!).Value!;

    private static Colaborador CriarColaborador(Logradouro logradouro) =>
        Colaborador.Criar(
            0,
            "Colaborador Application Completo",
            "52998224725",
            new DateOnly(1990, 5, 10),
            "49991234567",
            "colaborador.application@example.com",
            logradouro,
            "123",
            string.Empty,
            "Abcdef",
            Arquivo.Criar([1, 2, 3]).Value!,
            new DateOnly(2020, 1, 10),
            ColaboradorTipo.Atendente,
            ColaboradorVinculo.CLT).Value!;

    private static string CriarCaminhoTemporario() => Path.Combine(
        Path.GetTempPath(), $"academia_application_{Guid.NewGuid():N}.db");

    private static void ExcluirBancoTemporario(string banco)
    {
        foreach (var caminho in new[] { banco, banco + "-wal", banco + "-shm" })
        {
            if (File.Exists(caminho))
                File.Delete(caminho);
        }
    }
}
