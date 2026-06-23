# Fase 03 — Exceções Customizadas de Domínio

## O que foi implementado

Criação de exceções próprias da aplicação (`BusinessException`, `NotFoundException`, `ValidationException`) que substituem as exceções genéricas do .NET, tornando o código mais expressivo e o tratamento de erros mais preciso.

---

## Qual problema estava sendo resolvido?

No final da Fase 2, o serviço lançava exceções genéricas:

```csharp
// OrderService.cs — Fase 2
if (totalAmount <= 0)
    throw new ArgumentException("TotalAmount must be greater than zero");
```

E o middleware capturava pelo tipo genérico:

```csharp
// ErrorHandlingMiddleware.cs — Fase 2
catch (ArgumentException ex)
{
    await RespondWithErrorAsync(context, ex, 400);
}
catch (KeyNotFoundException ex)
{
    await RespondWithErrorAsync(context, ex, 404);
}
```

**Problemas:**

1. `ArgumentException` pertence ao .NET — pode ser lançada por qualquer código interno, não apenas pelo nosso domínio. Capturá-la como "erro de negócio" pode mascarar bugs reais.

2. O código não comunica **por que** a exceção foi lançada. `ArgumentException` diz "um argumento está errado" — mas errado por quê? Violou uma regra de negócio? Foi um dado inválido? É diferente de um recurso não encontrado?

3. Adicionar um novo tipo de erro (ex: `PaymentException`) exigiria alterar o middleware.

---

## Arquivos alterados

| Arquivo | Tipo | O que mudou |
|---|---|---|
| `Exceptions/BusinessException.cs` | Criado | Base de todas as exceções de domínio |
| `Exceptions/NotFoundException.cs` | Criado | Recurso não encontrado |
| `Exceptions/ValidationException.cs` | Criado | Violação de regra de negócio |
| `Services/IOrderService.cs` | Alterado | `GetById` → não-nullable; `Delete` → `void` |
| `Services/OrderService.cs` | Alterado | Substituídas exceções genéricas por exceções de domínio |
| `Controllers/OrdersController.cs` | Alterado | Removidas verificações de null e bool |
| `Middlewares/ErrorHandlingMiddleware.cs` | Alterado | Catches atualizados para exceções de domínio |

---

## As alterações em detalhe

### Hierarquia de exceções criada

```
Exception  (C# nativo)
└── BusinessException        ← base de todos os erros de domínio
    ├── NotFoundException    ← "este recurso não existe"
    └── ValidationException  ← "estes dados são inválidos"
```

**Por que criar uma hierarquia e não exceções independentes?**

A hierarquia permite que o middleware use múltiplos níveis de especificidade:

```csharp
catch (NotFoundException ex)   // captura só "não encontrado"
catch (ValidationException ex) // captura só "dado inválido"
catch (BusinessException ex)   // captura qualquer erro de domínio (fallback)
catch (Exception ex)           // captura qualquer coisa (último recurso)
```

Se amanhã criarmos `PaymentException extends BusinessException`, ela automaticamente cai no catch de `BusinessException` — sem precisar alterar o middleware. Isso é o **Open/Closed Principle (OCP)** do SOLID.

---

### `BusinessException.cs`

```csharp
public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
}
```

Classe base simples. Toda exceção de domínio herda dela. Sua existência permite ao middleware distinguir "erro de negócio" de "erro técnico inesperado".

---

### `NotFoundException.cs`

```csharp
public class NotFoundException : BusinessException
{
    public NotFoundException(string message) : base(message) { }
}
```

Usada quando buscamos um recurso pelo ID e ele não existe. Mapeia para `HTTP 404 Not Found`.

**Antes (Fase 2):** o serviço retornava `null` e o controller verificava `if (order == null) return NotFound()`

**Depois (Fase 3):** o serviço lança `NotFoundException` e o middleware retorna 404 automaticamente — o controller nem sabe que o recurso pode não existir.

---

### `ValidationException.cs`

```csharp
public class ValidationException : BusinessException
{
    public ValidationException(string message) : base(message) { }
}
```

Usada quando os dados fornecidos violam uma regra de negócio. Mapeia para `HTTP 400 Bad Request`.

**Nota sobre nomenclatura:** O .NET possui `System.ComponentModel.DataAnnotations.ValidationException`. A nossa está no namespace `OrdersApi.Exceptions` e são completamente independentes.

---

### Mudanças no serviço — `OrderService.cs`

**Validação no método `Create`:**

```csharp
// Antes:
throw new ArgumentException("TotalAmount must be greater than zero");

// Depois:
throw new ValidationException("TotalAmount must be greater than zero");
throw new ValidationException("CustomerName is required"); // nova validação
```

**Método `GetById` — de nullable para exceção:**

```csharp
// Antes: retornava null se não encontrado
public Order? GetById(Guid id)
{
    return _orders.FirstOrDefault(o => o.Id == id); // null se não existe
}

// Depois: lança exceção se não encontrado
public Order GetById(Guid id)
{
    var order = _orders.FirstOrDefault(o => o.Id == id);

    if (order is null)
        throw new NotFoundException($"Order {id} not found");

    return order;
}
```

**Método `Delete` — de bool para void:**

```csharp
// Antes: retornava bool
public bool Delete(Guid id)
{
    var order = _orders.FirstOrDefault(o => o.Id == id);
    if (order == null) return false;
    _orders.Remove(order);
    return true;
}

// Depois: void + reutiliza GetById (DRY)
public void Delete(Guid id)
{
    var order = GetById(id); // lança NotFoundException se não existir
    _orders.Remove(order);
}
```

O método `Delete` reutiliza `GetById` em vez de duplicar a lógica de busca — isso é o princípio **DRY** aplicado dentro do próprio serviço.

---

### Mudanças na interface — `IOrderService.cs`

```csharp
// Antes:
Order? GetById(Guid id); // nullable — pode retornar null
bool Delete(Guid id);    // retorna bool para indicar sucesso

// Depois:
Order GetById(Guid id);  // não-nullable — ou retorna, ou lança exceção
void Delete(Guid id);    // void — ou funciona, ou lança exceção
```

A interface agora documenta o comportamento pelo contrato de tipo: se `GetById` retorna `Order` (não `Order?`), o chamador sabe que nunca receberá null — receberá o pedido ou uma exceção.

---

### Mudanças no controller — `OrdersController.cs`

**Endpoint `GetById`:**

```csharp
// Antes: verificava null manualmente
public IActionResult GetById(Guid id)
{
    var order = _service.GetById(id);

    if (order == null)       // ← verificação manual
        return NotFound();

    // mapeia e retorna...
}

// Depois: sem verificação — serviço garante o retorno ou lança exceção
public IActionResult GetById(Guid id)
{
    var order = _service.GetById(id); // se não existir, NotFoundException é lançada
    // mapeia e retorna...
}
```

**Endpoint `Delete`:**

```csharp
// Antes: verificava bool de retorno
public IActionResult Delete(Guid id)
{
    var deleted = _service.Delete(id);

    if (!deleted)                            // ← verificação manual
        return NotFound(new { error = "..." });

    return NoContent();
}

// Depois: sem verificação
public IActionResult Delete(Guid id)
{
    _service.Delete(id); // se não existir, NotFoundException é lançada
    return NoContent();
}
```

O controller agora é completamente "otimista": chama o serviço e assume que vai funcionar. Se não funcionar, o middleware cuida.

---

### Mudanças no middleware — `ErrorHandlingMiddleware.cs`

```csharp
// Antes (exceções genéricas do .NET):
catch (ArgumentException ex)    { await RespondWithErrorAsync(context, ex, 400); }
catch (KeyNotFoundException ex) { await RespondWithErrorAsync(context, ex, 404); }
catch (Exception ex)            { await RespondWithErrorAsync(context, ex, 500); }

// Depois (exceções de domínio — mais específico primeiro):
catch (NotFoundException ex)    { await RespondWithErrorAsync(context, ex, 404); }
catch (ValidationException ex)  { await RespondWithErrorAsync(context, ex, 400); }
catch (BusinessException ex)    { await RespondWithErrorAsync(context, ex, 400); }
catch (Exception ex)            { await RespondWithErrorAsync(context, ex, 500); }
```

**A ordem dos catches é crítica.** O C# avalia de cima para baixo e executa o primeiro que corresponder ao tipo da exceção.

Se `BusinessException` viesse antes de `NotFoundException`, toda `NotFoundException` seria capturada como `BusinessException` — porque `NotFoundException` herda de `BusinessException`. O tipo mais específico sempre deve vir primeiro.

---

## Conceitos aprendidos

### Exceções como comunicação de domínio

Exceções não são apenas mecanismos de controle de fluxo — elas são documentação executável. Quando o código lança `NotFoundException`, ele está comunicando claramente:

> "Procurei este recurso e ele não existe. Este é um fato do domínio da aplicação."

`ArgumentException` comunica algo muito mais vago: "um argumento estava errado" — sem contexto de domínio.

### Null vs Exceção: quando usar cada um?

Uma discussão clássica em desenvolvimento de software:

**Retornar null:**
- Bom quando "não encontrado" é um resultado esperado e rotineiro
- Obriga o chamador a sempre verificar
- Pode causar `NullReferenceException` se o chamador esquecer de verificar

**Lançar exceção:**
- Bom quando "não encontrado" é uma situação excepcional que interrompe o fluxo
- O chamador não precisa verificar — ou recebe o resultado, ou a exceção é tratada
- Código fica mais limpo (sem `if (x == null)` em todo lugar)

Neste projeto, usamos exceções porque "buscar um pedido que não existe" é genuinamente uma situação excepcional no contexto de uma API.

### Open/Closed Principle (OCP)

O middleware está **aberto para extensão** (novas exceções de domínio são tratadas via herança de `BusinessException`) e **fechado para modificação** (não precisa ser alterado para cada nova exceção).

Se amanhã criarmos:

```csharp
public class PaymentException : BusinessException { ... }
```

O middleware já a tratará como `400 Bad Request` via o catch de `BusinessException` — sem nenhuma alteração.

### DRY dentro do próprio serviço

O método `Delete` reutiliza `GetById` internamente:

```csharp
public void Delete(Guid id)
{
    var order = GetById(id); // reutiliza a lógica de busca + exceção
    _orders.Remove(order);
}
```

Em vez de duplicar `_orders.FirstOrDefault(o => o.Id == id)` e `throw new NotFoundException(...)`, delegamos ao método que já faz isso. Se a lógica de "não encontrado" mudar (ex: mudar a mensagem), muda em um único lugar.

---

## Como a arquitetura evoluiu

```
FASE 1:
  Serviço: retorna null / bool
  Controller: verifica null/bool + monta erro manualmente
  Middleware: não existia

FASE 2:
  Serviço: lança ArgumentException / KeyNotFoundException
  Controller: sem try/catch (delegou para o middleware)
  Middleware: captura exceções genéricas do .NET

FASE 3:
  Serviço: lança NotFoundException / ValidationException (domínio)
  Controller: sem verificações — só chama e retorna
  Middleware: captura exceções de domínio com hierarquia
```

A cada fase, o controller ficou mais simples e focado. A cada fase, o serviço comunicou suas falhas de forma mais expressiva. A cada fase, o tratamento de erros ficou mais robusto e extensível.

---

## Estado final dos contratos

Após a Fase 3, os contratos da API são claros:

| Operação | Sucesso | Falha de validação | Não encontrado |
|---|---|---|---|
| `POST /api/orders` | `201` + pedido | `400` + ValidationException | — |
| `GET /api/orders` | `200` + lista | — | — |
| `GET /api/orders/{id}` | `200` + pedido | — | `404` + NotFoundException |
| `DELETE /api/orders/{id}` | `204` (sem corpo) | — | `404` + NotFoundException |

[← Fase anterior](Fase02.md)
