using LocalCache.Domain.General;
using LocalMemCache.Configuration;
using LocalMemCache.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// 1. Configurar Logs para evitar el error de ILogger
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// 2. Cargar Configuración
Settings cfg = new ConfigReader().LoadSettings();
builder.Services.AddSingleton(cfg);

// 3. Registrar ManageCache como Singleton (Equivalente a tu 'ref')
// Esto garantiza que todas las clases usen la MISMA instancia de memoria.
builder.Services.AddSingleton<ManageCache>();

// 4. Registrar el Worker que maneja el ciclo de vida
builder.Services.AddHostedService<LocalMemCache.CacheWorker>();

// 5. Soporte para Servicio de Windows
builder.Services.AddWindowsService(options => {
    options.ServiceName = "LocalMemCacheService";
});

var host = builder.Build();
await host.RunAsync();


//using LocalMemCache.Core;
//using LocalMemCache.Configuration;
//using LocalCache.Domain.General;
//using System.Net;
//using System.Net.Sockets;

//// 1. Cargar Configuración
//Settings cfg = new ConfigReader().LoadSettings();

//// 2. Inicializar el Orquestador (ManageCache)
//ManageCache Engine = new(cfg);
//HandleConnection TCPConn = new (cfg, ref Engine);


//// 3. Recuperación de Desastre (AOF / Snapshot)
//await Engine.RestoreData();

//// 4. Iniciar Servidor
//TcpListener listener = new(IPAddress.Loopback, cfg.PORT);
//listener.Start();
//Console.WriteLine($"Cache server running on port {cfg.PORT}...");

//while (true) {
//    TcpClient Client = await listener.AcceptTcpClientAsync();
//    // Ejecución asíncrona por cada cliente
//    _ = Task.Run(() => TCPConn.HandleClientConnection(Client));
//}