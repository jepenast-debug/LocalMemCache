namespace LocalCache.Domain.General {
    public interface ISettingsLoad {

        Settings LoadSettings ();

        string GetString (string Key, string DefaultValue);

        int GetInt (string Key, int DefaultValue);

        bool GetBool (string key, bool defaultValue);

    }
}
