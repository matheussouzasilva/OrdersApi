using OrdersApi.Models;

namespace OrdersApi.Services;

#region 📜 Interface do serviço de pedidos
// Define o contrato que qualquer implementação deve seguir
// Permite desacoplamento e facilita testes
#endregion
public interface IOrderService
{
    #region ➕ Criação de pedido
    // Recebe dados básicos e retorna um pedido criado
    Order Create(string customerName, decimal totalAmount);
    #endregion

    #region 🔍 Buscar pedido por ID
    // Retorna null caso não exista
    Order? GetById(Guid id);
    #endregion

    #region 📄 Listar todos os pedidos
    IEnumerable<Order> GetAll();
    #endregion

    #region 🗑️ Deletar pedido por ID
    /*
     Retorna true se o pedido foi encontrado e removido.
     Retorna false se o pedido não existir.

     Por que bool e não void?
     → O controller precisa saber se o pedido existia para
       retornar 204 (deletado) ou 404 (não encontrado).
     → Usar bool evita try/catch por enquanto.
       Nas fases seguintes, substituiremos por exceções customizadas.
    */
    bool Delete(Guid id);
    #endregion
}