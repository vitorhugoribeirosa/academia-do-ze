// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Enums;
using AcademiaDoZe.Domain.ValueObjects;

namespace AcademiaDoZe.Domain.Tests.Entities;

public class MatriculaTests
{
    private static Logradouro GetValidLogradouro() => Logradouro.Criar(1, "12345-678", "Rua Teste", "Bairro", "Cidade", "SP", "Brasil").Value!;
    private static Arquivo GetValidArquivo() => Arquivo.Criar([1, 2, 3]).Value!;
    private static Aluno GetValidAluno(DateOnly? nascimento = null) => Aluno.Criar(1, "João da Silva", "529.982.247-25", nascimento ?? DateOnly.FromDateTime(DateTime.Today.AddYears(-20)), "(11) 91234-5678", "user@example.com", GetValidLogradouro(), "123", "", "Abcdef", GetValidArquivo()).Value!;

    [Theory] [InlineData(999)] [InlineData((int)MatriculaPlano.Mensal)]
    public void Deve_FalharOuPassar_Criacao_Quando_ValorDoPlano(int planoValue)
    {
        var aluno = GetValidAluno();
        var r = Matricula.Criar(1, aluno, (MatriculaPlano)planoValue, DateOnly.FromDateTime(DateTime.Today), "Objetivo", MatriculaRestricoes.None, null);
        Assert.Equal(planoValue != 999, r.IsSuccess);
        if (planoValue == 999) Assert.Contains(r.Notifications, n => n.Mensagem == "PLANO_INVALIDO");
        else Assert.Equal(aluno.Id, r.Value!.AlunoId);
    }

    [Theory] [InlineData(true)] [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_DataInicioPadrao(bool useDefault)
    {
        var r = Matricula.Criar(1, GetValidAluno(), MatriculaPlano.Mensal, useDefault ? default : DateOnly.FromDateTime(DateTime.Today), "Objetivo", MatriculaRestricoes.None, null);
        Assert.Equal(!useDefault, r.IsSuccess);
        if (useDefault) Assert.Contains(r.Notifications, n => n.Mensagem == "DATA_INICIO_OBRIGATORIO");
    }

    [Theory] [InlineData(MatriculaPlano.Mensal, 1)] [InlineData(MatriculaPlano.Trimestral, 3)]
    [InlineData(MatriculaPlano.Semestral, 6)] [InlineData(MatriculaPlano.Anual, 12)]
    public void Deve_Calcular_DataFim_Corretamente(MatriculaPlano plano, int meses)
    {
        var inicio = DateOnly.FromDateTime(DateTime.Today);
        var r = Matricula.Criar(1, GetValidAluno(), plano, inicio, "Objetivo", MatriculaRestricoes.None, null);
        Assert.True(r.IsSuccess);
        Assert.Equal(inicio.AddMonths(meses), r.Value!.DataFim);
    }

    [Theory] [InlineData(MatriculaRestricoes.None, true)] [InlineData(MatriculaRestricoes.Diabetes, true)]
    [InlineData(MatriculaRestricoes.Diabetes | MatriculaRestricoes.Alergias, true)]
    public void Deve_Tratar_Restricoes_ComOuSemLaudo(MatriculaRestricoes restricoes, bool expectSuccess)
    {
        var laudo = restricoes == MatriculaRestricoes.None ? null : GetValidArquivo();
        var obs = restricoes == MatriculaRestricoes.None ? null : "Observacoes";
        var r = Matricula.Criar(1, GetValidAluno(), MatriculaPlano.Mensal, DateOnly.FromDateTime(DateTime.Today), "Objetivo", restricoes, laudo, obs!);
        Assert.Equal(expectSuccess, r.IsSuccess);
    }

    [Theory] [InlineData(15, true)] [InlineData(20, false)]
    public void Deve_Falhar_Criacao_Quando_Menor16_ExigeLaudo(int age, bool expectFailure)
    {
        var r = Matricula.Criar(1, GetValidAluno(DateOnly.FromDateTime(DateTime.Today.AddYears(-age))), MatriculaPlano.Mensal, DateOnly.FromDateTime(DateTime.Today), "Melhorar condicionamento", MatriculaRestricoes.None, null);
        Assert.Equal(expectFailure, r.IsFailure);
        if (expectFailure) Assert.Contains(r.Notifications, n => n.Mensagem == "MENOR_16_LAUDO_OBRIGATORIO");
    }

    [Theory] [InlineData(MatriculaPlano.Mensal, 1)] [InlineData(MatriculaPlano.Trimestral, 3)]
    public void Deve_Criar_Com_Sucesso_E_Calcular_DataFim(MatriculaPlano plano, int meses)
    {
        var inicio = DateOnly.FromDateTime(DateTime.Today);
        var r = Matricula.Criar(1, GetValidAluno(), plano, inicio, "Melhorar condicionamento", MatriculaRestricoes.None, null);
        Assert.True(r.IsSuccess);
        Assert.Equal(inicio.AddMonths(meses), r.Value!.DataFim);
    }

    [Theory] [InlineData(true)] [InlineData(false)]
    public void Deve_Falhar_Criacao_Quando_RestricoesSemLaudo(bool provideLaudo)
    {
        var r = Matricula.Criar(1, GetValidAluno(), MatriculaPlano.Mensal, DateOnly.FromDateTime(DateTime.Today), "Objetivo", MatriculaRestricoes.Diabetes, provideLaudo ? GetValidArquivo() : null, provideLaudo ? "obs" : "");
        Assert.Equal(provideLaudo, r.IsSuccess);
        if (!provideLaudo) Assert.Contains(r.Notifications, n => n.Mensagem == "RESTRICOES_LAUDO_OBRIGATORIO");
    }

    [Theory] [InlineData("  observa  testo  ", "observa testo")] [InlineData(" obs  outro ", "obs outro")]
    public void Deve_Normalizar_ObservacoesRestricoes_Quando_InputTemEspacosExtras(string input, string expected)
    {
        var r = Matricula.Criar(1, GetValidAluno(), MatriculaPlano.Mensal, DateOnly.FromDateTime(DateTime.Today), "Objetivo", MatriculaRestricoes.Diabetes, GetValidArquivo(), input);
        Assert.True(r.IsSuccess);
        Assert.Equal(expected, r.Value!.ObservacoesRestricoes);
    }
}
