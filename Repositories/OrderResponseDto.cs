namespace OrdersApi.DTOs;

#region 📤 DTO de resposta
// Representa os dados que a API retorna para o cliente
// Evita expor diretamente a entidade de domínio
#endregion
public class OrderResponseDto
{
    #region 🆔 ID do pedido retornado ao cliente
    public Guid Id { get; set; }
    #endregion

    #region 👤 Nome do cliente
    public string CustomerName { get; set; } = string.Empty;
    #endregion

    #region 💰 Valor total do pedido
    public decimal TotalAmount { get; set; }
    #endregion

    #region 📦 Status atual do pedido
    public string Status { get; set; } = string.Empty;
    #endregion
}