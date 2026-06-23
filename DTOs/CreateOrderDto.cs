namespace OrdersApi.DTOs;

#region 📥 DTO de criação de pedido
// Representa os dados que o cliente envia para criar um pedido
// Serve como contrato de entrada da API
#endregion
public class CreateOrderDto
{
    #region 👤 Nome do cliente recebido na requisição
    // Vem do JSON enviado pelo cliente
    public string CustomerName { get; set; } = string.Empty;
    #endregion

    #region 💰 Valor total enviado pelo cliente
    // Será validado na camada de serviço
    public decimal TotalAmount { get; set; }
    #endregion
}