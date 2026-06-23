using OrdersApi.Models;

namespace OrdersApi.Services;

#region 🧠 Implementação do serviço de pedidos
// Contém as regras de negócio da aplicação
// NÃO deve lidar com HTTP ou detalhes de transporte
#endregion
public class OrderService : IOrderService
{
    #region 🗄️ Armazenamento em memória
    // Simula um banco de dados
    // Persiste os pedidos durante a execução da aplicação
    private static readonly List<Order> _orders = new();
    #endregion

    #region ➕ Método de criação de pedido
    public Order Create(string customerName, decimal totalAmount)
    {
        #region ✔️ Validação de regra de negócio
        // Garante que o valor do pedido seja válido
        if (totalAmount <= 0)
            throw new ArgumentException("TotalAmount must be greater than zero");
        #endregion

        #region 🏗️ Criação da entidade Order
        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerName = customerName,
            TotalAmount = totalAmount,
            Status = "Pending"
        };
        #endregion

        #region 💾 Persistência em memória
        _orders.Add(order);
        #endregion

        return order;
    }
    #endregion

    #region 🔍 Buscar pedido por ID
    public Order? GetById(Guid id)
    {
        // Procura o pedido na lista
        return _orders.FirstOrDefault(o => o.Id == id);
    }
    #endregion

    #region 📄 Listar todos os pedidos
    public IEnumerable<Order> GetAll()
    {
        // Retorna todos os pedidos armazenados
        return _orders;
    }
    #endregion

    #region 🗑️ Deletar pedido por ID
    /*
     Separation of Concerns em ação:
     Este método é responsável apenas por:
       1. Encontrar o pedido
       2. Removê-lo da lista
       3. Sinalizar o resultado ao chamador (bool)

     Ele NÃO decide o que fazer com o resultado —
     essa responsabilidade é do controller (camada HTTP).
    */
    public bool Delete(Guid id)
    {
        #region 🔍 Busca do pedido
        // Tenta localizar o pedido na lista pelo ID
        var order = _orders.FirstOrDefault(o => o.Id == id);
        #endregion

        #region ⚠️ Pedido não encontrado
        // Se não existir, sinaliza false ao controller
        // Quem decide o que fazer com isso é a camada acima
        if (order == null)
            return false;
        #endregion

        #region 💾 Remoção da lista
        // Remove o pedido do armazenamento em memória
        _orders.Remove(order);
        #endregion

        return true;
    }
    #endregion
}