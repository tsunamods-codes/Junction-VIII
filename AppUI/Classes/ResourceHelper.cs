using System;
using System.Collections.Generic;
using System.Linq;
using AppCore;
using AppUI;

namespace AppUI.Classes
{
    public static class ResourceHelper
    {
        public static string Get(StringKey key)
        {
            try
            {
                return App.Current.Resources[key.ToString()].ToString().Replace("\\n", "\n");
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Mod name followed by the versions a compatibility rule names, for example
        /// "Archeology (1.3 or later)" or "Archeology (1.0, 1.3 to 2.0)".
        /// Just the name when the rule names no versions.
        /// </summary>
        public static string NameWithVersionRules(string modName, AppWrapper.CompatEntry entry)
        {
            var parts = new List<string>();
            if (entry.Versions != null)
                parts.AddRange(entry.Versions.Select(AppWrapper.ModVersionComparer.ToText));

            bool hasMin = AppWrapper.ModVersionComparer.TryParse(entry.MinVersion, out _);
            bool hasMax = AppWrapper.ModVersionComparer.TryParse(entry.MaxVersion, out _);
            if (hasMin && hasMax)
                parts.Add(
                    string.Format(
                        Get(StringKey.ModVersionFromTo),
                        entry.MinVersion,
                        entry.MaxVersion
                    )
                );
            else if (hasMin)
                parts.Add(string.Format(Get(StringKey.ModVersionOrLater), entry.MinVersion));
            else if (hasMax)
                parts.Add(string.Format(Get(StringKey.ModVersionOrEarlier), entry.MaxVersion));

            if (parts.Count == 0)
                return modName;

            return $"{modName} ({string.Join(", ", parts)})";
        }

        /// <summary>
        /// Maps the translated mod category to the respective english version
        /// </summary>
        public static Dictionary<string, string> ModCategoryTranslations
        {
            get
            {
                return new Dictionary<string, string>()
                {
                    { ResourceHelper.Get(StringKey.Miscellaneous), "Miscellaneous" },
                    { ResourceHelper.Get(StringKey.Animations), "Animations" },
                    { ResourceHelper.Get(StringKey.BattleModels), "Battle Models" },
                    { ResourceHelper.Get(StringKey.BattleTextures), "Battle Textures" },
                    { ResourceHelper.Get(StringKey.FieldModels), "Field Models" },
                    { ResourceHelper.Get(StringKey.FieldTextures), "Field Textures" },
                    { ResourceHelper.Get(StringKey.Gameplay), "Gameplay" },
                    { ResourceHelper.Get(StringKey.Media), "Media" },
                    { ResourceHelper.Get(StringKey.Minigames), "Minigames" },
                    { ResourceHelper.Get(StringKey.SpellTextures), "Spell Textures" },
                    { ResourceHelper.Get(StringKey.UserInterface), "User Interface" },
                    { ResourceHelper.Get(StringKey.WorldModels), "World Models" },
                    { ResourceHelper.Get(StringKey.WorldTextures), "World Textures" },
                    { ResourceHelper.Get(StringKey.ModShaders), "Shaders" },
                    { ResourceHelper.Get(StringKey.Unknown), "Unknown" },
                };
            }
        }
    }
}
