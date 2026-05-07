namespace LocalCache.Server.Protocol;

/// <summary>Convierte los argumentos enviados en una lista de argumentos para procesar</summary>
public static class RespParser {

    public static string[] ParseTextToArgs (string StrInput) {
        if (string.IsNullOrWhiteSpace(StrInput)) {
            return Array.Empty<string>();
        }
        string[] Parts = StrInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (Parts.Length < 3) {
            return Parts;
        }
        if (Parts[0].ToUpperInvariant() == "ADM") {
            //TODO:  Codigo para agregar comandos de administracion
        }
        //Validar ttl si llega
        int ttl = 0;
        int LastIndex = Parts.Length - 1;
        int ValEndIndex = LastIndex;
        // Intentamos ver si la última palabra es un número (TTL)
        if (Parts.Length > 3 && int.TryParse(Parts[LastIndex], out ttl)) {
            // Si es número, el valor termina una palabra antes
            ValEndIndex = LastIndex - 1;
        } else {
            ttl = 0; // No se envió TTL o no es número
        }

        if (Parts.Length >= 3) {
            string Action = Parts[0].ToUpperInvariant();
            string Key = Parts[1];
            string Value = string.Join(" ", Parts.Skip(2));
            if (ttl != 0) {
                Value = string.Join(" ", Parts.Skip(2).Take(ValEndIndex - 1));
            }
            Parts = [Action, Key, Value, ttl.ToString()];
        }
        if (Parts[0] == "AUTH") {
            Parts = [Parts[0], Parts[1], Parts[2]];
        }
        return Parts;
    }

    public static async Task<List<string[]>> ReadPipeline (StreamReader Reader) {
        var Cmds = new List<string[]>();

        // Mientras haya datos en el buffer del stream
        while (Reader.Peek() != -1) {
            string First = await Reader.ReadLineAsync() ?? string.Empty;
            if (!First.StartsWith("*")) break;

            int Count = int.Parse(First[1..]);
            string[] args = new string[Count];

            for (int i = 0; i < Count; i++) {
                await Reader.ReadLineAsync(); // Saltar $len
                args[i] = await Reader.ReadLineAsync() ?? string.Empty;
            }
            Cmds.Add(args);
        }
        return Cmds;
    }
}