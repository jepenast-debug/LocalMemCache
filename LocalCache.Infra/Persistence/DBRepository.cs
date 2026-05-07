using LocalCache.Domain.Entities;
using LocalCache.Domain.Interfaces;
using LocalCache.Infrastructure.Logging;
using LocalCache.Infrastructure.Metrics;
using LocalCache.Infrastructure.Resilience;
using Microsoft.Data.Sqlite;

namespace LocalCache.Infrastructure.Persistence;

public class DBRepository : IDBRepository {
    private readonly CircuitBreaker CB = new();
    private readonly MetricsService Metrics = new();
    private readonly SqliteConnection Conn;
    private readonly CmdRepository Querys = new();
    private readonly FileLogger Logging;


    public DBRepository (string DBPath, string LogPath) {
        Logging = new(LogPath);
        Conn = new SqliteConnection($"Data Source={DBPath}");
        using var _ = Initialize();
    }
    ~DBRepository () {
        using var _ = CloseConn();
    }

    private bool IsConnect () {
        if (Conn.State == System.Data.ConnectionState.Open) {
            return true;
        } else {
            return false;
        }
    }

    private async Task OpenConn () {
        try {
            if (!IsConnect()) {
                await Conn.OpenAsync();
            }
        } catch (Exception Ex) {
            Logging.Error(Ex.Message);
        }
    }

    private async Task CloseConn () {
        if (IsConnect()) {
            await Conn.CloseAsync();
        }
    }

    private void CatchData (string Msg) {
        CB.RegisterFailure();
        Logging.Error(Msg);
        Metrics.LogError();
    }

    private async Task<bool> Initialize () {
        bool Result = false;
        if (CB.IsOpen())
            Result = false;
        try {
            await OpenConn();
            await RetryPolicy.ExecuteAsync(async () => {
                if (IsConnect()) {
                    using SqliteCommand Cmd = Conn.CreateCommand();
                    Cmd.CommandText = Querys.CreateTable("Users");
                    Cmd.ExecuteNonQuery();
                    Cmd.CommandText = Querys.CreateTable("Metrics");
                    Cmd.ExecuteNonQuery();
                    Cmd.CommandText = Querys.CreateTable("UserLog");
                    Cmd.ExecuteNonQuery();
                }
            });
            Result = true;
        } catch (Exception Ex) {
            CatchData(Ex.Message);
            Result = false;
        } finally {
            await CloseConn();
        }
        return Result;
    }

    public async Task<ClientUser?> GetById (string Id) {
        try {
            SqliteParameter[] Params = [
                new SqliteParameter("$Id", Id)
            ];
            List<Dictionary<string, object>> Result = await ExecuteReader(Querys.GetUser(), Params);
            if (Result.Count > 0) {
                var Row = Result[0];
                return new ClientUser {
                    // Accedemos por el nombre de la columna en la DB
                    Id = Row["Id"].ToString()!,
                    PwdHash = Row["PwdHash"].ToString()!,
                    IsActive = Convert.ToInt32(Row["IsActive"]) == 1,
                    Profile = Convert.ToString(Row["Profile"])!
                };
            }
            return null;
        } catch (Exception Ex) {
            CatchData(Ex.Message);
            return null;
        }
    }

    public async Task AddUser (ClientUser User) {
        try {
            SqliteParameter[] Params = [
                new SqliteParameter("$id", User.Id),
                new SqliteParameter("$pass", User.PwdHash),
            ];
            await ExecuteCommand(Querys.GetAddUser(), Params);
        } catch (Exception Ex) {
            CatchData(Ex.Message);
        }
    }

    private async Task<bool> ExecuteCommand (string Query, SqliteParameter[] Params) {
        bool Result = false;
        try {
            await OpenConn();
            if (IsConnect()) {
                using SqliteCommand Cmd = Conn.CreateCommand();
                Cmd.CommandText = Query;
                Cmd.Parameters.AddRange(Params);
                await Cmd.ExecuteNonQueryAsync();
                Result = true;
            }
        } catch (Exception Ex) {
            CatchData(Ex.Message);
        } finally {
            await CloseConn();
        }
        return Result;
    }

    private async Task<List<Dictionary<string, object>>> ExecuteReader (string Query, SqliteParameter[] Params) {
        List<Dictionary<string, object>> DataR = [];
        try {
            await OpenConn();
            if (IsConnect()) {
                using SqliteCommand Cmd = Conn.CreateCommand();
                Cmd.CommandText = Query;
                Cmd.Parameters.AddRange(Params);
                using SqliteDataReader Reader = await Cmd.ExecuteReaderAsync();
                while (await Reader.ReadAsync()) {
                    Dictionary<string, object> Row = [];
                    for (int i = 0; i < Reader.FieldCount; i++) {
                        Row[Reader.GetName(i)] = Reader.GetValue(i);
                    }
                    DataR.Add(Row);
                }
            }
        } catch (Exception Ex) {
            CatchData(Ex.Message);
        } finally {
            await CloseConn();
        }
        return DataR;
    }

    public async Task<bool> UpdatePwd (ClientUser User) {
        try {
            SqliteParameter[] Params = [
                new SqliteParameter("$id", User.Id),
                new SqliteParameter("$pass", User.PwdHash),
            ];
            return await ExecuteCommand(Querys.GetChagePwd(), Params);
        } catch (Exception Ex) {
            CatchData(Ex.Message);
            return true;
        }
    }

    public async Task<bool> DeleteUser (string Id) {
        try {
            SqliteParameter[] Params = [
                new SqliteParameter("$id", Id)
            ];
            return await ExecuteCommand(Querys.GetDeleteUser(), Params);
        } catch (Exception Ex) {
            CatchData(Ex.Message);
            return true;
        }
    }

    public async Task<string> CheckProfile (string IdUser) {
        try {
            ClientUser User = await GetById(IdUser) ?? new();
            return User.Profile ?? string.Empty;
        } catch (Exception Ex) {
            CatchData(Ex.Message);
            return string.Empty;
        }
    }

    public async Task UpdateProfile (string Id, string Profile) {
        try {
            SqliteParameter[] Params = [
               new SqliteParameter("$id", Id),
               new SqliteParameter("$Profile", Profile)
            ];
            await ExecuteCommand(Querys.GetUpdateStatus(), Params);
        } catch (Exception Ex) {
            CB.RegisterFailure();
            Logging.Error(Ex.Message);
            Metrics.LogError();
        }
    }

    public async Task<bool> UserExist (string Id) {
        try {
            ClientUser User = await GetById(Id) ?? new();
            return !string.IsNullOrEmpty(User.Id);
        } catch (Exception Ex) {
            CB.RegisterFailure();
            Logging.Error(Ex.Message);
            Metrics.LogError();
            return false;
        }
    }

    public async Task Activate (string id) {
        await UpdateStatus(id, true);
    }

    public async Task Deactivate (string id) {
        await UpdateStatus(id, false);
    }

    private async Task UpdateStatus (string Id, bool Active) {
        try {
            SqliteParameter[] Params = [
               new SqliteParameter("$id", Id),
               new SqliteParameter("$active", Active ? 1 : 0)
            ];
            await ExecuteCommand(Querys.GetUpdateStatus(), Params);
        } catch (Exception Ex) {
            CB.RegisterFailure();
            Logging.Error(Ex.Message);
            Metrics.LogError();
        }
    }

    public async Task UpdateMetrics (string IdUser, bool Active) {
        try {
            SqliteParameter[] Params = [
               new SqliteParameter("$id", IdUser),
               new SqliteParameter("$active", Active ? 1 : 0)
            ];
            await ExecuteCommand(Querys.GetUpdateStatus(), Params);
        } catch (Exception Ex) {
            CB.RegisterFailure();
            Logging.Error(Ex.Message);
            Metrics.LogError();
        }
    }
}