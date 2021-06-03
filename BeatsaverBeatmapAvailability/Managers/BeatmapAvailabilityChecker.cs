using BeatsaverBeatmapAvailability.UI;
using SongDataCore.BeatStar;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace BeatsaverBeatmapAvailability.Managers
{
    // TODO: actually use this instead of the spaghetti in ButtonManager
    class BeatmapAvailabilityChecker : IInitializable, IDisposable
    {
        private readonly SongDataCoreZenjectWrapper _songDataCoreZenjectWrapper;
        private readonly ButtonManager _buttonManager;

        public bool IsReady
        {
            get
            {
                return SongCore.Loader.AreSongsLoaded && _songDataCoreZenjectWrapper.IsReady;
            }
        }

        public BeatmapAvailabilityChecker(SongDataCoreZenjectWrapper songDataCoreZenjectWrapper, ButtonManager buttonManager)
        {
            _songDataCoreZenjectWrapper = songDataCoreZenjectWrapper;
            _buttonManager = buttonManager;
        }

        public void Initialize()
        {
            _buttonManager.OnButtonPressed += _buttonManager_OnButtonPressed;
        }

        private void _buttonManager_OnButtonPressed(string hash, string songName, bool isWIP)
        {
            if (string.IsNullOrEmpty(hash)) return;
            if (_songDataCoreZenjectWrapper.TryGetSongByHash(hash, out var test))
            {
                Logger.log.Debug("key_from_cached_song_data_core: " + test.key);
            }
        }

        public bool CheckIfBeatmapAvailableByHash(string hash)
        {
            return false;
        }

        private bool BeatmapIsOnline(string key)
        {

            return false;
        }

        public void Dispose()
        {
            if (_buttonManager != null)
            {
                _buttonManager.OnButtonPressed -= _buttonManager_OnButtonPressed;
            }
        }

    }
}
