// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class AcessoAlunoTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar([1, 2, 3]).Value!;
    private static Aluno GetValidAluno(int id = 1) => Aluno.Criar(id, "João", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo()).Value!;

    [Theory(DisplayName = "AcessoAluno: aluno nulo -> ALUNO_INVALIDO")]
    [InlineData(true)] [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_AlunoEhNulo(bool alunoNull)
    {
        var aluno = alunoNull ? null : GetValidAluno();
        var result = AcessoAluno.Criar(1, aluno!, DateTime.Today.AddHours(10));
        Assert.Equal(!alunoNull, result.IsSuccess);
        if (alunoNull) Assert.Contains(result.Notifications, n => n.Mensagem == "ALUNO_INVALIDO");
    }

    [Theory(DisplayName = "AcessoAluno: horario fora do intervalo -> DATAHORA_INTERVALO")]
    [InlineData(5, 59)] [InlineData(22, 1)]
    public void Deve_Falhar_Criacao_Quando_HorarioForaDoIntervalo(int hour, int minute)
    {
        var result = AcessoAluno.Criar(1, GetValidAluno(), DateTime.Today.AddHours(hour).AddMinutes(minute));
        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications, n => n.Mensagem == "DATA_HORA_INTERVALO_INVALIDO");
    }

    [Theory(DisplayName = "AcessoAluno: criação bem-sucedida em horários permitidos")]
    [InlineData(10)] [InlineData(11)]
    public void Deve_Criar_Com_Sucesso_Quando_HorarioValido(int hour)
    {
        var aluno = GetValidAluno(); var dataHora = DateTime.Today.AddHours(hour);
        var result = AcessoAluno.Criar(1, aluno, dataHora);
        Assert.True(result.IsSuccess);
        Assert.Equal(aluno.Id, result.Value!.AlunoId);
        Assert.Equal(dataHora, result.Value.DataHoraEntrada);
        Assert.Null(result.Value.DataHoraSaida);
        Assert.True(result.Value.EstaEmAberto);
    }

    [Theory(DisplayName = "AcessoAluno: permite horários de borda 06:00 e 22:00")]
    [InlineData(6)] [InlineData(22)]
    public void Deve_Permitir_HorariosDeBorda_06_00_e_22_00(int hour) =>
        Assert.True(AcessoAluno.Criar(1, GetValidAluno(), DateTime.Today.AddHours(hour)).IsSuccess);

    [Fact(DisplayName = "AcessoAluno: registra saída do mesmo aluno")]
    public void Deve_Registrar_Saida_Do_Mesmo_Aluno()
    {
        var aluno = GetValidAluno();
        var acesso = AcessoAluno.Criar(
            1, aluno, DateTime.Today.AddHours(10)).Value!;
        var dataHoraSaida = DateTime.Today.AddHours(12);

        var result = acesso.RegistrarSaida(aluno, dataHoraSaida);

        Assert.True(result.IsSuccess);
        Assert.Equal(dataHoraSaida, acesso.DataHoraSaida);
        Assert.False(acesso.EstaEmAberto);
    }

    [Fact(DisplayName = "AcessoAluno: rejeita saída de aluno diferente")]
    public void Deve_Falhar_Saida_Quando_AlunoForDiferente()
    {
        var acesso = AcessoAluno.Criar(
            1, GetValidAluno(1), DateTime.Today.AddHours(10)).Value!;

        var result = acesso.RegistrarSaida(
            GetValidAluno(2), DateTime.Today.AddHours(12));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications,
            n => n.Mensagem == "ALUNO_DIFERENTE_DA_ENTRADA");
        Assert.True(acesso.EstaEmAberto);
    }

    [Fact(DisplayName = "AcessoAluno: rejeita saída anterior à entrada")]
    public void Deve_Falhar_Saida_Quando_ForAnteriorAEntrada()
    {
        var aluno = GetValidAluno();
        var acesso = AcessoAluno.Criar(
            1, aluno, DateTime.Today.AddHours(10)).Value!;

        var result = acesso.RegistrarSaida(
            aluno, DateTime.Today.AddHours(9));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications,
            n => n.Mensagem == "SAIDA_ANTERIOR_A_ENTRADA");
        Assert.True(acesso.EstaEmAberto);
    }

    [Fact(DisplayName = "AcessoAluno: rejeita segunda saída")]
    public void Deve_Falhar_Quando_SaidaJaFoiRegistrada()
    {
        var aluno = GetValidAluno();
        var acesso = AcessoAluno.Criar(
            1, aluno, DateTime.Today.AddHours(10)).Value!;
        var primeiraSaida = DateTime.Today.AddHours(12);
        acesso.RegistrarSaida(aluno, primeiraSaida);

        var result = acesso.RegistrarSaida(
            aluno, DateTime.Today.AddHours(13));

        Assert.True(result.IsFailure);
        Assert.Contains(result.Notifications,
            n => n.Mensagem == "SAIDA_JA_REGISTRADA");
        Assert.Equal(primeiraSaida, acesso.DataHoraSaida);
    }
}
