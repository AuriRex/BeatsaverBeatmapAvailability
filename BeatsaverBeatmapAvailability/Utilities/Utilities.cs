using System;
using System.Collections;
using UnityEngine;

namespace BeatsaverBeatmapAvailability.Utilities
{
    public class Utilities
    {


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
