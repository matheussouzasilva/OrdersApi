using OrdersApi.Middlewares;
using OrdersApi.Services;

#region 🏗️ Criação do builder da aplicação
// WebApplication.CreateBuilder(args):
// - Inicializa a aplicação
// - Carrega configurações (appsettings.json, variáveis de ambiente, etc.)
// - Prepara o container de injeção de dependência (DI)
// - Configura logging e ambiente (Development, Production...)
var builder = WebApplication.CreateBuilder(args);
#endregion

#region 🔧 Registro de serviços (Dependency Injection)
// Aqui registramos tudo que a aplicação vai precisar durante a execução.
// O ASP.NET injeta automaticamente essas dependências onde forem solicitadas.

#region 🌐 Controllers (camada HTTP)
// Habilita suporte a controllers (API REST)
// Permite usar [ApiController], [HttpGet], [HttpPost], etc.
builder.Services.AddControllers();
#endregion

#region 🧠 Service de pedidos
// AddSingleton:
// - Uma única instância é criada para toda a aplicação
// - Todos os requests compartilham a mesma instância
// Aqui estamos dizendo:
// Sempre que alguém pedir IOrderService → entregue OrderService
builder.Services.AddSingleton<IOrderService, OrderService>();
#endregion

#endregion

#region 🏗️ Build da aplicação
// Constrói o objeto final da aplicação com base nas configurações acima
// A partir daqui não adicionamos mais serviços
var app = builder.Build();
#endregion

#region 🔁 Configuração do pipeline HTTP
// O pipeline define como cada requisição HTTP será processada

#region 🛡️ Middleware global de tratamento de exceções
/*
 IMPORTANTE: deve ser o PRIMEIRO middleware registrado.

 Por quê?
 → Os middlewares executam na ordem em que são registrados.
 → Se uma exceção ocorrer em qualquer middleware posterior
   (Authorization, Controllers...), ela precisa ser capturada
   por alguém que já esteja "acima" na cadeia.
 → Colocando ErrorHandlingMiddleware primeiro, garantimos que
   ele envolve toda a requisição com um try/catch global.

 Ordem de execução:
   Request → [ErrorHandling ✅] → [Authorization] → Controller
   Response ←        ←                  ←               ←
*/
app.UseMiddleware<ErrorHandlingMiddleware>();
#endregion

#region 🔐 Middleware de autorização
// Controla acesso a endpoints protegidos
// (Ainda não estamos usando autenticação, mas já está preparado)
app.UseAuthorization();
#endregion

#region 🗺️ Mapeamento de controllers
// Faz o "binding" das rotas HTTP com os controllers
// Exemplo:
// GET /api/orders → OrdersController.GetAll()
app.MapControllers();
#endregion

#endregion

#region ▶️ Inicialização da aplicação
// Inicia o servidor web (Kestrel)
// Começa a escutar requisições HTTP na porta configurada
// Exemplo:
// http://localhost:5069
app.Run();
#endregion