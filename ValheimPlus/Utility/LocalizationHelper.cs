using System;
using HarmonyLib;

namespace ValheimPlus
{
    internal static class LocalizationHelper
    {
        private static bool initialized;
        private static Func<string, string> localize;

        internal static string Localize(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            EnsureInitialized();

            if (localize != null)
            {
                try
                {
                    return localize(text);
                }
                catch
                {
                    // Fall back to raw text if localization fails.
                }
            }

            return text;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
                return;

            initialized = true;

            try
            {
                var type = AccessTools.TypeByName("Localization");
                if (type == null)
                    return;

                object instance = AccessTools.Property(type, "instance")?.GetValue(null) ??
                                  AccessTools.Property(type, "Instance")?.GetValue(null) ??
                                  AccessTools.Field(type, "instance")?.GetValue(null) ??
                                  AccessTools.Field(type, "m_instance")?.GetValue(null) ??
                                  AccessTools.Field(type, "s_instance")?.GetValue(null);

                if (instance == null)
                    return;

                var method = AccessTools.Method(type, "Localize", new[] { typeof(string) }) ??
                             AccessTools.Method(type, "Localize");

                if (method == null)
                    return;

                localize = text => (string)method.Invoke(instance, new object[] { text });
            }
            catch (Exception ex)
            {
                ValheimPlusPlugin.Logger?.LogWarning($"Localization helper init failed: {ex}");
            }
        }
    }
}
