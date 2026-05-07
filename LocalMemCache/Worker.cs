using LocalCache.Domain.General;
using LocalMemCache.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;

namespace LocalMemCache;

public class CacheWorker : BackgroundService {
    private readonly ManageCache Engine; // Esta es tu instancia única
    private readonly Settings Cfg;
    private readonly ILogger<CacheWorker> Logger;

    public CacheWorker (ManageCache engine, Settings cfg, ILogger<CacheWorker> logger) {
        Engine = engine;
        Cfg = cfg;
        Logger = logger;
    }

    protected override async Task ExecuteAsync (CancellationToken stoppingToken) {
        Logger.LogInformation("Modo: " + (Environment.UserInteractive ? "Consola" : "Servicio"));

        // 1. Restauración inicial
        await Engine.RestoreData();

        // 2. Servidor TCP
        TcpListener listener = new(IPAddress.Loopback, Cfg.PORT);
        listener.Start();
        Logger.LogInformation($"Servidor listo en puerto {Cfg.PORT}");

        // 3. Timer para Snapshots (Cada 15 min)
        _ = Task.Run(async () => {
            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(15));
            while (await timer.WaitForNextTickAsync(stoppingToken)) {
                await Engine.CreateSnapshot();
                Logger.LogInformation("Snapshot automático completado.");
            }
        }, stoppingToken);

        // 4. Bucle de conexiones (Reemplaza tu while de Program.cs)
        try {
            while (!stoppingToken.IsCancellationRequested) {
                TcpClient client = await listener.AcceptTcpClientAsync(stoppingToken);

                // USAMOS TU LÓGICA DE REFERENCIA AQUÍ
                var engineRef = Engine;
                HandleConnection connHandler = new(Cfg, ref engineRef);

                _ = Task.Run(() => connHandler.HandleClientConnection(client), stoppingToken);
            }
        } finally {
            listener.Stop();
        }
    }
}