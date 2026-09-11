using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace ValheimPlus.Configurations
{
    /// <summary>
    /// Soft-dependency glue for the BepInEx Configuration Manager plugin. Decides when a settings
    /// change is accepted, and reapplies the Harmony patches once one is. Does nothing when
    /// Configuration Manager is not installed.
    /// </summary>
    internal static class ConfigurationManagerWatcher
    {
        internal const string ConfigurationManagerGuid = "com.bepis.bepinex.configurationmanager";

        // Shudnal's Valheim fork, same API. The two refuse to load together.
        internal const string ShudnalConfigurationManagerGuid = "_shudnal.ConfigurationManager";

        private static readonly List<BaseConfig> Sections = new();

        private static object plugin;
        private static PropertyInfo displayingWindow;
        private static MethodInfo buildSettingList;

        private static bool atMainMenu = true;
        private static bool legacyOverride;
        private static bool readyForEdit = true;
        private static bool windowShown;
        private static bool dirty;

        /// <summary>Whether a settings change would be accepted right now.</summary>
        public static bool ReadyForEdit => readyForEdit;

        /// <summary>True while the settings window is open in a world, see Menu_IsVisible_Patch.</summary>
        public static bool BlocksGameInput => windowShown && !atMainMenu;

        public static void Install(ConfigFile config, List<BaseConfig> sections, bool legacyOverrideActive)
        {
            Sections.Clear();
            Sections.AddRange(sections);
            legacyOverride = legacyOverrideActive;

            config.SettingChanged += (_, _) => dirty = true;

            // Applied before hooking up, so the first window built has the right state.
            ApplyReadyForEdit(force: true);

            // BepInDependency on ValheimPlusPlugin makes BepInEx load Configuration Manager first when
            // it is present, so it is already registered by the time this runs.
            var info = FindPlugin(ConfigurationManagerGuid) ?? FindPlugin(ShudnalConfigurationManagerGuid);
            if (info == null)
            {
                ValheimPlusPlugin.Logger.LogDebug(
                    "Configuration Manager is not installed. Settings can still be changed by editing " +
                    "the config file, which takes effect on the next launch.");
                return;
            }

            try
            {
                plugin = info.Instance;
                displayingWindow = plugin.GetType().GetProperty("DisplayingWindow");
                buildSettingList = plugin.GetType().GetMethod("BuildSettingList", Type.EmptyTypes);

                var windowChanged = plugin.GetType().GetEvent("DisplayingWindowChanged");
                if (displayingWindow == null || windowChanged == null)
                {
                    ValheimPlusPlugin.Logger.LogWarning(
                        "Configuration Manager is installed but does not look the way we expect, so " +
                        "changed settings will only apply on the next launch.");
                    return;
                }

                // The event is EventHandler<ValueChangedEventArgs<bool>> and that argument type lives in
                // Configuration Manager, which we deliberately do not reference. Relaxed delegate binding
                // lets a handler taking plain object stand in for it.
                Action<object, object> handler = OnDisplayingWindowChanged;
                windowChanged.AddEventHandler(plugin,
                    Delegate.CreateDelegate(windowChanged.EventHandlerType, handler.Target, handler.Method));

                ValheimPlusPlugin.Logger.LogDebug(readyForEdit
                    ? "Configuration Manager found, settings are editable at the main menu."
                    : "Configuration Manager found, but the legacy config file is overriding settings, " +
                      "so they are read-only.");
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning($"Could not hook into Configuration Manager: {e.Message}");
            }
        }

        private static BepInEx.PluginInfo FindPlugin(string guid) =>
            Chainloader.PluginInfos.TryGetValue(guid, out var info) && info?.Instance != null ? info : null;

        /// <summary>Called when a world is entered or left.</summary>
        public static void SetInWorld(bool inWorld)
        {
            atMainMenu = !inWorld;
            ApplyReadyForEdit(force: false);
        }

        /// <summary>Forgets that settings were changed, once the patches have been reapplied.</summary>
        public static void MarkClean() => dirty = false;

        private static void ApplyReadyForEdit(bool force)
        {
            var ready = atMainMenu && !legacyOverride;
            if (!force && ready == readyForEdit) return;
            readyForEdit = ready;

            var note = legacyOverride ? "read-only, legacy cfg" : "read-only in a world";
            foreach (var section in Sections) section.SetEditable(ready, note);

            if (!force) RefreshOpenWindow();
        }

        /// <summary>
        /// Rebuilds an open settings window, which is otherwise holding tags from before the change.
        /// Closing it is the fallback, since that rebuilds on the next open.
        /// </summary>
        private static void RefreshOpenWindow()
        {
            if (plugin == null || displayingWindow == null) return;

            try
            {
                if (!(bool)displayingWindow.GetValue(plugin, null)) return;

                if (buildSettingList != null) buildSettingList.Invoke(plugin, null);
                else displayingWindow.SetValue(plugin, false, null);
            }
            catch (Exception e)
            {
                ValheimPlusPlugin.Logger.LogWarning(
                    $"Could not refresh the Configuration Manager window: {e.Message}");
            }
        }

        private static void OnDisplayingWindowChanged(object sender, object args)
        {
            windowShown = (bool)displayingWindow.GetValue(plugin, null);

            // Only the close is interesting; the window has no commit step of its own.
            if (windowShown) return;
            if (!dirty) return;

            if (!readyForEdit)
            {
                // Normally ServerSync applying a server's values, which reapplies patches itself.
                ValheimPlusPlugin.Logger.LogDebug(
                    "Settings changed while they are read-only, so patches were left alone.");
                return;
            }

            dirty = false;
            ValheimPlusPlugin.Logger.LogDebug("Configuration changed, re-applying patches.");
            ValheimPlusPlugin.UnpatchSelf();
            ValheimPlusPlugin.PatchAll();
        }
    }
}
