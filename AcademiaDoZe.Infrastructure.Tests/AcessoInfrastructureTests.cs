using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;
using AcademiaDoZe.Infrastructure.Data;
using AcademiaDoZe.Infrastructure.Exceptions;
using AcademiaDoZe.Infrastructure.Repositories;

namespace AcademiaDoZe.Infrastructure.Tests;

public class AcessoInfrastructureTests
{
    [Fact(DisplayName = "Acesso de aluno: fluxo completo no repositório")]
    public async Task AcessoAluno_Deve_Executar_Fluxo_Completo()
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

            var logradouro = await logradouroRepository.Adicionar(CriarLogradouro());
            var aluno = await alunoRepository.Adicionar(CriarAluno(logradouro));
            var alunoSemAcesso = await alunoRepository.Adicionar(
                CriarAluno(logradouro, "11144477735", "semacesso@example.com"));
            var entrada = DateTime.Today.AddHours(10);
            var acesso = AcessoAluno.Criar(0, aluno, entrada).Value!;

            await acessoRepository.Adicionar(acesso);

            Assert.True(acesso.Id > 0);
            Assert.True(await acessoRepository.EstaNaAcademia(aluno.Id));
            var salvo = await acessoRepository.ObterPorId(acesso.Id);
            Assert.NotNull(salvo);
            Assert.Equal(entrada, salvo.DataHoraEntrada);
            Assert.Null(salvo.DataHoraSaida);

            var saida = entrada.AddHours(2);
            acesso.RegistrarSaida(aluno, saida);
            await acessoRepository.Atualizar(acesso);

            Assert.False(await acessoRepository.EstaNaAcademia(aluno.Id));
            var acessosDoDia = (await acessoRepository.ObterAcessosPorAlunoPeriodo(
                aluno.Id,
                DateOnly.FromDateTime(entrada),
                DateOnly.FromDateTime(entrada))).ToList();
            Assert.Single(acessosDoDia);
            Assert.Equal(saida, acessosDoDia[0].DataHoraSaida);

            var historicoGeralDoDia = (await acessoRepository.ObterAcessosPorAlunoPeriodo(
                null,
                DateOnly.FromDateTime(entrada),
                DateOnly.FromDateTime(entrada))).ToList();
            Assert.Single(historicoGeralDoDia);

            Assert.Single(await acessoRepository.ObterAcessosPorAlunoPeriodo());

            var horarios = await acessoRepository.ObterHorarioMaisProcuradoPorMes(
                entrada.Month);
            Assert.Equal(1, horarios[new TimeOnly(10, 0)]);

            var permanencia = await acessoRepository.ObterPermanenciaMediaPorMes(
                entrada.Month);
            Assert.Equal(TimeSpan.FromHours(2), permanencia[entrada.Day]);

            var semAcesso = (await acessoRepository.ObterAlunosSemAcessoNosUltimosDias(1))
                .ToList();
            Assert.Contains(semAcesso, item => item.Id == alunoSemAcesso.Id);
            Assert.DoesNotContain(semAcesso, item => item.Id == aluno.Id);

            var novoAcesso = AcessoAluno.Criar(
                0, aluno, entrada.AddHours(3)).Value!;
            await acessoRepository.Adicionar(novoAcesso);
            var ultimo = await acessoRepository.ObterUltimoAcesso(aluno.Id);
            Assert.Equal(novoAcesso.Id, ultimo?.Id);

            var duplicado = AcessoAluno.Criar(
                0, aluno, entrada.AddHours(4)).Value!;
            await Assert.ThrowsAsync<InfrastructureException>(
                () => acessoRepository.Adicionar(duplicado));

            Assert.True(await acessoRepository.Remover(novoAcesso.Id));
            Assert.False(await acessoRepository.Remover(novoAcesso.Id));
        }
        finally
        {
            ExcluirBancoTemporario(banco);
        }
    }

    [Fact(DisplayName = "Acesso de colaborador: soma permanência do dia")]
    public async Task AcessoColaborador_Deve_Somar_Permanencia_Do_Dia()
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
            var data = DateOnly.FromDateTime(DateTime.Today);

            await AdicionarAcessoFechadoAsync(
                acessoRepository, colaborador, DateTime.Today.AddHours(8), 2);
            await AdicionarAcessoFechadoAsync(
                acessoRepository, colaborador, DateTime.Today.AddHours(11), 3);

            var total = await acessoRepository.ObterHorasTrabalhadasNoDia(
                colaborador.Id, data);
            Assert.Equal(TimeSpan.FromHours(5), total);

            var acessos = (await acessoRepository.ObterAcessosPorColaboradorPeriodo(
                colaborador.Id, data, data)).ToList();
            Assert.Equal(2, acessos.Count);
            Assert.Equal(acessos[0].Id,
                (await acessoRepository.ObterUltimoAcesso(colaborador.Id))?.Id);
        }
        finally
        {
            ExcluirBancoTemporario(banco);
        }
    }

    private static async Task AdicionarAcessoFechadoAsync(
        AcessoColaboradorRepository repository,
        Colaborador colaborador,
        DateTime entrada,
        int horas)
    {
        var acesso = AcessoColaborador.Criar(0, colaborador, entrada).Value!;
        acesso.RegistrarSaida(colaborador, entrada.AddHours(horas));
        await repository.Adicionar(acesso);
    }

    private static Logradouro CriarLogradouro() => Logradouro.Criar(
        0, "12345678", "Rua Teste", "Centro", "Lages", "SC", "Brasil").Value!;

    private static Aluno CriarAluno(
        Logradouro logradouro,
        string cpf = "52998224725",
        string email = "aluno@example.com") =>
        Aluno.Criar(
            0,
            "Aluno Teste Completo",
            cpf,
            new DateOnly(1998, 8, 15),
            "49991234567",
            email,
            logradouro,
            "123",
            string.Empty,
            "Abcdef",
            Arquivo.Criar([1, 2, 3]).Value!).Value!;

    private static Colaborador CriarColaborador(Logradouro logradouro) =>
        Colaborador.Criar(
            0,
            "Colaborador Teste Completo",
            "52998224725",
            new DateOnly(1990, 5, 10),
            "49991234567",
            "colaborador@example.com",
            logradouro,
            "123",
            string.Empty,
            "Abcdef",
            Arquivo.Criar([1, 2, 3]).Value!,
            new DateOnly(2020, 1, 10),
            ColaboradorTipo.Atendente,
            ColaboradorVinculo.CLT).Value!;

    private static string CriarCaminhoTemporario() => Path.Combine(
        Path.GetTempPath(), $"academia_acesso_{Guid.NewGuid():N}.db");

    private static void ExcluirBancoTemporario(string banco)
    {
        foreach (var caminho in new[] { banco, banco + "-wal", banco + "-shm" })
        {
            if (File.Exists(caminho))
                File.Delete(caminho);
        }
    }
}
