// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class ColaboradorTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar([1, 2, 3]).Value!;
    private static ResultWrapper Criar(DateOnly admissao, ColaboradorTipo tipo, ColaboradorVinculo vinculo)
    {
        var r = Colaborador.Criar(1, "Fulano", "529.982.247-25", DateOnly.FromDateTime(DateTime.Today.AddYears(-30)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo(), admissao, tipo, vinculo);
        return new(r.IsSuccess, r.IsFailure, r.Notifications);
    }
    private record ResultWrapper(bool IsSuccess, bool IsFailure, IReadOnlyCollection<AcademiaDoZe.Domain.Common.Notification> Notifications);

    [Theory(DisplayName = "Colaborador: data admissao obrigatória -> DATA_ADMISSAO_OBRIGATORIO")]
    [InlineData(true)] [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_DataAdmissaoPadrao(bool useDefault)
    {
        var r = Criar(useDefault ? default : DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), ColaboradorTipo.Atendente, ColaboradorVinculo.CLT);
        Assert.Equal(!useDefault, r.IsSuccess);
        if (useDefault) Assert.Contains(r.Notifications, n => n.Mensagem == "DATA_ADMISSAO_OBRIGATORIO");
    }

    [Theory(DisplayName = "Colaborador: Administrador com vinculo inválido -> ADMINISTRADOR_CLT_INVALIDO")]
    [InlineData(ColaboradorTipo.Administrador, ColaboradorVinculo.Estagio)]
    [InlineData(ColaboradorTipo.Administrador, ColaboradorVinculo.CLT)]
    public void Deve_Falhar_Criacao_Quando_AdminComVinculoInvalido(ColaboradorTipo tipo, ColaboradorVinculo vinc)
    {
        var r = Criar(DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), tipo, vinc);
        Assert.Equal(vinc == ColaboradorVinculo.CLT, r.IsSuccess);
        if (vinc == ColaboradorVinculo.Estagio) Assert.Contains(r.Notifications, n => n.Mensagem == "ADMINISTRADOR_CLT_INVALIDO");
    }

    [Theory(DisplayName = "Colaborador: data admissao futura -> DATA_ADMISSAO_MAIOR_ATUAL")]
    [InlineData(1)] [InlineData(-1)]
    public void Deve_Falhar_Criacao_Quando_DataAdmissaoFutura(int daysOffset)
    {
        var r = Criar(DateOnly.FromDateTime(DateTime.Today.AddDays(daysOffset)), ColaboradorTipo.Atendente, ColaboradorVinculo.CLT);
        Assert.Equal(daysOffset <= 0, r.IsSuccess);
        if (daysOffset > 0) Assert.Contains(r.Notifications, n => n.Mensagem == "DATA_ADMISSAO_MAIOR_QUE_ATUAL");
    }

    [Theory(DisplayName = "Colaborador: tipo ou vínculo inválido -> valida enum inválido")]
    [InlineData(999, 1)] [InlineData(1, 999)]
    public void Deve_Falhar_Criacao_Quando_TipoOuVinculoInvalido(int tipoValue, int vincValue)
    {
        var r = Criar(DateOnly.FromDateTime(DateTime.Today.AddYears(-1)), (ColaboradorTipo)tipoValue, (ColaboradorVinculo)vincValue);
        Assert.True(r.IsFailure);
    }
}
