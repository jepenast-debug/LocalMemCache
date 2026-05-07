using LocalCache.Application;
using LocalCache.Domain.Entities;
using LocalCache.Domain.General;
using LocalCache.Infrastructure.Logging;
using LocalCache.Infrastructure.Persistence;
using LocalCache.Server.Protocol;
using System.Net.Sockets;
using System.Text;

namespace LocalMemCache.Core {
    public class HandleConnection {

        private readonly Settings Cfg;
        private readonly AuthService AuthService;
        private readonly DBRepository DBRepo;
        public readonly ManageCache Engine;
        private readonly FileLogger Logger;
        private readonly ManageApp AdmApp;

        public HandleConnection (Settings Config, ref ManageCache EngCache) {
            Cfg = Config;
            DBRepo = new(Cfg.DBPATCH, Cfg.PathLog);
            AuthService = new(DBRepo);
            Engine = EngCache;
            Logger = new(Cfg.PathLog);
            AdmApp = new(DBRepo, ref Engine);
        }
        public async Task HandleClientConnection (TcpClient Client) {
            using var Stream = Client.GetStream();
            using var Reader = new StreamReader(Stream, Encoding.UTF8);
            using var Writer = new StreamWriter(Stream, Encoding.UTF8) { AutoFlush = true };

            bool IsAuthenticated = !Cfg.AUTH; // Si AUTH es false, entra autenticado
            ClientInfo InfClient = new();

            try {
                while (Client.Connected) {
                    string? LineArgs = await Reader.ReadLineAsync();

                    if (LineArgs == null) break;

                    string[] Args = RespParser.ParseTextToArgs(LineArgs);
                    if (Args.Length == 0) continue;

                    string Cmd = Args[0].ToUpperInvariant();
                    string Response = "ERR unknown command";

                    // --- CAPA DE SEGURIDAD: AUTH ---
                    if (Cmd == "AUTH") {
                        if (Args.Length < 3)
                            Response = "ERR syntax: AUTH user password";
                        else {
                            // Args[1] es el usuario, Args[2] es TODO el resto (la clave)
                            if (await AuthService.Authenticate(Args[1], Args[2])) {
                                InfClient.ClientId = Args[1];
                                InfClient.IsAuthenticated = true;
                                Response = "OK";
                            } else Response = "ERR invalid user or password";
                        }
                    } else if (!InfClient.IsAuthenticated) {
                        Response = "NOAUTH Authentication required";
                    } else {
                        if (Cmd == "ADM" && InfClient.IsAuthenticated) {
                            Response = await AdmApp.ExecuteCommand(InfClient.ClientId, Args);
                        } else {
                            Response = await Engine.ExecuteCommand(InfClient.ClientId!, Cmd, Args);
                        }
                    }
                    await Writer.WriteLineAsync(Response);
                }
            } catch (Exception ex) {
                Logger.Error($"Client Error: {ex.Message}");
            }
        }
    }
}
