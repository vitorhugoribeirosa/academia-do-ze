// VITOR HUGO RIBEIRO SA
using AcademiaDoZe.Domain.Common;
using AcademiaDoZe.Domain.Entities;
using AcademiaDoZe.Domain.Exceptions;

namespace AcademiaDoZe.Domain.Tests.Common;

public class CommonTests
{
    private sealed class EntidadeTeste(int id) : Entity(id);

    [Theory(DisplayName = "Entity: aceita identificadores não negativos")]
    [InlineData(0)] [InlineData(1)] [InlineData(10)] [InlineData(int.MaxValue)]
    public void Deve_Criar_Entidade_Com_Id_NaoNegativo(int id) =>
        Assert.Equal(id, new EntidadeTeste(id).Id);

    [Theory(DisplayName = "Entity: rejeita identificadores negativos")]
    [InlineData(-1)] [InlineData(-10)] [InlineData(int.MinValue)]
    public void Deve_Lancar_Excecao_Para_Id_Negativo(int id) =>
        Assert.Equal("ID_NEGATIVO", Assert.Throws<DomainException>(() => new EntidadeTeste(id)).Message);

    [Theory(DisplayName = "Result: sucesso armazena valores")]
    [InlineData(1)] [InlineData(2)] [InlineData(100)] [InlineData(-5)]
    public void Deve_Criar_Result_De_Sucesso(int valor)
    {
        var r = Result<int>.Success(valor);
        Assert.True(r.IsSuccess); Assert.False(r.IsFailure); Assert.Equal(valor, r.Value); Assert.Empty(r.Notifications);
    }

    [Theory(DisplayName = "Result: falha armazena notificação")]
    [InlineData("Campo", "ERRO")] [InlineData("Nome", "NOME_OBRIGATORIO")]
    [InlineData("Cpf", "CPF_INVALIDO")] [InlineData("Senha", "SENHA_FORMATO")]
    public void Deve_Criar_Result_De_Falha(string propriedade, string mensagem)
    {
        var r = Result<int>.Failure(propriedade, mensagem);
        Assert.True(r.IsFailure); Assert.False(r.IsSuccess); Assert.Contains(r.Notifications, n => n.Propriedade == propriedade && n.Mensagem == mensagem);
    }
}
