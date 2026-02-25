using System.Reflection;
using MelonLoader;
using S1API.Internal.Utils;
using UnityEngine;

namespace WeaponShipments.Utils
{
    /// <summary>
    /// Loads quest-specific icons from embedded resources.
    /// Add PNG files to Resources/ with names like quest_steal.png, quest_sell.png, etc.
    /// </summary>
    public static class QuestIconLoader
    {
        /// <summary>Load a quest icon by filename (e.g. "quest_steal.png"). Returns null if not found.</summary>
        public static Sprite? Load(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;

            try
            {
                var asm = Assembly.GetExecutingAssembly();
                var target = fileName.ToLowerInvariant();

                foreach (var name in asm.GetManifestResourceNames())
                {
                    if (!name.ToLowerInvariant().EndsWith(target))
                        continue;

                    using (var stream = asm.GetManifestResourceStream(name))
                    {
                        if (stream == null) continue;

                        var data = new byte[stream.Length];
                        stream.Read(data, 0, data.Length);
                        return ImageUtils.LoadImageRaw(data);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MelonLogger.Warning($"[QuestIconLoader] Failed to load {fileName}: {ex.Message}");
            }

            return null;
        }
    }
}
