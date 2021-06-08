using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace BeatsaverBeatmapAvailability.Utilities
{
    public class Utilities
    {

        public static bool IsWIPLevel(IPreviewBeatmapLevel previewBeatmapLevel)
        {
            return SongCore.Loader.CustomWIPLevels.Any(x => x.Value.levelID.Equals(previewBeatmapLevel.levelID));
        }

        public static IEnumerator DoAfter(float time, Action action)
        {
            float start = Time.fixedTime;
            while (start + time > Time.fixedTime)
                yield return null;
            action?.Invoke();
            yield break;
        }

    }
}
