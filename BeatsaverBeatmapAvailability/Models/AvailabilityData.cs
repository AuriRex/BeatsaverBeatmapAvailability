using BeatSaverSharp;

namespace BeatsaverBeatmapAvailability.Models
{
    public enum Availability
    {
        Online,
        Offline
    }

    public struct AvailabilityData
    {
        public Availability Availability { get; internal set; }
        public Beatmap Beatmap { get; internal set; }
        public string KeyText { get; internal set; }
    }
}
