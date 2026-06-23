using Microsoft.AspNetCore.Mvc;
using OrdersApi.DTOs;
using OrdersApi.Services;

namespace OrdersApi.Controllers;

#region 🌐 Controller de pedidos
// Responsável por lidar com requisições HTTP
// Atua como ponte entre cliente e regra de negócio
#endregion
[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    #region 🔌 Injeção de dependência do serviço
    private readonly IOrderService _service;

    public OrdersController(IOrderService service)
    {
        _service = service;
    }
    #endregion

[HttpGet("teste")]
public IActionResult Teste()
{
    return Ok(new { message = "API online" });
}

    #region ➕ Endpoint: Criar pedido (POST)
    /*
     Por que não tem mais try/catch aqui?
     ──────────────────────────────────────
     O ErrorHandlingMiddleware (Middlewares/ErrorHandlingMiddleware.cs)
     agora intercepta qualquer exceção lançada em qualquer camada.

     Benefícios:
     → Controller mais limpo e focado em sua responsabilidade:
       receber a requisição, chamar o serviço e retornar a resposta.
     → Tratamento de erros centralizado em um único lugar.
     → Não precisamos repetir try/catch em cada endpoint.

     Isso é o princípio DRY (Don't Repeat Yourself) em ação.
    */
    [HttpPost]
    public IActionResult Create(CreateOrderDto dto)
    {
        #region 🧠 Chamada da regra de negócio
        // Se TotalAmount <= 0, o serviço lança ArgumentException
        // O middleware captura e retorna 400 automaticamente
        var order = _service.Create(dto.CustomerName, dto.TotalAmount);
        #endregion

        #region 🔄 Mapeamento para DTO de resposta
        var response = new OrderResponseDto
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            TotalAmount = order.TotalAmount,
            Status = order.Status
        };
        #endregion

        #region 📤 Retorno HTTP 201 Created
        // 201 Created: recurso criado com sucesso
        // CreatedAtAction gera o header Location apontando para GET /api/orders/{id}
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
        #endregion
    }
    #endregion

    #region 🔍 Endpoint: Buscar por ID (GET)
    /*
     Antes desta fase: verificávamos "if (order == null) return NotFound()"
     Agora: o serviço lança NotFoundException se o pedido não existir,
     e o middleware converte para 404 automaticamente.

     O controller não precisa mais saber o que significa "não encontrado".
     Ele apenas chama o serviço e confia que exceções serão tratadas.
     Isso é Separation of Concerns funcionando em todas as camadas.
    */
    [HttpGet("{id:guid}")]
    public IActionResult GetById(Guid id)
    {
        #region 🧠 Chamada ao serviço (lança NotFoundException se não existir)
        var order = _service.GetById(id);
        #endregion

        #region 🔄 Mapeamento para DTO de resposta
        var response = new OrderResponseDto
        {
            Id           = order.Id,
            CustomerName = order.CustomerName,
            TotalAmount  = order.TotalAmount,
            Status       = order.Status
        };
        #endregion

        return Ok(response);
    }
    #endregion

    #region 📄 Endpoint: Listar pedidos (GET)
    [HttpGet]
    public IActionResult GetAll()
    {
        var orders = _service.GetAll();

        #region 🔄 Transformação para DTO
        var response = orders.Select(order => new OrderResponseDto
        {
            Id = order.Id,
            CustomerName = order.CustomerName,
            TotalAmount = order.TotalAmount,
            Status = order.Status
        });
        #endregion

        return Ok(response);
    }
    #endregion

    #region 🗑️ Endpoint: Deletar pedido (DELETE)
    /*
     Por que DELETE e não POST para "cancelar"?
     → REST define DELETE como o verbo semântico correto
       para remoção de recursos.
     → A URL identifica o recurso: DELETE /api/orders/{id}
       significa "remova este pedido específico".

     Por que 204 No Content e não 200 OK?
     → 204 significa "operação bem-sucedida, sem corpo na resposta".
     → Após uma exclusão, não há nada a retornar ao cliente.
       É a resposta REST semanticamente correta.

     Por que 404 Not Found?
     → O recurso identificado pelo ID não existe no servidor.
     → Informar o cliente que o recurso não foi localizado
       faz parte do contrato RESTful da API.
    */
    [HttpDelete("{id:guid}")]
    public IActionResult Delete(Guid id)
    {
        #region 🧠 Chamada da camada de serviço
        /*
         Delete agora é void: não retorna bool.
         Se o pedido não existir, o serviço lança NotFoundException.
         O middleware captura e retorna 404 automaticamente.

         O controller não precisa mais de if/else para tratar "não encontrado".
         Isso é Separation of Concerns: cada camada tem uma responsabilidade clara.
        */
        _service.Delete(id);
        #endregion

        #region ✅ Pedido deletado com sucesso → 204
        // Se chegou aqui, nenhuma exceção foi lançada → deleção bem-sucedida
        // NoContent() = HTTP 204 No Content: sucesso sem corpo de resposta
        return NoContent();
        #endregion
    }
    #endregion
}