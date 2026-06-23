# Fase 01 — Endpoint DELETE

## O que foi implementado

Adição do endpoint `DELETE /api/orders/{id}` para remoção de pedidos, completando o conjunto básico de operações CRUD da API.

---

## Qual problema estava sendo resolvido?

A API já possuía:
- `POST /api/orders` — criar pedido
- `GET /api/orders` — listar pedidos
- `GET /api/orders/{id}` — buscar pedido por ID

Faltava a operação de remoção. Sem ela, pedidos criados não podiam ser excluídos — o que é uma operação fundamental em qualquer sistema de gerenciamento.

---

## Arquivos alterados

| Arquivo | Tipo de alteração | O que mudou |
|---|---|---|
| `Services/IOrderService.cs` | Alterado | Adicionado método `bool Delete(Guid id)` ao contrato |
| `Services/OrderService.cs` | Alterado | Implementado o método `Delete` |
| `Controllers/OrdersController.cs` | Alterado | Adicionado endpoint `DELETE /api/orders/{id}` |

---

## As alterações em detalhe

### Interface — `IOrderService.cs`

```csharp
// Adicionado ao contrato:
bool Delete(Guid id);
```

**Por que `bool` e não `void`?**

O controller precisa saber se o pedido existia para decidir o que responder:
- Se existia e foi deletado → retorna `204 No Content`
- Se não existia → retorna `404 Not Found`

Usar `bool` evita o uso de `try/catch` neste momento, já que ainda não temos um middleware de tratamento de exceções. Nas fases seguintes, esse `bool` será substituído por exceções de domínio.

---

### Serviço — `OrderService.cs`

```csharp
public bool Delete(Guid id)
{
    var order = _orders.FirstOrDefault(o => o.Id == id);

    if (order == null)
        return false; // sinaliza "não encontrado"

    _orders.Remove(order);
    return true; // sinaliza "deletado com sucesso"
}
```

**Separation of Concerns em ação:**
O método é responsável exclusivamente por:
1. Encontrar o pedido
2. Removê-lo
3. Sinalizar o resultado

Ele **não decide** o que fazer com o resultado — isso é responsabilidade do controller.

---

### Controller — `OrdersController.cs`

```csharp
[HttpDelete("{id:guid}")]
public IActionResult Delete(Guid id)
{
    var deleted = _service.Delete(id);

    if (!deleted)
        return NotFound(new { error = "Order not found" });

    return NoContent();
}
```

**Por que `DELETE` e não outro verbo?**

O HTTP define verbos semânticos para cada tipo de operação:
- `GET` → leitura (sem efeitos colaterais)
- `POST` → criação de recurso
- `PUT`/`PATCH` → atualização de recurso
- `DELETE` → remoção de recurso

Usar `DELETE` para remover é uma convenção REST que qualquer desenvolvedor reconhece imediatamente.

**Por que `204 No Content` e não `200 OK`?**

`200 OK` implica que há um corpo na resposta. Após deletar um pedido, não há nada a retornar — o recurso não existe mais. `204 No Content` comunica exatamente isso: "operação bem-sucedida, sem corpo de resposta".

**Por que `404 Not Found`?**

Se o cliente tenta deletar um pedido que não existe, o servidor não encontrou o recurso identificado pela URL. `404` é o status correto para essa situação.

---

## Conceitos aprendidos

### Verbos HTTP e semântica REST

REST define um conjunto de convenções para APIs web. Uma das mais importantes é o uso correto dos verbos HTTP:

| Verbo | Operação | Idempotente? |
|---|---|---|
| `GET` | Leitura | Sim |
| `POST` | Criação | Não |
| `PUT` | Substituição completa | Sim |
| `PATCH` | Atualização parcial | Sim |
| `DELETE` | Remoção | Sim |

**Idempotente** significa que chamar a operação múltiplas vezes produz o mesmo resultado. `DELETE /api/orders/123` chamado duas vezes: na primeira deleta, na segunda retorna 404 — o recurso deixou de existir da mesma forma.

### Status Codes de sucesso

| Código | Quando usar |
|---|---|
| `200 OK` | Operação bem-sucedida com corpo na resposta |
| `201 Created` | Recurso criado — geralmente acompanhado do header `Location` |
| `204 No Content` | Operação bem-sucedida sem corpo na resposta |

### `{id:guid}` — Restrição de rota

O `{id:guid}` na rota instrui o ASP.NET Core a aceitar apenas GUIDs válidos naquele segmento da URL. Requisições com IDs malformados são automaticamente rejeitadas com `400 Bad Request` antes de chegar ao controller.

```
/api/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6  ✓  aceito
/api/orders/abc123                                 ✗  rejeitado automaticamente
```

---

## Como a arquitetura evoluiu

Antes desta fase, a API tinha operações de criação e leitura. Com o DELETE, o conjunto básico de operações fica completo:

```
Antes:   POST, GET (lista), GET (por ID)
Depois:  POST, GET (lista), GET (por ID), DELETE
```

Do ponto de vista arquitetural, esta fase demonstrou como **estender a API** de forma limpa: um método na interface, uma implementação no serviço, um endpoint no controller — sem tocar em nada que já funcionava.

---

## Limitações desta fase

O tratamento de erros ainda estava distribuído: o controller `Create` tinha seu próprio `try/catch` para `ArgumentException`. O DELETE usava `bool` para sinalizar "não encontrado".

Isso funciona, mas **não escala bem**: se a API crescer com 20 endpoints, teríamos tratamento de erro espalhado por 20 lugares. A **Fase 2** resolve exatamente este problema.

[Próxima fase →](Fase02.md)
