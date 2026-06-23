# Visão Geral do Projeto

## O que é a OrdersApi?

A OrdersApi é uma API REST para gerenciamento de pedidos construída com ASP.NET Core. Ela permite criar, listar, buscar e remover pedidos via requisições HTTP.

O projeto foi criado com propósito educacional: cada decisão de arquitetura foi tomada intencionalmente para demonstrar boas práticas de desenvolvimento de APIs, com comentários explicativos em todo o código.

---

## O problema que ela resolve

Do ponto de vista de negócio, a API resolve o gerenciamento básico de pedidos: registrar quem fez um pedido, qual o valor e qual o status atual.

Do ponto de vista educacional, ela resolve uma pergunta mais importante:

> "Como estruturar uma API REST de forma organizada, escalável e fácil de manter?"

A resposta é demonstrada através de camadas bem definidas, separação de responsabilidades e evolução progressiva da arquitetura.

---

## Por onde começar?

Se você chegou a este projeto sem contexto, siga esta ordem de leitura:

1. **Este documento** — entenda o propósito geral
2. **[Fase 01](Evolution/Fase01.md)** — estrutura inicial e endpoint DELETE
3. **[Fase 02](Evolution/Fase02.md)** — middleware de tratamento de erros
4. **[Fase 03](Evolution/Fase03.md)** — exceções de domínio
5. **[README principal](../README.md)** — referência completa da API

---

## Visão arquitetural

```
┌────────────────────────────────────────────────────────────────┐
│                         CLIENTE                                │
│              (Postman, curl, frontend, outro serviço)          │
└───────────────────────────┬────────────────────────────────────┘
                            │  HTTP (JSON)
                            ▼
┌────────────────────────────────────────────────────────────────┐
│                    ASP.NET CORE PIPELINE                       │
│                                                                │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  ErrorHandlingMiddleware                                 │  │
│  │  → captura qualquer exceção do pipeline                  │  │
│  │  → converte para JSON padronizado + status HTTP          │  │
│  └─────────────────────────┬────────────────────────────────┘  │
│                            │                                   │
│  ┌─────────────────────────▼────────────────────────────────┐  │
│  │  OrdersController                                        │  │
│  │  → lida com rotas HTTP (GET, POST, DELETE)               │  │
│  │  → converte DTOs ↔ entidades                             │  │
│  │  → retorna status codes corretos                         │  │
│  └─────────────────────────┬────────────────────────────────┘  │
│                            │                                   │
│  ┌─────────────────────────▼────────────────────────────────┐  │
│  │  IOrderService / OrderService                            │  │
│  │  → regras de negócio                                     │  │
│  │  → validações                                            │  │
│  │  → lança exceções de domínio quando necessário           │  │
│  └─────────────────────────┬────────────────────────────────┘  │
│                            │                                   │
│  ┌─────────────────────────▼────────────────────────────────┐  │
│  │  List<Order> (in-memory)                                 │  │
│  │  → simula um banco de dados                              │  │
│  └──────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────────┘
```

---

## Decisões técnicas principais

### Por que armazenamento em memória?

O foco do projeto é arquitetura de API, não persistência de dados. Uma lista estática em memória elimina a complexidade de configurar um banco de dados e permite focar inteiramente nas camadas HTTP, de negócio e de erros.

Em um projeto real, `OrderService` seria substituído por uma implementação que usa Entity Framework Core — sem alterar o controller, porque ele depende da interface `IOrderService`.

### Por que Singleton e não Scoped?

O serviço foi registrado como `Singleton` porque a lista de pedidos vive na instância do serviço. Se fosse `Scoped`, uma nova instância seria criada a cada requisição e os dados seriam perdidos.

```csharp
// Program.cs
builder.Services.AddSingleton<IOrderService, OrderService>();
```

Em uma aplicação com banco de dados, o serviço seria `Scoped` — a instância viveria apenas durante uma requisição.

### Por que interface + implementação separadas?

O controller depende de `IOrderService`, não de `OrderService`. Isso significa:

- Em produção: o container de DI injeta `OrderService`
- Em testes: podemos injetar um `FakeOrderService` sem banco de dados
- No futuro: podemos criar `DatabaseOrderService` sem tocar no controller

Isso é o princípio de Dependency Inversion (DIP) do SOLID.

---

## Evolução do projeto

O projeto cresceu em 3 fases, cada uma resolvendo um problema arquitetural específico:

| Fase | Problema | Solução |
|---|---|---|
| Fase 1 | Faltava o verbo DELETE | Endpoint `DELETE /api/orders/{id}` com `bool` de retorno no serviço |
| Fase 2 | try/catch repetido em cada controller | `ErrorHandlingMiddleware` centralizando o tratamento de erros |
| Fase 3 | Exceções genéricas sem semântica de domínio | `BusinessException`, `NotFoundException`, `ValidationException` |

Cada fase está documentada individualmente em [`Documentation/Evolution/`](Evolution/).
