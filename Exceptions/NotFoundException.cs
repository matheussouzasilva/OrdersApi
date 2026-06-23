namespace OrdersApi.Exceptions;

#region 🔍 Exceção de recurso não encontrado: NotFoundException
/*
 Quando usar esta exceção?
 ─────────────────────────
 Sempre que uma operação buscar um recurso pelo identificador
 e ele não existir na base de dados (ou no armazenamento em memória).

 Exemplos:
   → GetById(id) → pedido não existe → throw new NotFoundException(...)
   → Delete(id)  → pedido não existe → throw new NotFoundException(...)

 Por que não usar KeyNotFoundException do C#?
 ────────────────────────────────────────────
 KeyNotFoundException é uma exceção genérica do .NET, sem semântica
 de domínio. Ao usar NotFoundException:
   → O código fica mais expressivo e legível
   → O middleware pode classificar exatamente este tipo de erro
   → Adicionamos contexto rico (qual recurso, qual ID) na mensagem
   → Qualquer camada que lançar NotFoundException está comunicando
     claramente que buscou algo e não encontrou

 Relação com HTTP:
 O middleware mapeará NotFoundException → HTTP 404 Not Found.
 Mas a exceção em si NÃO sabe disso — essa é responsabilidade
 exclusiva da camada HTTP (middleware).
*/
#endregion
public class NotFoundException : BusinessException
{
    #region 🏗️ Construtor
    /*
     Herda de BusinessException, que herda de Exception.
     Isso nos dá a cadeia: NotFoundException → BusinessException → Exception.

     O middleware pode capturar qualquer nível desta hierarquia:
       catch (NotFoundException ex)  → trata especificamente
       catch (BusinessException ex)  → trata qualquer erro de negócio
       catch (Exception ex)          → trata qualquer coisa
    */
    public NotFoundException(string message) : base(message)
    {
    }
    #endregion
}
