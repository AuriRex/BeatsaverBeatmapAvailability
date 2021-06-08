using SongDataCore.BeatStar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zenject;

namespace BeatsaverBeatmapAvailability.Managers
{
    public class SongDataCoreZenjectWrapper : IInitializable, IDisposable
    {
        public bool IsReady { get; private set; } = false;

        public event Action OnDataFinishedProcessing;

        public void Initialize()
        {
            _ = Task.Run(async () => {
                await Task.Delay(200);
                if (SongDataCore.Plugin.Songs == null) return;
                SongDataCore.Plugin.Songs.OnDataFinishedProcessing += HandleOnDataFinishedProcessing;
            });
        }

        public bool TryGetSongByHash(string hash, out BeatStarSong songData)
        {
            songData = null;
            if (!IsReady) return false;
            return SongDataCore.Plugin.Songs?.Data?.Songs?.TryGetValue(hash, out songData) ?? false;
        }

        public void HandleOnDataFinishedProcessing()
        {
            IsReady = true;
            OnDataFinishedProcessing?.Invoke();
        }

        public void Dispose()
        {
            IsReady = false;
            if(SongDataCore.Plugin.Songs != null)
            {
                SongDataCore.Plugin.Songs.OnDataFinishedProcessing -= HandleOnDataFinishedProcessing;
            }
            
        }
    }
}
