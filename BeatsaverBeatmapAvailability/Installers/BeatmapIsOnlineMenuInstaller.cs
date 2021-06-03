using BeatsaverBeatmapAvailability.Managers;
using BeatsaverBeatmapAvailability.UI;
using Zenject;

namespace BeatsaverBeatmapAvailability.Installers
{
    class BeatmapIsOnlineMenuInstaller : Installer
    {
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<ButtonManager>().AsSingle();
            Container.BindInterfacesAndSelfTo<SongDataCoreZenjectWrapper>().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatmapAvailabilityChecker>().AsSingle();
            Container.BindInterfacesAndSelfTo<BeatmapAvailabilityCacheManager>().AsSingle();
        }
    }
}
