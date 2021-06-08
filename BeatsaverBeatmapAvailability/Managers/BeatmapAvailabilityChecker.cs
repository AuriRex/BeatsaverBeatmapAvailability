using BeatsaverBeatmapAvailability.Models;
using BeatsaverBeatmapAvailability.UI;
using BeatSaverSharp;
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
    class BeatmapAvailabilityChecker
    {
        private readonly BeatSaverAPIManager _beatSaverAPIManager;
        private readonly BeatmapAvailabilityCacheManager _beatmapAvailabilityCacheManager;

        public BeatmapAvailabilityChecker(BeatSaverAPIManager beatSaverAPIManager, BeatmapAvailabilityCacheManager beatmapAvailabilityCacheManager)
        {
            _beatSaverAPIManager = beatSaverAPIManager;
            _beatmapAvailabilityCacheManager = beatmapAvailabilityCacheManager;
        }

        internal async Task<AvailabilityData> CheckIfBeatmapIsAvailable(IPreviewBeatmapLevel previewBeatmapLevel)
        {
            return await CheckIfBeatmapIsAvailableByHash(SongCore.Collections.hashForLevelID(previewBeatmapLevel.levelID));
        }

        public async Task<AvailabilityData> CheckIfBeatmapIsAvailableByHash(string hash)
        {
            Availability availability = Availability.Offline;
            Beatmap beatmap = null;
            string statusText;
            if (_beatmapAvailabilityCacheManager.HashIsCached(hash))
            {
                statusText = $"BeatSaverKey: {_beatmapAvailabilityCacheManager.GetOldBeatSaverKeyIfAvailable(hash)}";
                if (_beatmapAvailabilityCacheManager.IsProbablyOnline(hash))
                    availability = Availability.Online;
                else if (_beatmapAvailabilityCacheManager.IsOffline(hash))
                    availability = Availability.Offline;
            }
            else
            {
                // Beatmap not cached, contact BeatSaver and cache it.
                beatmap = await _beatSaverAPIManager.GetMapByHash(hash);
                if (BeatSaverAPIManager.MapIsOffline(beatmap))
                {
                    SongCore.Loader.CustomLevels.TryGetValue(hash, out var localBeatmapPreview);
                    _beatmapAvailabilityCacheManager.AddOfflineBeatmap(hash, localBeatmapPreview?.songName ?? hash);
                    statusText = $"BeatSaverKey: {_beatmapAvailabilityCacheManager.GetOldBeatSaverKeyIfAvailable(hash)}";
                    availability = Availability.Offline;
                }
                else
                {
                    _beatmapAvailabilityCacheManager.AddOnlineBeatmap(hash, beatmap);
                    statusText = $"BeatSaverKey: {beatmap.Key}";
                    availability = Availability.Online;
                }
            }

            return new AvailabilityData() {
                Availability = availability,
                Beatmap = beatmap,
                KeyText = statusText
            };
        }
    }
}
