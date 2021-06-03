using BeatSaverSharp;
using IPA.Loader;
using BeatsaverBeatmapAvailability.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace BeatsaverBeatmapAvailability.Managers
{
    public class BeatSaverAPIManager : IInitializable, IDisposable
    {
        private readonly PluginMetadata _pluginMetadata;
        private BeatSaver _beatsaver;

        public BeatSaverAPIManager(ModMeta modMeta)
        {
            _pluginMetadata = modMeta.PluginMetadata;
        }

        public void Initialize()
        {
            // Thanks Kyle <3
            string steamDllPath = Path.Combine(IPA.Utilities.UnityGame.InstallPath, "Beat Saber_Data", "Plugins", "steam_api64.dll");
            bool hasSteamDll = File.Exists(steamDllPath);
            string platform = hasSteamDll ? "steam" : "oculus";
            string gameVersionFull = $"{IPA.Utilities.UnityGame.GameVersion.ToString()}-{platform}";

            var httpAgent = new HttpAgent("BeatSaber", gameVersionFull);
            var agentList = new List<HttpAgent> { httpAgent };
            var httpOptions = new HttpOptions(
                _pluginMetadata.Name,
                Assembly.GetExecutingAssembly().GetName().Version,
                agents: agentList
            );

            _beatsaver = new BeatSaver(httpOptions);
        }

#nullable enable
        public async Task<Beatmap?> GetMapByHash(string hash)
        {
            return await _beatsaver.Hash(hash);
        }

        public static bool MapIsOffline(Beatmap? beatmap)
        {
            return beatmap == null; // lol
        }
#nullable restore

        public void Dispose()
        {
            if (_beatsaver != null)
            {
                _beatsaver.Dispose();
            }
        }
    }
}
