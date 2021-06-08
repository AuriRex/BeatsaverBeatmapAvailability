
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace BeatsaverBeatmapAvailability.Configuration
{
    internal class PluginConfig
    {
        public event Action onConfigChanged;

        public virtual bool Enabled { get; set; } = true;
        public virtual bool AutoCheck { get; set; } = false;

        public virtual ButtonData ButtonSettings { get; set; } = new ButtonData();

        [NonNullable, UseConverter(typeof(ListConverter<ButtonData>))]
        public virtual List<ButtonData> ButtonPresets { get; set; } = new List<ButtonData>() {
            new ButtonData(),
            new ButtonData()
            {
                Position = new SVector2(40, -40)
            }
        };

        [NonNullable, UseConverter(typeof(ListConverter<OfflineBeatmap>))]
        public virtual List<OfflineBeatmap> OfflineBeatmaps { get; set; } = new List<OfflineBeatmap>();

        public class OfflineBeatmap
        {
            public virtual string Name { get; set; }
            public virtual string Hash { get; set; }
            public virtual string BeatSaverKey { get; set; }
        }

        public class ButtonData
        {
            public virtual SVector2 Position { get; set; } = new SVector2(24, 5);
            public virtual float Scale { get; set; } = 1f;
        }

        public class SVector2
        {
            public SVector2()
            {
            }
            public SVector2(float x, float y)
            {
                X = x;
                Y = y;
            }

            public virtual float X { get; set; } = 0;
            public virtual float Y { get; set; } = 0;

            public Vector3 ToVector3()
            {
                return new Vector3(X,Y,0);
            }

            public void FromVector3(Vector3 vector)
            {
                X = vector.x;
                Y = vector.y;
            }
        }

        public class SVector3
        {
            public SVector3()
            {
            }
            public SVector3(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public virtual float X { get; set; } = 0;
            public virtual float Y { get; set; } = 0;
            public virtual float Z { get; set; } = 0;

            public Vector3 ToVector3()
            {
                return new Vector3(X, Y, Z);
            }

            public void FromVector3(Vector3 vector)
            {
                X = vector.x;
                Y = vector.y;
                Z = vector.z;
            }
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
            onConfigChanged?.Invoke();
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
