using BeatSaverSharp;
using BeatsaverBeatmapAvailability.Configuration;
using SongDataCore.BeatStar;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace BeatsaverBeatmapAvailability.Managers
{
    public class BeatmapAvailabilityCacheManager : IInitializable, IDisposable
    {
        private readonly PluginConfig _pluginConfig;
        private readonly SongDataCoreZenjectWrapper _songDataCoreZenjectWrapper;

        private Dictionary<string, string> _offlineBeatmapsCache; // hash, name
        private Dictionary<string, (float, Beatmap)> _onlineBeatmapsCache;

        public const string kUnknown = "Unknown";

        internal BeatmapAvailabilityCacheManager(PluginConfig pluginConfig, SongDataCoreZenjectWrapper songDataCoreZenjectWrapper)
        {
            _pluginConfig = pluginConfig;
            _songDataCoreZenjectWrapper = songDataCoreZenjectWrapper;
        }

        /// <summary>
        /// Time in seconds after which cached "Online" Beatmaps are considered stale.
        /// Default is 1 hour
        /// </summary>
        public float OnlineCacheStaleTime { get; private set; } = 3600;

        public void Initialize()
        {
            _offlineBeatmapsCache = new Dictionary<string, string>();
            _onlineBeatmapsCache = new Dictionary<string, (float, Beatmap)>();

            LoadPersistentOfflineHashes();
        }

        private void LoadPersistentOfflineHashes()
        {
            foreach(var offlineBeatmap in _pluginConfig.OfflineBeatmaps)
            {
                AddOfflineBeatmap(offlineBeatmap.Hash, offlineBeatmap.Name);
            }
        }

        private void SaveToConfig()
        {
            foreach (var beatmapThatWentOffline in _onlineBeatmapsCache.Where(x => _offlineBeatmapsCache.ContainsKey(x.Key)))
            {
                string hash = beatmapThatWentOffline.Key;
                Beatmap beatmap = beatmapThatWentOffline.Value.Item2;

                if(!_pluginConfig.OfflineBeatmaps.Any(x => x.Hash.Equals(hash)))
                {
                    _pluginConfig.OfflineBeatmaps.Add(new PluginConfig.OfflineBeatmap()
                    {
                        Name = beatmap.Name,
                        Hash = hash,
                        BeatSaverKey = beatmap.Key
                    });
                }
            }

            foreach (var offlineEntry in _offlineBeatmapsCache.Where(x => !_onlineBeatmapsCache.ContainsKey(x.Key)))
            {
                string hash = offlineEntry.Key;
                string songName = offlineEntry.Value;

                _songDataCoreZenjectWrapper.TryGetSongByHash(hash, out BeatStarSong songData);

                if (!_pluginConfig.OfflineBeatmaps.Any(x => x.Hash.Equals(hash)))
                {
                    _pluginConfig.OfflineBeatmaps.Add(new PluginConfig.OfflineBeatmap()
                    {
                        Name = songName,
                        Hash = hash,
                        BeatSaverKey = songData != null ? songData.key : kUnknown
                    });
                }
            }
        }

        public void AddOfflineBeatmap(string hash, string songName)
        {
            if (string.IsNullOrEmpty(hash)) throw new ArgumentException($"{nameof(hash)} argument can not be null or empty!");
            _offlineBeatmapsCache.Add(hash, songName);
        }

        public void AddOnlineBeatmap(string hash, Beatmap beatmap)
        {
            if (string.IsNullOrEmpty(hash)) throw new ArgumentException($"{nameof(hash)} argument can not be null or empty!");
            if (beatmap == null) throw new ArgumentException($"{nameof(beatmap)} argument can not be null!");

            if (_onlineBeatmapsCache.ContainsKey(hash)) _onlineBeatmapsCache.Remove(hash);
            _onlineBeatmapsCache.Add(hash, (Time.realtimeSinceStartup, beatmap));
        }

        /// <summary>
        /// Checks if a map is cached.
        /// </summary>
        /// <param name="hash">Beatmap hash</param>
        /// <returns>true if the map is cached and has not gone stale (if in online cache)</returns>
        public bool HashIsCached(string hash)
        {
            return IsOffline(hash) || IsProbablyOnline(hash);
        }

        /// <summary>
        /// Checks if an "Online" Beatmap has gone stale.
        /// </summary>
        /// <param name="hash">Beatmap hash</param>
        /// <returns>true if the map has gone stale</returns>
        public bool CachedOnlineBeatmapIsStale(string hash)
        {
            if (_onlineBeatmapsCache.TryGetValue(hash, out (float time, Beatmap beatmap) values))
            {
                if (CachedOnlineBeatmapIsStale(values)) return true;
            }

            return false;
        }

        private bool CachedOnlineBeatmapIsStale((float time, Beatmap beatmap) values)
        {
            return values.time + OnlineCacheStaleTime < Time.realtimeSinceStartup;
        }

        public string GetOldBeatSaverKeyIfAvailable(string hash)
        {
            if(TryGetOnline(hash, out var beatmap))
            {
                return beatmap != null ? beatmap.Key : kUnknown;
            }

            if (_songDataCoreZenjectWrapper.TryGetSongByHash(hash, out var songData)) {
                return songData != null ? songData.key : kUnknown;
            }

            return kUnknown;
        }

        /// <summary>
        /// Check if a Beatmap is known to be offline.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>true if map is offline</returns>
        public bool IsOffline(string hash)
        {
            return _offlineBeatmapsCache.ContainsKey(hash);
        }

        /// <summary>
        /// Check if a map is probably still online.
        /// </summary>
        /// <param name="hash">Beatmap hash</param>
        /// <returns>true if the map is in the online beatmaps cache and has not gone stale</returns>
        public bool IsProbablyOnline(string hash)
        {
            if (CachedOnlineBeatmapIsStale(hash)) return false;

            return _onlineBeatmapsCache.ContainsKey(hash);
        }

        public bool TryGetOnline(string hash, out Beatmap beatmap)
        {
            if (string.IsNullOrEmpty(hash)) throw new ArgumentException($"{nameof(hash)} argument can not be null or empty!");

            if (_onlineBeatmapsCache.TryGetValue(hash, out (float time, Beatmap beatmap) values))
            {
                beatmap = values.beatmap;
                return true;
            }
            beatmap = null;
            return false;
        }

        public void Dispose()
        {
            SaveToConfig();
            _offlineBeatmapsCache.Clear();
            _onlineBeatmapsCache.Clear();
            _offlineBeatmapsCache = null;
            _onlineBeatmapsCache = null;
        }
    }
}
