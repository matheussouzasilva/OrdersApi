# OrdersApi

API REST educacional desenvolvida em **C# com ASP.NET Core** para gerenciamento de pedidos.

Este projeto foi construído com foco em **aprendizado de arquitetura de APIs**, aplicando boas práticas de forma gradual e explicada. Cada decisão técnica foi documentada para que o código seja um material de estudo — não apenas uma implementação funcional.

---

## Índice

- [Visão Geral](#visão-geral)
- [Tecnologias Utilizadas](#tecnologias-utilizadas)
- [Estrutura do Projeto](#estrutura-do-projeto)
- [Arquitetura](#arquitetura)
- [Fluxo de uma Requisição](#fluxo-de-uma-requisição)
- [Como Executar o Projeto](#como-executar-o-projeto)
- [Como Testar a API](#como-testar-a-api)
- [Tratamento de Erros](#tratamento-de-erros)
- [Conceitos Aplicados](#conceitos-aplicados)

---

## Visão Geral

A **OrdersApi** é uma API REST para criação e gerenciamento de pedidos. Ela permite:

- Criar um novo pedido informando o nome do cliente e o valor total
- Listar todos os pedidos cadastrados
- Buscar um pedido específico pelo seu ID
- Excluir um pedido

O propósito real do projeto **não é o domínio de pedidos em si**, mas sim servir como laboratório para aprender e praticar os fundamentos de uma API REST profissional: camadas bem definidas, separação de responsabilidades, tratamento de erros centralizado e exceções de domínio.

A API foi construída em **3 fases evolutivas**, cada uma adicionando uma camada de sofisticação arquitetural sobre a anterior. Essa progressão está documentada na pasta [`Documentation/Evolution/`](Documentation/Evolution/).

---

## Tecnologias Utilizadas

| Tecnologia | Versão | Por que foi escolhida |
|---|---|---|
| **C#** | 13 | Linguagem principal — fortemente tipada, expressiva e amplamente usada em APIs empresariais |
| **ASP.NET Core** | .NET 10 | Framework web da Microsoft — maduro, performático e com suporte nativo a DI, middlewares e roteamento REST |
| **Kestrel** | (embutido) | Servidor HTTP embutido no ASP.NET Core — sem necessidade de IIS para rodar localmente |

> **Nota:** O projeto usa armazenamento **em memória** (lista estática) como substituto simplificado de um banco de dados. O objetivo é focar na arquitetura da API sem a complexidade de um ORM ou banco de dados real neste momento.

---

## Estrutura do Projeto

```
OrdersApi/
│
├── Controllers/                  # Camada HTTP — recebe requisições e retorna respostas
│   └── OrdersController.cs
│
├── Services/                     # Camada de negócio — regras e operações da aplicação
│   ├── IOrderService.cs          # Contrato (interface) que o controller conhece
│   └── OrderService.cs           # Implementação concreta da lógica de negócio
│
├── Models/                       # Entidades de domínio — estrutura interna dos dados
│   └── Order.cs
│
├── DTOs/                         # Objetos de transferência — o que entra e sai da API
│   └── CreateOrderDto.cs         # Dados que o cliente envia para criar um pedido
│
├── Repositories/                 # (pasta criada, sem Repository Pattern implementado)
│   └── OrderResponseDto.cs       # Dados que a API retorna ao cliente
│
├── Exceptions/                   # Exceções customizadas de domínio
│   ├── BusinessException.cs      # Base de todas as exceções de domínio
│   ├── NotFoundException.cs      # Recurso não encontrado
│   └── ValidationException.cs   # Violação de regra de negócio
│
├── Middlewares/                  # Componentes do pipeline HTTP
│   └── ErrorHandlingMiddleware.cs # Tratamento centralizado de exceções
│
├── Properties/
│   └── launchSettings.json       # Configurações de porta e ambiente
│
├── appsettings.json              # Configurações gerais da aplicação
├── appsettings.Development.json  # Configurações específicas do ambiente de desenvolvimento
├── Program.cs                    # Ponto de entrada — configuração do pipeline e DI
└── OrdersApi.csproj              # Arquivo de projeto .NET
```

---

## Arquitetura

A aplicação segue uma arquitetura em camadas. Cada camada tem uma responsabilidade bem definida e **não deve invadir a responsabilidade da outra**.

```
┌─────────────────────────────────────────────────────┐
│                  Cliente (Postman, curl)             │
└───────────────────────┬─────────────────────────────┘
                        │ HTTP Request
                        ▼
┌─────────────────────────────────────────────────────┐
│              PIPELINE HTTP (Program.cs)             │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │         ErrorHandlingMiddleware             │   │
│  │   (captura exceções de qualquer camada)     │   │
│  └──────────────────────┬──────────────────────┘   │
│                         │                           │
│  ┌──────────────────────▼──────────────────────┐   │
│  │              Controllers/                   │   │
│  │   Recebe a requisição HTTP, chama o serviço │   │
│  │   e devolve a resposta com o status code    │   │
│  └──────────────────────┬──────────────────────┘   │
│                         │                           │
│  ┌──────────────────────▼──────────────────────┐   │
│  │               Services/                     │   │
│  │   Contém as regras de negócio               │   │
│  │   Valida dados, manipula entidades          │   │
│  │   Lança exceções de domínio quando necessário│  │
│  └──────────────────────┬──────────────────────┘   │
│                         │                           │
│  ┌──────────────────────▼──────────────────────┐   │
│  │            Armazenamento (memória)          │   │
│  │   Lista estática List<Order>                │   │
│  └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

### Controllers

**Responsabilidade:** Ser a porta de entrada das requisições HTTP.

O controller recebe a requisição, extrai os dados (body, parâmetros de rota), chama o serviço correspondente e retorna a resposta com o status HTTP correto.

O controller **não contém regras de negócio**. Ele apenas orquestra o fluxo.

```csharp
// Correto: controller delega ao serviço
var order = _service.Create(dto.CustomerName, dto.TotalAmount);
return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);

// Errado: controller com regra de negócio
if (dto.TotalAmount <= 0) return BadRequest(); // isso é responsabilidade do serviço
```

### Services

**Responsabilidade:** Conter toda a lógica de negócio da aplicação.

O serviço **não sabe** que existe HTTP, status codes ou JSON. Ele trabalha apenas com objetos de domínio e exceções de domínio.

```
IOrderService  ←  interface (contrato)
OrderService   ←  implementação concreta
```

O controller depende da **interface** (`IOrderService`), não da implementação. Isso permite trocar a implementação sem alterar o controller — princípio de Dependency Inversion (DIP).

### Models

**Responsabilidade:** Representar as entidades do domínio internamente na aplicação.

`Order` é a entidade central: contém `Id`, `CustomerName`, `TotalAmount`, `Status` e `CreatedAt`. Esta classe **não é exposta diretamente ao cliente** — para isso existem os DTOs.

### DTOs (Data Transfer Objects)

**Responsabilidade:** Controlar exatamente o que entra e o que sai da API.

| DTO | Direção | Uso |
|---|---|---|
| `CreateOrderDto` | Entrada (cliente → API) | Dados para criar um pedido |
| `OrderResponseDto` | Saída (API → cliente) | Dados retornados ao cliente |

**Por que não retornar a entidade diretamente?**
A entidade `Order` pode ter campos internos (auditoria, flags) que não devem ser expostos. O DTO permite controlar precisamente o contrato público da API.

### Middlewares

**Responsabilidade:** Processar todas as requisições e respostas no nível do pipeline HTTP, antes e depois dos controllers.

O `ErrorHandlingMiddleware` envolve toda a cadeia de execução com um `try/catch` global. Qualquer exceção não tratada em qualquer camada é capturada aqui, convertida em uma resposta JSON padronizada e enviada ao cliente.

### Exceptions

**Responsabilidade:** Representar situações de erro com semântica de domínio.

```
BusinessException        ← base de todos os erros de domínio
├── NotFoundException    ← recurso não encontrado  →  HTTP 404
└── ValidationException  ← dado inválido           →  HTTP 400
```

Usar exceções de domínio em vez de exceções genéricas do .NET torna o código mais expressivo e o middleware mais preciso no mapeamento para status HTTP.

---

## Fluxo de uma Requisição

Veja o caminho completo de uma requisição `POST /api/orders`:

```
1. Cliente envia:
   POST /api/orders
   { "customerName": "Ana", "totalAmount": 150.00 }

2. ASP.NET Core recebe a requisição no pipeline HTTP

3. ErrorHandlingMiddleware é ativado
   └── Envolve tudo com try/catch e aguarda o resultado

4. OrdersController.Create() é chamado
   └── Deserializa o JSON para CreateOrderDto
   └── Chama _service.Create("Ana", 150.00)

5. OrderService.Create() executa
   ├── Valida: totalAmount > 0 ✓
   ├── Valida: customerName não vazio ✓
   ├── Cria entidade Order com Guid.NewGuid()
   ├── Adiciona à lista _orders
   └── Retorna a entidade criada

6. OrdersController mapeia Order → OrderResponseDto
   └── Retorna HTTP 201 Created
       Location: /api/orders/{id}
       Body: { "id": "...", "customerName": "Ana", ... }

7. ErrorHandlingMiddleware deixa a resposta passar (sem exceção)

8. Cliente recebe a resposta
```

**Fluxo com erro** (ex: `totalAmount = -10`):

```
1. Cliente envia: { "customerName": "Ana", "totalAmount": -10 }

2–4. Mesmo fluxo acima

5. OrderService.Create() executa
   └── Valida: totalAmount > 0 ✗
   └── throw new ValidationException("TotalAmount must be greater than zero")

6. Exceção sobe pela cadeia de chamadas até o middleware

7. ErrorHandlingMiddleware captura ValidationException
   └── Mapeia para HTTP 400 Bad Request
   └── Responde: { "error": "TotalAmount must be greater than zero", "type": "ValidationException" }

8. Cliente recebe o erro padronizado
```

---

## Como Executar o Projeto

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download) instalado
- Terminal (PowerShell, bash, ou qualquer outro)

### Passos

```bash
# 1. Clone o repositório
git clone https://github.com/matheussouzasilva/OrdersApi.git
cd OrdersApi

# 2. Restaure as dependências
dotnet restore

# 3. Execute a aplicação
dotnet run
```

A API estará disponível em:
- HTTP:  `http://localhost:5069`
- HTTPS: `https://localhost:7062`

### Verificando se está rodando

```bash
curl http://localhost:5069/api/orders/teste
# Resposta esperada: {"message":"API online"}
```

---

## Como Testar a API

### Endpoints disponíveis

| Método | Rota | Descrição | Status de sucesso |
|---|---|---|---|
| `POST` | `/api/orders` | Cria um novo pedido | `201 Created` |
| `GET` | `/api/orders` | Lista todos os pedidos | `200 OK` |
| `GET` | `/api/orders/{id}` | Busca pedido por ID | `200 OK` |
| `DELETE` | `/api/orders/{id}` | Remove um pedido | `204 No Content` |

---

### Criar um pedido

```bash
curl -X POST http://localhost:5069/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Ana Silva", "totalAmount": 250.00}'
```

**Resposta (201 Created):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerName": "Ana Silva",
  "totalAmount": 250.00,
  "status": "Pending"
}
```

---

### Listar todos os pedidos

```bash
curl http://localhost:5069/api/orders
```

**Resposta (200 OK):**
```json
[
  {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "customerName": "Ana Silva",
    "totalAmount": 250.00,
    "status": "Pending"
  }
]
```

---

### Buscar pedido por ID

```bash
curl http://localhost:5069/api/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Resposta (200 OK):**
```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerName": "Ana Silva",
  "totalAmount": 250.00,
  "status": "Pending"
}
```

**Resposta se não encontrado (404 Not Found):**
```json
{
  "error": "Order 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found",
  "type": "NotFoundException"
}
```

---

### Deletar um pedido

```bash
curl -X DELETE http://localhost:5069/api/orders/3fa85f64-5717-4562-b3fc-2c963f66afa6
```

**Resposta de sucesso:** `204 No Content` (sem corpo)

**Resposta se não encontrado (404 Not Found):**
```json
{
  "error": "Order 3fa85f64-5717-4562-b3fc-2c963f66afa6 not found",
  "type": "NotFoundException"
}
```

---

### Testando erros de validação

```bash
curl -X POST http://localhost:5069/api/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName": "Ana", "totalAmount": -50}'
```

**Resposta (400 Bad Request):**
```json
{
  "error": "TotalAmount must be greater than zero",
  "type": "ValidationException"
}
```

---

## Tratamento de Erros

### Estratégia

Todos os erros são tratados de forma centralizada pelo `ErrorHandlingMiddleware`. Os controllers **não possuem try/catch** — qualquer exceção lançada em qualquer camada sobe automaticamente até o middleware.

### Mapeamento de exceções para status HTTP

| Exceção | Status HTTP | Quando ocorre |
|---|---|---|
| `ValidationException` | `400 Bad Request` | Dados de entrada inválidos (ex: valor negativo) |
| `NotFoundException` | `404 Not Found` | Recurso não existe (ex: ID inexistente) |
| `BusinessException` | `400 Bad Request` | Qualquer outra regra de negócio violada |
| `Exception` | `500 Internal Server Error` | Erro inesperado no servidor |

### Formato padrão de resposta de erro

Todos os erros retornam o mesmo formato JSON, independentemente do tipo:

```json
{
  "error": "Mensagem descritiva do problema",
  "type": "NomeDaExcecao"
}
```

### Hierarquia de exceções

```
Exception  (C# nativo)
└── BusinessException        ← base das exceções de domínio
    ├── NotFoundException    ← "este recurso não existe"
    └── ValidationException  ← "estes dados são inválidos"
```

A hierarquia permite que o middleware capture exceções de forma precisa (do mais específico ao mais genérico), e que novas exceções de domínio sejam adicionadas no futuro sem alterar o middleware — desde que herdem de `BusinessException`.

---

## Conceitos Aplicados

### REST (Representational State Transfer)

REST é um estilo arquitetural para APIs web. Os princípios aplicados neste projeto:

- **Recursos identificados por URLs:** `/api/orders`, `/api/orders/{id}`
- **Verbos HTTP semânticos:** `GET` para leitura, `POST` para criação, `DELETE` para remoção
- **Status codes corretos:** `201` ao criar, `204` ao deletar, `404` quando não encontrado, `400` para dados inválidos
- **Sem estado (stateless):** cada requisição carrega todas as informações necessárias

### Dependency Injection (DI)

Em vez de o controller instanciar o serviço diretamente (`new OrderService()`), o ASP.NET Core injeta a dependência automaticamente via construtor:

```csharp
public OrdersController(IOrderService service)
{
    _service = service; // ASP.NET injeta automaticamente
}
```

O vínculo entre interface e implementação é feito no `Program.cs`:
```csharp
builder.Services.AddSingleton<IOrderService, OrderService>();
```

Isso significa: "sempre que alguém pedir `IOrderService`, entregue uma instância de `OrderService`."

### Service Layer

A lógica de negócio vive exclusivamente na camada de serviço (`OrderService`). O controller não valida dados nem manipula entidades — ele apenas chama o serviço e retorna o resultado.

Isso facilita:
- **Testes:** o serviço pode ser testado isoladamente, sem HTTP
- **Manutenção:** a regra de negócio está em um único lugar
- **Reutilização:** múltiplos controllers poderiam usar o mesmo serviço

### Separation of Concerns (SoC)

Cada parte do código tem uma responsabilidade bem definida e não invade a dos outros:

| Camada | Responsabilidade |
|---|---|
| Controller | Lidar com HTTP (rotas, status codes, serialização) |
| Service | Executar regras de negócio |
| Model | Representar a entidade de domínio |
| DTO | Definir o contrato público da API |
| Middleware | Tratamento transversal (erros, logging, autenticação) |
| Exception | Comunicar falhas com semântica de domínio |

### SOLID

Os seguintes princípios do SOLID foram aplicados:

**S — Single Responsibility Principle**
Cada classe tem apenas uma razão para mudar. `OrderService` cuida de negócio. `ErrorHandlingMiddleware` cuida de erros. `OrdersController` cuida de HTTP.

**O — Open/Closed Principle**
O middleware está aberto para extensão (novas exceções de domínio são capturadas automaticamente via herança de `BusinessException`) mas fechado para modificação (não precisa ser alterado para cada nova exceção).

**D — Dependency Inversion Principle**
O controller depende da abstração `IOrderService`, não da implementação `OrderService`. Isso permite trocar a implementação (ex: para uma com banco de dados) sem alterar o controller.

### Middleware

Middleware é um componente que participa do pipeline de requisições HTTP. Cada requisição passa por todos os middlewares registrados, em ordem, antes de chegar ao controller — e a resposta percorre o mesmo caminho de volta.

```
Request → [ErrorHandling] → [Authorization] → [Controller] → Response
Response ←       ←                ←                ←
```

A posição no pipeline importa: o `ErrorHandlingMiddleware` é registrado primeiro para garantir que envolva toda a cadeia com seu `try/catch`.

### DTOs (Data Transfer Objects)

DTOs são objetos criados especificamente para transferir dados entre camadas — especialmente entre a API e o cliente. Eles desacoplam o contrato público da API da estrutura interna das entidades.

```
Cliente envia → CreateOrderDto → Serviço → Order (entidade) → OrderResponseDto → Cliente recebe
```

Se a entidade `Order` ganhar um campo interno (ex: `InternalNotes`), o cliente nunca verá esse campo, porque o DTO de resposta não o inclui.

### Status Codes HTTP

Os status codes comunicam o resultado de uma operação ao cliente:

| Código | Nome | Quando usar |
|---|---|---|
| `200 OK` | Sucesso com corpo | Listagem, busca por ID |
| `201 Created` | Recurso criado | POST bem-sucedido |
| `204 No Content` | Sucesso sem corpo | DELETE bem-sucedido |
| `400 Bad Request` | Dado inválido | Validação falhou |
| `404 Not Found` | Recurso inexistente | ID não encontrado |
| `500 Internal Server Error` | Erro no servidor | Exceção não prevista |

### Exceções de Domínio

Em vez de usar exceções genéricas do .NET (`ArgumentException`, `KeyNotFoundException`), criamos exceções próprias que carregam semântica de domínio:

```csharp
// Antes (genérico, sem contexto de domínio):
throw new ArgumentException("TotalAmount must be greater than zero");

// Depois (expressivo, pertence ao domínio da aplicação):
throw new ValidationException("TotalAmount must be greater than zero");
```

Isso torna o código mais legível e o tratamento de erros mais preciso.
