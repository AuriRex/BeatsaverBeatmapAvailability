using IPA.Loader;
using BeatsaverBeatmapAvailability.Configuration;
using BeatsaverBeatmapAvailability.Managers;
using BeatsaverBeatmapAvailability.Models;
using Zenject;

namespace BeatsaverBeatmapAvailability.Installers
{
    class BeatmapIsOnlineCoreInstaller : Installer
    {
        private readonly PluginConfig _pluginConfig;
        private readonly ModMeta _modMeta;

        public BeatmapIsOnlineCoreInstaller(PluginConfig pluginConfig, ModMeta modMeta)
        {
            _pluginConfig = pluginConfig;
            _modMeta = modMeta;
        }

        public override void InstallBindings()
        {
            Container.Bind<PluginConfig>().FromInstance(_pluginConfig).AsSingle();
            Container.Bind<ModMeta>().FromInstance(_modMeta).AsSingle();

            Container.BindInterfacesAndSelfTo<BeatSaverAPIManager>().AsSingle();
        }
    }
}
