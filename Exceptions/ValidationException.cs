namespace OrdersApi.Exceptions;

#region ✅ Exceção de validação de negócio: ValidationException
/*
 Quando usar esta exceção?
 ─────────────────────────
 Sempre que os dados fornecidos pelo cliente violarem uma regra
 de negócio — ou seja, quando a requisição é tecnicamente válida
 (JSON correto, tipos corretos), mas semanticamente inválida
 para as regras da aplicação.

 Exemplos:
   → TotalAmount <= 0     → throw new ValidationException(...)
   → CustomerName vazio   → throw new ValidationException(...)
   → Data no passado      → throw new ValidationException(...)

 Diferença entre ValidationException e NotFoundException:
 ──────────────────────────────────────────────────────────
   ValidationException → dados de entrada inválidos → 400 Bad Request
   NotFoundException   → recurso não existe         → 404 Not Found

 Por que não usar ArgumentException do C#?
 ──────────────────────────────────────────
 ArgumentException comunica que "um argumento está errado",
 mas não carrega semântica de domínio.

 ValidationException comunica claramente:
   "Esta requisição viola uma regra de negócio da aplicação."

 Aviso de nomenclatura:
 ──────────────────────
 O .NET possui System.ComponentModel.DataAnnotations.ValidationException.
 Nossa exceção está no namespace OrdersApi.Exceptions e é completamente
 independente — não há conflito enquanto não misturarmos os namespaces.
*/
#endregion
public class ValidationException : BusinessException
{
    #region 🏗️ Construtor
    /*
     Herda de BusinessException.
     O middleware mapeará ValidationException → HTTP 400 Bad Request.
    */
    public ValidationException(string message) : base(message)
    {
    }
    #endregion
}
