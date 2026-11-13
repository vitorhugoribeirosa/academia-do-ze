// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class AcessoColaboradorTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar([1, 2, 3]).Value!;
    private static Colaborador GetValidColaborador(int id = 1) => Colaborador.Criar(id, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), ColaboradorTipo.Atendente, ColaboradorVinculo.CLT).Value!;

    [Theory(DisplayName = "AcessoColaborador: colaborador nulo -> COLABORADOR_INVALIDO")]
    [InlineData(true)] [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_ColaboradorEhNulo(bool colaboradorNull)
    {
        var colaborador = colaboradorNull ? null : GetValidColaborador();
        var result = AcessoColaborador.Criar(1, colaborador!, DateTime.Today.AddHours(10));
        Assert.Equal(!colaboradorNull, result.IsSuccess);
        if (colaboradorNull) Assert.Contains(result.Notifications, n => n.Mensagem == "COLABORADOR_INVALIDO");
    }

    [Theory(DisplayName = "AcessoColaborador: permite registro em qualquer horário")]
    [InlineData(0, 0)] [InlineData(5, 59)] [InlineData(22, 1)] [InlineData(23, 59)]
    public void Deve_Permitir_Criacao_Em_Qualquer_Horario(int hour, int minute)
    {
        var result = AcessoColaborador.Criar(1, GetValidColaborador(), DateTime.Today.AddHours(hour).AddMinutes(minute));
        Assert.True(result.IsSuccess);
    }

    [Theory(DisplayName = "AcessoColaborador: criação bem-sucedida em horários permitidos")]
    [InlineData(10)] [InlineData(14)]
    public void Deve_Criar_Com_Sucesso_Quando_HorarioValido(int hour)
    {
        var colaborador = GetValidColaborador(); var dataHora = DateTime.Today.AddHours(hour);
        var result = AcessoColaborador.Criar(1, colaborador, dataHora);
        Assert.True(result.IsSuccess);
        Assert.Equal(colaborador.Id, result.Value!.ColaboradorId);
        Assert.Equal(dataHora, result.Value.DataHoraEntrada);
        Assert.Null(result.Value.DataHoraSaida);
        Assert.True(result.Value.EstaEmAberto);
    }

    [Fact(DisplayName = "AcessoColaborador: registra saída do mesmo colaborador")]
    public void Deve_Registrar_Saida_Do_Mesmo_Colaborador()
    {
        var colaborador = GetValidColaborador();
        var acesso = AcessoColaborador.Criar(
            1, colaborador, DateTime.Today.AddHours(10)).Value!;
        var dataHoraSaida = DateTime.Today.AddHours(12);

        var result = acesso.RegistrarSaida(colaborador, dataHoraSaida);

        Assert.True(result.IsSuccess);
        Assert.Equal(dataHoraSaida, acesso.DataHoraSaida);
        Assert.False(acesso.EstaEmAberto);
    }

    [Fact(DisplayName = "AcessoColaborador: rejeita saída de colaborador diferente")]
    public void Deve_Falhar_Saida_Quando_ColaboradorForDiferente()
    {
        var acesso = AcessoColaborador.Criar(
            1, GetValidColaborador(1), DateTime.Today.AddHours(10)).Value!;

        var result = acesso.RegistrarSaida(
            GetValidColaborador(2), DateTime.Today.AddHours(12));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications,
            n => n.Mensagem == "COLABORADOR_DIFERENTE_DA_ENTRADA");
        Assert.True(acesso.EstaEmAberto);
    }

    [Fact(DisplayName = "AcessoColaborador: rejeita saída anterior à entrada")]
    public void Deve_Falhar_Saida_Quando_ForAnteriorAEntrada()
    {
        var colaborador = GetValidColaborador();
        var acesso = AcessoColaborador.Criar(
            1, colaborador, DateTime.Today.AddHours(10)).Value!;

        var result = acesso.RegistrarSaida(
            colaborador, DateTime.Today.AddHours(9));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications,
            n => n.Mensagem == "SAIDA_ANTERIOR_A_ENTRADA");
        Assert.True(acesso.EstaEmAberto);
    }

    [Fact(DisplayName = "AcessoColaborador: rejeita segunda saída")]
    public void Deve_Falhar_Quando_SaidaJaFoiRegistrada()
    {
        var colaborador = GetValidColaborador();
        var acesso = AcessoColaborador.Criar(
            1, colaborador, DateTime.Today.AddHours(10)).Value!;
        var primeiraSaida = DateTime.Today.AddHours(12);
        acesso.RegistrarSaida(colaborador, primeiraSaida);

        var result = acesso.RegistrarSaida(
            colaborador, DateTime.Today.AddHours(13));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications,
            n => n.Mensagem == "SAIDA_JA_REGISTRADA");
        Assert.Equal(primeiraSaida, acesso.DataHoraSaida);
    }
}
