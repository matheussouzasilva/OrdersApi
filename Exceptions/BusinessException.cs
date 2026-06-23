namespace OrdersApi.Exceptions;

#region 🏛️ Exceção base de domínio: BusinessException
/*
 Por que criar uma exceção customizada em vez de usar Exception?
 ──────────────────────────────────────────────────────────────
 Exception é genérica demais. Ao criar BusinessException, estamos
 dizendo claramente: "esta falha vem de uma regra de negócio,
 não de um bug ou falha de infraestrutura."

 Isso é importante porque o middleware precisa distinguir:
   → Erro de domínio (BusinessException) → resposta ao cliente (4xx)
   → Erro inesperado (Exception)         → bug interno (500)

 Por que as outras exceções herdam desta?
 ────────────────────────────────────────
 Hierarquia de herança nos dá flexibilidade no middleware:
   → Podemos tratar NotFoundException e ValidationException individualmente
   → Podemos tratar "qualquer erro de negócio" via BusinessException
   → Qualquer nova exceção de domínio criada no futuro automaticamente
     herda o comportamento base

 Isso é o princípio Open/Closed (OCP) do SOLID:
 aberto para extensão (novas exceções), fechado para modificação
 (o middleware não precisa mudar para cada nova exceção).

 Separation of Concerns:
 As exceções pertencem à camada de domínio.
 Quem decide o status HTTP correspondente é o middleware (camada HTTP).
 As duas camadas não se conhecem diretamente.
*/
#endregion
public class BusinessException : Exception
{
    #region 🏗️ Construtor
    /*
     Passamos a mensagem para a classe base (Exception).
     Dessa forma, ex.Message funciona normalmente em todo o código,
     incluindo no middleware ao montar a resposta de erro.
    */
    public BusinessException(string message) : base(message)
    {
    }
    #endregion
}
