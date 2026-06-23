using OrdersApi.Models;

namespace OrdersApi.Services;

#region 📜 Interface do serviço de pedidos
/*
 O que é uma interface e por que usá-la aqui?
 ─────────────────────────────────────────────
 Uma interface define um CONTRATO: lista de métodos que qualquer
 implementação deve fornecer, sem ditar como eles funcionam.

 Benefícios:
   → Dependency Inversion Principle (DIP) do SOLID:
     Controllers dependem da abstração (IOrderService),
     não da implementação concreta (OrderService).

   → Testabilidade: em testes unitários, podemos criar um
     FakeOrderService que implementa IOrderService sem tocar
     no banco de dados ou na lógica real.

   → Substituibilidade: se amanhã trocarmos a implementação
     in-memory por uma que usa banco de dados, o controller
     não precisa mudar — ele só conhece a interface.

 Mudanças nesta fase (Fase 3):
 ──────────────────────────────
   → GetById: deixou de retornar Order? (nullable) e agora retorna Order.
     Motivo: o serviço agora lança NotFoundException em vez de retornar null.
     O controller não precisa mais verificar "se é null".

   → Delete: deixou de retornar bool e agora é void.
     Motivo: o serviço agora lança NotFoundException se o pedido não existir.
     O controller não precisa mais verificar o valor de retorno.
*/
#endregion
public interface IOrderService
{
    #region ➕ Criação de pedido
    /*
     Recebe dados básicos e retorna um pedido criado.
     Lança ValidationException se os dados forem inválidos.
    */
    Order Create(string customerName, decimal totalAmount);
    #endregion

    #region 🔍 Buscar pedido por ID
    /*
     Retorna o pedido encontrado.
     Lança NotFoundException se o pedido não existir.

     Por que não retornar null?
     → Null como "não encontrado" força o chamador a verificar sempre.
     → Exceção comunica explicitamente que algo errado aconteceu.
     → O controller fica mais limpo: apenas chama e usa o resultado.
    */
    Order GetById(Guid id);
    #endregion

    #region 📄 Listar todos os pedidos
    IEnumerable<Order> GetAll();
    #endregion

    #region 🗑️ Deletar pedido por ID
    /*
     Remove o pedido do armazenamento.
     Lança NotFoundException se o pedido não existir.

     Por que void em vez de bool?
     → bool obrigava o controller a verificar o retorno e decidir o 404.
     → Com exceção, o controller simplesmente chama Delete(id) e,
       se tudo correr bem, retorna 204. O middleware cuida do resto.
    */
    void Delete(Guid id);
    #endregion
}
