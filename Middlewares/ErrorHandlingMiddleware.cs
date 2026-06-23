using System.Net;
using System.Text.Json;

namespace OrdersApi.Middlewares;

#region 🛡️ Middleware global de tratamento de exceções
/*
 O que é um Middleware?
 ─────────────────────
 Middleware é um componente que fica no meio do pipeline HTTP.
 Cada requisição passa por uma "cadeia" de middlewares antes
 de chegar no controller, e a resposta passa pela mesma cadeia
 no caminho de volta.

 Ordem de execução (pipeline):
   Request → [ErrorHandling] → [Authorization] → Controller → ...
   Response ←        ←               ←               ←

 Por que centralizar o tratamento de exceções aqui?
 ──────────────────────────────────────────────────
 Sem esse middleware, cada controller precisaria de try/catch próprio.
 Isso viola o princípio DRY (Don't Repeat Yourself) e dificulta
 manutenção. Ao centralizar, qualquer exceção lançada em qualquer
 camada da aplicação é capturada e tratada de forma padronizada.

 Isso também respeita o princípio de Single Responsibility (SRP):
 o controller cuida apenas de orquestrar a requisição,
 o middleware cuida do tratamento de erros.
*/
#endregion
public class ErrorHandlingMiddleware
{
    #region 🔗 Referência ao próximo middleware da cadeia
    /*
     RequestDelegate representa o próximo passo do pipeline.
     Ao chamar _next(context), passamos a requisição adiante.
     Se algum middleware ou controller lançar uma exceção,
     ela "sobe" a cadeia e é capturada pelo nosso try/catch.
    */
    private readonly RequestDelegate _next;
    #endregion

    #region 🏗️ Construtor
    /*
     O ASP.NET injeta automaticamente o RequestDelegate aqui.
     Isso é Dependency Injection funcionando no nível do middleware.
    */
    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    #endregion

    #region ⚙️ Método principal: processa cada requisição
    /*
     InvokeAsync é chamado pelo ASP.NET para cada requisição HTTP.
     Ele "envolve" toda a cadeia de middlewares e controllers
     em um try/catch centralizado.

     Fluxo:
       1. Tenta executar o restante do pipeline (await _next(context))
       2. Se der certo → resposta segue normalmente
       3. Se lançar exceção → capturamos, classificamos e respondemos
    */
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            #region ▶️ Passa para o próximo middleware / controller
            await _next(context);
            #endregion
        }

        #region 🔴 Exceção de validação de negócio → 400 Bad Request
        /*
         ArgumentException é lançada quando os dados de entrada
         violam uma regra de negócio (ex: TotalAmount <= 0).

         HTTP 400 Bad Request:
         → O cliente enviou dados inválidos.
         → É responsabilidade do cliente corrigir a requisição.

         ATENÇÃO: Na Fase 3, substituiremos ArgumentException
         por uma exceção customizada (BusinessException ou ValidationException).
        */
        catch (ArgumentException ex)
        {
            await RespondWithErrorAsync(context, ex, StatusCodes.Status400BadRequest);
        }
        #endregion

        #region 🔴 Recurso não encontrado → 404 Not Found
        /*
         KeyNotFoundException é lançada quando buscamos um recurso
         que não existe (ex: pedido com ID inexistente).

         HTTP 404 Not Found:
         → O recurso identificado na URL não existe no servidor.

         ATENÇÃO: Na Fase 3, substituiremos KeyNotFoundException
         por uma exceção customizada (NotFoundException).
        */
        catch (KeyNotFoundException ex)
        {
            await RespondWithErrorAsync(context, ex, StatusCodes.Status404NotFound);
        }
        #endregion

        #region 🔴 Erro inesperado → 500 Internal Server Error
        /*
         Captura qualquer exceção não tratada pelos catches anteriores.

         HTTP 500 Internal Server Error:
         → Algo deu errado no servidor que o cliente não pode corrigir.
         → Em produção, evite expor ex.Message diretamente
           pois pode vazar detalhes internos.
        */
        catch (Exception ex)
        {
            await RespondWithErrorAsync(context, ex, StatusCodes.Status500InternalServerError);
        }
        #endregion
    }
    #endregion

    #region 📤 Método auxiliar: formata e envia a resposta de erro
    /*
     Centraliza a formatação da resposta de erro em um único lugar.
     Todos os catches chamam este método, garantindo que a estrutura
     do JSON de erro seja sempre a mesma — padronização de contrato.

     Exemplo de resposta:
     {
       "error": "TotalAmount must be greater than zero",
       "type": "ArgumentException"
     }
    */
    private static async Task RespondWithErrorAsync(HttpContext context, Exception ex, int statusCode)
    {
        #region 📝 Configuração do status e content-type da resposta
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        #endregion

        #region 🧱 Montagem do objeto de erro padronizado
        var errorResponse = new
        {
            error = ex.Message,
            type  = ex.GetType().Name
        };
        #endregion

        #region 📤 Serialização e escrita na resposta HTTP
        await context.Response.WriteAsJsonAsync(errorResponse);
        #endregion
    }
    #endregion
}
