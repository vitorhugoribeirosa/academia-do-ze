// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class AcessoColaboradorTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar([1, 2, 3]).Value!;
    private static Colaborador GetValidColaborador() => Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), ColaboradorTipo.Atendente, ColaboradorVinculo.CLT).Value!;

    [Theory(DisplayName = "AcessoColaborador: colaborador nulo -> COLABORADOR_INVALIDO")]
    [InlineData(true)] [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_ColaboradorEhNulo(bool colaboradorNull)
    {
        var colaborador = colaboradorNull ? null : GetValidColaborador();
        var result = AcessoColaborador.Criar(1, colaborador!, DateTime.Today.AddHours(10));
        Assert.Equal(!colaboradorNull, result.IsSuccess);
        if (colaboradorNull) Assert.Contains(result.Notifications, n => n.Mensagem == "COLABORADOR_INVALIDO");
    }

    [Theory(DisplayName = "AcessoColaborador: horario fora do intervalo -> DATAHORA_INTERVALO")]
    [InlineData(5, 59)] [InlineData(22, 1)]
    public void Deve_Falhar_Criacao_Quando_HorarioForaDoIntervalo(int hour, int minute)
    {
        var result = AcessoColaborador.Criar(1, GetValidColaborador(), DateTime.Today.AddHours(hour).AddMinutes(minute));
        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO");
    }

    [Theory(DisplayName = "AcessoColaborador: criação bem-sucedida em horários permitidos")]
    [InlineData(10)] [InlineData(14)]
    public void Deve_Criar_Com_Sucesso_Quando_HorarioValido(int hour)
    {
        var colaborador = GetValidColaborador(); var dataHora = DateTime.Today.AddHours(hour);
        var result = AcessoColaborador.Criar(1, colaborador, dataHora);
        Assert.True(result.IsSuccess); Assert.Equal(colaborador.Id, result.Value!.ColaboradorId); Assert.Equal(dataHora, result.Value.DataHora);
    }

    [Theory(DisplayName = "AcessoColaborador: permite horários de borda 06:00 e 22:00")]
    [InlineData(6)] [InlineData(22)]
    public void Deve_Permitir_HorariosDeBorda_06_00_e_22_00(int hour) =>
        Assert.True(AcessoColaborador.Criar(1, GetValidColaborador(), DateTime.Today.AddHours(hour)).IsSuccess);
}
