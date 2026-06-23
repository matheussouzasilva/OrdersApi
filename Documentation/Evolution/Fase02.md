# Fase 02 — Middleware Global de Tratamento de Exceções

## O que foi implementado

Criação de um middleware centralizado (`ErrorHandlingMiddleware`) que captura todas as exceções lançadas em qualquer camada da aplicação, convertendo-as em respostas HTTP padronizadas em JSON.

---

## Qual problema estava sendo resolvido?

No final da Fase 1, o tratamento de erros estava assim:

```csharp
// OrdersController.cs — endpoint Create
[HttpPost]
public IActionResult Create(CreateOrderDto dto)
{
    try
    {
        var order = _service.Create(dto.CustomerName, dto.TotalAmount);
        // ...
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

// Endpoint DELETE — tratava erro de forma diferente, via bool
if (!deleted)
    return NotFound(new { error = "Order not found" });
```

**Problemas concretos:**
1. Cada endpoint tinha sua própria estratégia de tratamento de erros
2. O formato da resposta de erro era inconsistente: `{ message }` em um endpoint, `{ error }` em outro
3. Se adicionarmos 10 novos endpoints, precisamos repetir o `try/catch` em todos
4. Se quisermos mudar o formato do erro (adicionar um campo `timestamp`, por exemplo), precisamos alterar todos os endpoints

Isso viola o princípio **DRY (Don't Repeat Yourself)**: a mesma lógica de tratamento de erro estava se repetindo, ou precisaria se repetir conforme a API crescesse.

---

## Arquivos alterados

| Arquivo | Tipo | O que mudou |
|---|---|---|
| `Middlewares/ErrorHandlingMiddleware.cs` | Criado | Middleware que intercepta exceções globalmente |
| `Controllers/OrdersController.cs` | Alterado | `try/catch` removido do endpoint `Create` |
| `Program.cs` | Alterado | Middleware registrado no pipeline HTTP |

---

## As alterações em detalhe

### O que é um Middleware?

Middleware é um componente que fica no meio do pipeline HTTP. Cada requisição passa por uma cadeia de middlewares antes de chegar ao controller, e a resposta percorre o mesmo caminho de volta.

```
Requisição → [Middleware A] → [Middleware B] → [Controller]
Resposta   ←       ←                ←               ←
```

O ASP.NET Core já usa middlewares internamente para autenticação, compressão, roteamento, etc. Nós criamos o nosso para tratamento de erros.

---

### Middleware — `ErrorHandlingMiddleware.cs`

```csharp
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next; // referência ao próximo middleware da cadeia
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context); // executa o restante do pipeline
        }
        catch (ArgumentException ex)
        {
            await RespondWithErrorAsync(context, ex, 400);
        }
        catch (KeyNotFoundException ex)
        {
            await RespondWithErrorAsync(context, ex, 404);
        }
        catch (Exception ex)
        {
            await RespondWithErrorAsync(context, ex, 500);
        }
    }

    private static async Task RespondWithErrorAsync(HttpContext context, Exception ex, int statusCode)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        await context.Response.WriteAsJsonAsync(new
        {
            error = ex.Message,
            type  = ex.GetType().Name
        });
    }
}
```

**Como funciona:**

1. `_next` é uma referência ao próximo passo do pipeline (outro middleware ou o controller)
2. `await _next(context)` executa todo o restante da requisição
3. Se qualquer parte do pipeline lançar uma exceção, ela "sobe" até este `try/catch`
4. O middleware classifica a exceção e retorna a resposta adequada

**Por que `RequestDelegate` é injetado no construtor?**

O ASP.NET Core injeta automaticamente o `RequestDelegate` ao registrar o middleware. Isso é Dependency Injection acontecendo no nível do middleware — o mesmo princípio que usamos com `IOrderService` no controller.

---

### Pipeline — `Program.cs`

```csharp
// PRIMEIRO: middleware de erros (envolve todo o pipeline)
app.UseMiddleware<ErrorHandlingMiddleware>();

// DEPOIS: os demais middlewares e controllers
app.UseAuthorization();
app.MapControllers();
```

**Por que o ErrorHandlingMiddleware deve ser o primeiro?**

A ordem de registro define a ordem de execução. Se registrarmos o middleware de erros depois de outros middlewares, exceções lançadas pelos middlewares anteriores não serão capturadas.

Visualizando:

```
CORRETO:
Request → [ErrorHandling] → [Authorization] → [Controller]
              ↑ captura qualquer exceção que venha de baixo

ERRADO:
Request → [Authorization] → [ErrorHandling] → [Controller]
              ↑ uma exceção aqui escapa do ErrorHandling
```

---

### Controller — `OrdersController.cs`

O `try/catch` foi completamente removido do endpoint `Create`:

```csharp
// Antes (com try/catch):
[HttpPost]
public IActionResult Create(CreateOrderDto dto)
{
    try
    {
        var order = _service.Create(dto.CustomerName, dto.TotalAmount);
        // ...
        return CreatedAtAction(...);
    }
    catch (ArgumentException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}

// Depois (sem try/catch):
[HttpPost]
public IActionResult Create(CreateOrderDto dto)
{
    var order = _service.Create(dto.CustomerName, dto.TotalAmount);
    // ...
    return CreatedAtAction(...);
}
```

O controller ficou mais limpo e focado em sua responsabilidade principal: orquestrar a requisição.

---

## Conceitos aprendidos

### Pipeline HTTP e ordem dos middlewares

O pipeline do ASP.NET Core é uma cadeia de responsabilidades. Cada middleware decide se passa a requisição adiante (`await _next(context)`) ou interrompe o fluxo (retornando uma resposta diretamente).

O `ErrorHandlingMiddleware` sempre passa a requisição adiante — mas fica "em volta" da chamada para capturar exceções:

```csharp
try
{
    await _next(context); // tudo acontece aqui dentro
}
catch (...)
{
    // só chega aqui se algo lançar uma exceção
}
```

### DRY (Don't Repeat Yourself)

O princípio DRY diz que cada pedaço de conhecimento deve ter uma única representação no sistema. O tratamento de erros é um conhecimento que antes estava espalhado em vários controllers. Agora existe em um único lugar.

Se precisarmos mudar o formato da resposta de erro, alteramos apenas o `ErrorHandlingMiddleware`.

### Single Responsibility Principle (SRP)

Após remover o `try/catch` do controller, cada camada ficou com uma responsabilidade única:

- **Controller:** recebe requisição, chama serviço, retorna resposta
- **Service:** executa regras de negócio
- **Middleware:** trata erros de toda a aplicação

Nenhuma dessas camadas invade o território da outra.

### Formato padronizado de resposta de erro

Antes desta fase, os erros podiam vir em formatos diferentes:
```json
{ "message": "TotalAmount must be greater than zero" }
{ "error": "Order not found" }
```

Depois, todos os erros seguem o mesmo contrato:
```json
{
  "error": "mensagem do erro",
  "type": "TipoDeExcecao"
}
```

Isso é importante porque clientes que consomem a API (frontends, outros serviços) podem confiar em um formato único para tratar erros.

---

## Limitações desta fase

O middleware ainda captura `ArgumentException` e `KeyNotFoundException` — exceções genéricas do .NET que não carregam semântica de domínio.

`ArgumentException` pode ser lançada por inúmeras causas no .NET, não apenas por violação de regra de negócio. Se uma biblioteca interna lançar `ArgumentException` por um motivo técnico, o middleware incorretamente a trataria como erro de negócio (`400 Bad Request`) em vez de erro interno (`500`).

A **Fase 3** resolve isso criando exceções específicas do domínio da aplicação.

[← Fase anterior](Fase01.md) | [Próxima fase →](Fase03.md)
