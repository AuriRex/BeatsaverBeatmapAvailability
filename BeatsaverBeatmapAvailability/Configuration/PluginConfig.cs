﻿
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatsaverBeatmapAvailability.Configuration
{
    internal class PluginConfig
    {
        public virtual bool Enabled { get; set; } = true; // Must be 'virtual' if you want BSIPA to detect a value change and save the config automatically.
        public virtual bool AutoCheck { get; set; } = false;

        [NonNullable, UseConverter(typeof(ListConverter<OfflineBeatmap>))]
        public virtual List<OfflineBeatmap> OfflineBeatmaps { get; set; } = new List<OfflineBeatmap>();

        public class OfflineBeatmap
        {
            public virtual string Name { get; set; }
            public virtual string Hash { get; set; }
            public virtual string BeatSaverKey { get; set; }
        }

        /// <summary>
        /// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
        /// </summary>
        public virtual void OnReload()
        {
            // Do stuff after config is read from disk.
        }

        /// <summary>
        /// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
        /// </summary>
        public virtual void Changed()
        {
            // Do stuff when the config is changed.
        }

        /// <summary>
        /// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
        /// </summary>
        public virtual void CopyFrom(PluginConfig other)
        {
            // This instance's members populated from other
        }
    }
}