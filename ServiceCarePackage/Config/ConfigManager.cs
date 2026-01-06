using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ServiceCarePackage.Models;
using ServiceCarePackage.Services.Logs;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceCarePackage.Config
{
    public class ConfigManager
    {
        private readonly IDalamudPluginInterface pi;
        private readonly JsonSerializerOptions jsonOptions;
        private readonly IPlayerState playerState;
        private readonly ILog log;

        public CharacterKey? CurrentKey { get; private set; }
        public CharacterConfiguration Current { get; private set; } = new();

        public ConfigManager(ILog log, IDalamudPluginInterface pi, IPlayerState playerState)
        {
            this.log = log;
            this.pi = pi;
            this.playerState = playerState;
            jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        private string BaseDir => Path.Combine(pi.GetPluginConfigDirectory(), "CharacterConfigs");

        public void LoadForCurrentCharacter()
        {
            log.Debug($"Loading CharConfig for current character");
            if (playerState != null && playerState.IsLoaded)
            {
                LoadFor(new CharacterKey(playerState.CharacterName, playerState.HomeWorld.Value.Name.ToString()));
            }
        }

        public void LoadFor(CharacterKey key)
        {
            log.Debug($"Loading CharConfig for {key.ToString()}");
            Directory.CreateDirectory(BaseDir);

            CurrentKey = key;

            var path = GetPathFor(key);
            if (!File.Exists(path))
            {
                Current = new CharacterConfiguration();
                Save(); // create file on first use (optional)
                return;
            }

            try
            {
                var json = File.ReadAllText(path);
                Current = JsonSerializer.Deserialize<CharacterConfiguration>(json, jsonOptions) ?? new CharacterConfiguration();                
            }
            catch
            {
                // If corrupted, fall back to defaults (optionally rename bad file)
                Current = new CharacterConfiguration();
            }

            FixedConfig.LoadFromConfig(Current);
        }

        public void Save()
        {
            if (CurrentKey == null && playerState != null && playerState.IsLoaded)
            {
                CurrentKey = new(playerState.CharacterName, playerState.HomeWorld.Value.Name.ToString());
            }
            log.Debug($"Saving CharConfig for {CurrentKey?.ToString()}");
            if (CurrentKey is null) return;

            Directory.CreateDirectory(BaseDir);
            var path = GetPathFor(CurrentKey);
            var json = JsonSerializer.Serialize(Current, jsonOptions);
            File.WriteAllText(path, json);

            FixedConfig.LoadFromConfig(Current);
        }

        private string GetPathFor(CharacterKey key)
            => Path.Combine(BaseDir, $"{MakeSafeFileName(key.ToString())}.json");

        private static string MakeSafeFileName(string s)
        {
            // Windows-safe + predictable
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');

            // also avoid weird separators
            s = s.Replace('/', '_').Replace('\\', '_').Replace(':', '_');

            return s;
        }
    }
}
