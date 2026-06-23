namespace OrdersApi.Models;

#region 📦 Entidade Order (Domínio principal do sistema)
// Representa um pedido dentro do sistema.
// Essa classe define os dados que compõem um pedido
// e será usada internamente na aplicação.
#endregion
public class Order
{
    #region 🆔 Identificador único do pedido
    // Guid garante unicidade global
    // Usado para identificar o pedido na API e no sistema
    public Guid Id { get; set; }
    #endregion

    #region 👤 Nome do cliente
    // Representa quem fez o pedido
    // Informação obrigatória para contexto de negócio
    public string CustomerName { get; set; } = string.Empty;
    #endregion

    #region 💰 Valor total do pedido
    // Define o valor financeiro do pedido
    // Regra de negócio: deve ser maior que zero
    public decimal TotalAmount { get; set; }
    #endregion

    #region 📦 Status do pedido
    // Controla o estado do pedido no fluxo de negócio
    // Exemplo: Pending, Paid, Shipped, Delivered
    public string Status { get; set; } = "Pending";
    #endregion

    #region 📅 Data de criação
    // Indica quando o pedido foi criado
    // Usado para auditoria e rastreabilidade
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    #endregion
}