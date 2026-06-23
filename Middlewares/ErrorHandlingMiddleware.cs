using OrdersApi.Exceptions;

namespace OrdersApi.Middlewares;

#region 🛡️ Middleware global de tratamento de exceções
/*
 Mudanças nesta fase (Fase 3):
 ──────────────────────────────
 Antes: capturava ArgumentException e KeyNotFoundException (tipos do .NET)
 Agora: captura ValidationException, NotFoundException e BusinessException
        — exceções do nosso próprio domínio.

 Por que isso é melhor?
 → As exceções de domínio carregam semântica clara.
 → O middleware não precisa "adivinhar" o que ArgumentException significa
   neste contexto — ValidationException já diz explicitamente.
 → Podemos adicionar novos tipos de exceção de domínio no futuro
   sem que o middleware precise conhecer cada caso específico:
   basta herdar de BusinessException e o catch genérico já cobre.

 Hierarquia tratada (mais específico → mais genérico):
   NotFoundException   → 404 Not Found
   ValidationException → 400 Bad Request
   BusinessException   → 400 Bad Request  (fallback para outros erros de domínio)
   Exception           → 500 Internal Server Error

 IMPORTANTE: a ordem dos catch importa.
 O C# avalia do primeiro ao último.
 Se BusinessException viesse antes de NotFoundException,
 ela seria capturada antes — pois NotFoundException herda de BusinessException.
 Sempre coloque os tipos mais específicos primeiro.
*/
#endregion
public class ErrorHandlingMiddleware
{
    #region 🔗 Referência ao próximo middleware da cadeia
    private readonly RequestDelegate _next;
    #endregion

    #region 🏗️ Construtor
    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    #endregion

    #region ⚙️ Método principal: processa cada requisição
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            #region ▶️ Passa para o próximo middleware / controller
            await _next(context);
            #endregion
        }

        #region 🔴 Recurso não encontrado → 404 Not Found
        /*
         Substituiu: catch (KeyNotFoundException ex)
         Motivo: NotFoundException é nossa exceção de domínio.
         Mais expressiva, mais testável, mais alinhada ao vocabulário da aplicação.
        */
        catch (NotFoundException ex)
        {
            await RespondWithErrorAsync(context, ex, StatusCodes.Status404NotFound);
        }
        #endregion

        #region 🔴 Violação de regra de negócio → 400 Bad Request
        /*
         Substituiu: catch (ArgumentException ex)
         Motivo: ValidationException comunica explicitamente que
         os dados fornecidos pelo cliente violaram uma regra de negócio.
        */
        catch (ValidationException ex)
        {
            await RespondWithErrorAsync(context, ex, StatusCodes.Status400BadRequest);
        }
        #endregion

        #region 🔴 Qualquer outro erro de domínio → 400 Bad Request
        /*
         Fallback para exceções que herdam de BusinessException
         mas não são NotFoundException nem ValidationException.

         Exemplo futuro: PaymentException, StockException, etc.
         Ao herdar de BusinessException, automaticamente caem aqui
         sem precisar alterar o middleware — isso é Open/Closed (OCP).
        */
        catch (BusinessException ex)
        {
            await RespondWithErrorAsync(context, ex, StatusCodes.Status400BadRequest);
        }
        #endregion

        #region 🔴 Erro inesperado → 500 Internal Server Error
        /*
         Última linha de defesa: captura qualquer exceção não prevista.
         Em produção, o ideal é logar ex aqui (ILogger) e
         retornar uma mensagem genérica ao cliente,
         sem expor detalhes internos da aplicação.
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
     Resposta padronizada para todos os erros:
     {
       "error": "Order 3fa85f64... not found",
       "type": "NotFoundException"
     }
    */
    private static async Task RespondWithErrorAsync(HttpContext context, Exception ex, int statusCode)
    {
        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = ex.Message,
            type  = ex.GetType().Name
        };

        await context.Response.WriteAsJsonAsync(errorResponse);
    }
    #endregion
}
