using OrdersApi.Exceptions;
using OrdersApi.Models;

namespace OrdersApi.Services;

#region 🧠 Implementação do serviço de pedidos
/*
 Responsabilidade desta classe:
 ───────────────────────────────
 Contém toda a lógica de negócio relacionada a pedidos.
 NÃO sabe nada sobre HTTP, status codes ou JSON.
 Apenas executa operações e lança exceções de domínio quando necessário.

 Separation of Concerns:
 → OrderService cuida de: regras de negócio, validações, persistência
 → Controllers cuidam de: receber requisição, chamar serviço, retornar resposta
 → Middleware cuida de: capturar exceções e traduzir para respostas HTTP

 Mudanças nesta fase (Fase 3):
 ──────────────────────────────
   → ArgumentException  substituído por ValidationException (domínio)
   → Retorno null       substituído por NotFoundException   (domínio)
   → Retorno bool       substituído por void + NotFoundException
*/
#endregion
public class OrderService : IOrderService
{
    #region 🗄️ Armazenamento em memória
    /*
     Simula um banco de dados durante o aprendizado.
     static garante que todos os requests compartilhem a mesma lista,
     já que o serviço foi registrado como Singleton no Program.cs.
    */
    private static readonly List<Order> _orders = new();
    #endregion

    #region ➕ Criação de pedido
    /*
     Fluxo:
       1. Valida os dados de entrada → lança ValidationException se inválido
       2. Cria a entidade Order com os dados fornecidos
       3. Persiste em memória
       4. Retorna a entidade criada ao controller

     Por que lançar ValidationException e não retornar null ou false?
     → A operação FALHOU por dados inválidos — isso é uma situação excepcional.
     → Exceções comunicam falha de forma clara e interrompem o fluxo imediatamente.
     → O middleware captura e converte para HTTP 400 sem poluir o controller.
    */
    public Order Create(string customerName, decimal totalAmount)
    {
        #region ✔️ Validação de regra de negócio
        /*
         Antes: throw new ArgumentException(...)
         Agora: throw new ValidationException(...)

         Por que mudar?
         → ArgumentException pertence ao .NET genérico.
         → ValidationException pertence ao nosso domínio.
         → O middleware pode classificar exatamente este tipo de erro
           e responder com semântica correta (400 Bad Request).
        */
        if (totalAmount <= 0)
            throw new ValidationException("TotalAmount must be greater than zero");

        if (string.IsNullOrWhiteSpace(customerName))
            throw new ValidationException("CustomerName is required");
        #endregion

        #region 🏗️ Criação da entidade Order
        var order = new Order
        {
            Id           = Guid.NewGuid(),
            CustomerName = customerName,
            TotalAmount  = totalAmount,
            Status       = "Pending"
        };
        #endregion

        #region 💾 Persistência em memória
        _orders.Add(order);
        #endregion

        return order;
    }
    #endregion

    #region 🔍 Buscar pedido por ID
    /*
     Antes: retornava Order? (null se não encontrado)
     Agora: retorna Order   (lança NotFoundException se não encontrado)

     Por que a mudança?
     → O controller deixa de ter responsabilidade de tratar "não encontrado".
     → O método comunica claramente: "ou retorno o pedido, ou algo errado ocorreu."
     → Menos código condicional no controller (sem if order == null).
    */
    public Order GetById(Guid id)
    {
        #region 🔍 Busca na lista
        var order = _orders.FirstOrDefault(o => o.Id == id);
        #endregion

        #region ⚠️ Pedido não encontrado → exceção de domínio
        /*
         Antes: return null
         Agora: throw new NotFoundException(...)

         A mensagem é descritiva e será enviada ao cliente pelo middleware.
         Formato da resposta:
         {
           "error": "Order 3fa85f64... not found",
           "type": "NotFoundException"
         }
        */
        if (order is null)
            throw new NotFoundException($"Order {id} not found");
        #endregion

        return order;
    }
    #endregion

    #region 📄 Listar todos os pedidos
    public IEnumerable<Order> GetAll()
    {
        return _orders;
    }
    #endregion

    #region 🗑️ Deletar pedido por ID
    /*
     Antes: retornava bool (true = deletado, false = não encontrado)
     Agora: retorna void  (lança NotFoundException se não encontrado)

     Por que a mudança?
     → bool forçava o controller a verificar o retorno e montar o 404 manualmente.
     → Com NotFoundException, o controller faz apenas: _service.Delete(id) + return NoContent().
     → O middleware intercepta a exceção e retorna 404 automaticamente.

     Isso é Single Responsibility (SRP): o controller não precisa
     conhecer o que significa "não encontrado" — apenas orquestra.
    */
    public void Delete(Guid id)
    {
        #region 🔍 Busca do pedido (reutiliza GetById que já lança exceção)
        /*
         Reutilizamos GetById para não duplicar a lógica de busca + não-encontrado.
         Se o pedido não existir, GetById já lança NotFoundException.
         Isso é o princípio DRY (Don't Repeat Yourself).
        */
        var order = GetById(id);
        #endregion

        #region 💾 Remoção da lista
        _orders.Remove(order);
        #endregion
    }
    #endregion
}
