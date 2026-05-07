using LocalCache.Domain.Entities;
using LocalCache.Infrastructure.Logging;
using LocalCache.Infrastructure.Persistence;
using LocalCache.Infrastructure.Security;
using LocalMemCache.Configuration;

namespace LocalMemCache.Core {
    public class ManageApp {

        private readonly DBRepository DBRepo;
        private readonly FileLogger Logger;
        private readonly ConfigReader Cfg;
        private readonly ManageCache Cache;

        public ManageApp (DBRepository dbrepo, ref ManageCache Engine) {
            DBRepo = dbrepo;
            Cache = Engine;
            Cfg = new();
            Logger = new(Cfg.LoadSettings().PathLog);
        }

        public async Task<string> ExecuteCommand (string User, string[] Data) {
            try {
                //TODO:
                //de los argumentos deben salir los comandos para la ejecucion esperada
                //Codigo para administrar la aplicacion
                //Cambiar la configuracion
                //reCheckProfileiniciar el servicio
                //Limpiar cache
                //Administrar usuarios

                // Protección contra inyección / validación de tamaño básica
                if (Data.Any(a => a.Length > 50000)) return "ERR value too large";
                //Separar los comandos
                string[] Args = Data[0].Split(' ');
                string Cmd = Args[0].ToUpperInvariant();
                if (await DBRepo.CheckProfile(User) == "Admin") {
                    return Cmd switch {
                        //Agregar un usuario id,pdw,perfil,quien asigna
                        "ADD_USER" => await AddUser(Args[1], Args[2], Args[3], User),
                        //Actualizar contraseña del usuario id,oldpwd,newpwd, quien asigna
                        "UDT_PWD" => await UpdatePassword(Args[1], Args[2], Args[3], User),
                        //Eliminar un usuario
                        "DEL_USER" => await DeleteUser(User, Args[1]),
                        //Cambiar el perfil del usuario id, newprofile
                        "PERF_USER" => await ChangeProfile(Args[1], Args[2], User),
                        //Eliminar todos los datos de cache
                        "CLEAR_ALL" => (string)await ClearAllCache(User),
                        //Cambiar una configuracion key, value, user
                        "CHG_CFG" => await ChangeConfig(Args[1], Args[2], User),
                        _ => throw new NotImplementedException(),
                    };
                } else {
                    return "ERROR No authorized";
                }
            } catch (Exception ex) {
                Logger.Error($"Client Error: {ex.Message}");
                return "Error exec command";
            }
        }
        private async Task<string> AddUser (string NameUser, string Pwd, string Profile, string User) {
            try {
                if (NameUser.Length < 5) {
                    return "Name user to short, required minimun 5 and maximun 10 characters";
                }
                if (Pwd.Length < 12) {
                    return "Paswword to short minimum 12 and maximun 32 characters";
                }
                ClientUser NUser = new() {
                    Id = NameUser,
                    PwdHash = StringHasher.Hash(Pwd),
                    IsActive = true,
                    Profile = Profile
                };
                var Result = DBRepo.AddUser(NUser);
                Logger.Error("User " + User + " has added:, result: " + Result.ToString());
                return "Ok";
                //Guardar el dato
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                return "ERROR";
            }
        }

        private async Task<string> UpdatePassword (string UserChg, string OldPwd, string NewPwd, string User) {
            try {
                ClientUser NUser = new();
                ClientUser UserA = new();
                if (await DBRepo.UserExist(UserChg) == false) {
                    return "User not found";
                }
                if (NewPwd.Length < 12) {
                    return "Password must be 12-32 characters long.";
                }
                OldPwd = StringHasher.Hash(OldPwd);
                if (OldPwd != StringHasher.Hash(NewPwd)) {
                    UserA = await DBRepo.GetById(UserChg) ?? new();
                    if (UserA.PwdHash == OldPwd || UserA.Id == string.Empty) {
                        NUser = new() {
                            Id = UserChg,
                            PwdHash = StringHasher.Hash(NewPwd),
                            IsActive = true
                        };
                    } else {
                        return "Username not found or incorrect password.";
                    }
                    //Validar tambien que la antigua contraseña concuerde
                    var Result = DBRepo.UpdatePwd(NUser);
                    Logger.Error("User " + User + " has added:, result: " + Result.ToString());
                    return "Ok";
                } else {
                    return "The new password cannot be the same as the old one.";
                }
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                return "ERROR";
            }
        }

        public async Task<string> DeleteUser (string User, string DeleteUser) {
            try {
                if (await DBRepo.UserExist(DeleteUser) == false) {
                    return "User not found";
                }
                var Result = DBRepo.DeleteUser(DeleteUser);
                Logger.Error("the user " + User + " has deleted, result: " + Result.ToString());
                return "OK";
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                return "ERROR";
            }
        }

        public async Task<string> ChangeProfile (string NameUser, string NewProfile, string User) {
            try {
                if (await DBRepo.UserExist(NameUser) == false) {
                    return "User not found";
                }
                ClientUser UserA = await DBRepo.GetById(NameUser) ?? new();
                if (UserA.Profile == NewProfile) {
                    await DBRepo.UpdateProfile(NameUser, NewProfile);
                }
                Logger.Error("the user " + User + " has change profile to " + NameUser);
                return "OK";
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                return "ERROR";
            }
        }

        public async Task<string> ChangeConfig (string Key, string Value, string User) {
            try {
                if (await DBRepo.UserExist(User) == false) {
                    return "User not found";
                }
                Cfg.SetSetting(Key, Value);
                Logger.Log(User + " Change settings");
                return "OK";
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                return "ERROR";
            }
        }

        public async Task<string> ClearAllCache (string User) {
            string Result = await Cache.Clear();
            Logger.Log(User + " Clear all data in cache");
            return Result;
        }
    }
}
