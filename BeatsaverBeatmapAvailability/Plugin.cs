using IPA;
using IPA.Config;
using IPA.Config.Stores;
using IPA.Loader;
using BeatsaverBeatmapAvailability.Installers;
using BeatsaverBeatmapAvailability.Models;
using SiraUtil.Zenject;
using IPALogger = IPA.Logging.Logger;

namespace BeatsaverBeatmapAvailability
{
    [Plugin(RuntimeOptions.DynamicInit)]
    public class Plugin
    {
        [Init]
        public void Init(IPALogger logger, Zenjector zenjector, Config conf, PluginMetadata pluginMetadata)
        {
            Logger.log = logger;

            var modMeta = new ModMeta()
            {
                PluginMetadata = pluginMetadata
            };

            zenjector.OnApp<BeatmapIsOnlineCoreInstaller>().WithParameters(conf.Generated<Configuration.PluginConfig>(), modMeta);
            zenjector.OnMenu<BeatmapIsOnlineMenuInstaller>();
        }

        [OnEnable]
        public void OnApplicationStart()
        {
            
        }

        [OnDisable]
        public void OnApplicationQuit()
        {

        }
    }
}
