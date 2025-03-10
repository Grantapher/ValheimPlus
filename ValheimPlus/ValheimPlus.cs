using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ServerSync;
using ValheimPlus.Configurations;
using ValheimPlus.GameClasses;
using ValheimPlus.RPC;
using ValheimPlus.UI;

namespace ValheimPlus
{
    // COPYRIGHT 2021 KEVIN "nx#8830" J. // http://n-x.xyz
    // GITHUB REPOSITORY https://github.com/valheimPlus/ValheimPlus

    [BepInPlugin(ValheimPlusGuid, ValheimPlusName, NumericVersion)]
    public class ValheimPlusPlugin : BaseUnityPlugin
    {
        private const string ValheimPlusGuid = "org.bepinex.plugins.valheim_plus";
        private const string ValheimPlusName = "Valheim Plus";

        // Version used when numeric is required (assembly info, bepinex, System.Version parsing).
        public const string NumericVersion = "0.9.17.0";

        // Extra version, like alpha/beta/rc/stable. Can leave blank if a stable release.
        private const string VersionExtra = "-alpha01";

        // Version used when numeric is NOT required (Logging, config file lookup)
        public const string FullVersion = NumericVersion + VersionExtra;

        // Minimum required version for full compatibility.
        private const string MinRequiredNumericVersion = NumericVersion;

        // The lowest game version this version of V+ is known to work with.
        private static readonly GameVersion MinSupportedGameVersion = new(0, 220, 3);

        // The game version this version of V+ was compiled against.
        private static readonly GameVersion TargetGameVersion = new(0, 220, 3);

        // Versions we know for sure will not work with this game version.
        // Useful if a PTB is active to exclude it from the stable release.
        // ReSharper disable once CollectionNeverUpdated.Local
        private static readonly Dictionary<GameVersion, string> ExcludeGameVersions = new()
        {
            // example below:
            // [new GameVersion(0, 218, 19)] =
            //     $"This version of Valheim Plus ({FullVersion}) does not work with the current Valheim game version " +
            //     $"({Version.CurrentVersion}). Update the Valheim game."
        };

        internal static string newestVersion { get; private set; } = "";
        internal static bool isUpToDate { get; private set; }

        // ReSharper disable once InconsistentNaming
        public new static ManualLogSource Logger { get; private set; }

        public static readonly System.Timers.Timer MapSyncSaveTimer = new(TimeSpan.FromMinutes(5).TotalMilliseconds);

        public static readonly string VPlusDataDirectoryPath =
            Paths.BepInExRootPath + Path.DirectorySeparatorChar + "vplus-data";

        private static readonly Harmony Harmony = new("mod.valheim_plus");

        // Project Repository Info
        public const string Repository = "https://github.com/Grantapher/ValheimPlus/releases/latest";
        private const string ApiRepository = "https://api.github.com/repos/grantapher/valheimPlus/releases/latest";

        // Website INI for auto update
        internal static readonly string IniFile =
            $"https://github.com/Grantapher/ValheimPlus/releases/download/{FullVersion}/valheim_plus.cfg";

        // mod fails to load when this type is correctly specified as VersionCheck,
        // so we'll just cast it as needed instead.
        private static readonly object VersionCheck = new VersionCheck(ValheimPlusGuid)
        {
            DisplayName = "Valheim Plus",
            CurrentVersion = NumericVersion,
            MinimumRequiredVersion = MinRequiredNumericVersion,
        };

        // Awake is called once when both the game and the plug-in are loaded
        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Valheim game version: {Version.GetVersionString()}");
            Logger.LogInfo($"Valheim Plus full version: {FullVersion}");
            Logger.LogInfo($"Valheim Plus dll file location: '{GetType().Assembly.Location}'");

            var tooOld = IsGameVersionTooOld();
            if (tooOld) LogTooOld();

            var versionExcluded = CheckIsGameVersionExcluded();

            if (tooOld || versionExcluded)
            {
                Logger.LogFatal("Aborting loading of Valheim Plus due to incompatible version.");
                return;
            }

            Logger.LogInfo("Trying to load the configuration file");

            if (ConfigurationExtra.LoadSettings() != true)
            {
                Logger.LogError("Error while loading configuration file.");
            }
            else
            {
                Logger.LogInfo("Configuration file loaded successfully.");


                PatchAll();

                isUpToDate = !IsNewVersionAvailable();
                if (!isUpToDate)
                {
                    Logger.LogWarning($"There is a newer version available of ValheimPlus. Please visit {Repository}.");
                }
                else
                {
                    Logger.LogInfo($"ValheimPlus [{FullVersion}] is up to date.");
                }

                // Create VPlus dir if it does not exist.
                if (!Directory.Exists(VPlusDataDirectoryPath)) Directory.CreateDirectory(VPlusDataDirectoryPath);

                // Logo
                // if (Configuration.Current.ValheimPlus.IsEnabled && Configuration.Current.ValheimPlus.mainMenuLogo)
                // No need to exclude with IF, this only loads the images,
                // causes issues if this config setting is changed
                VPlusMainMenu.Load();

                VPlusSettings.Load();

                //Map Sync Save Timer
                if (ZNet.m_isServer && Configuration.Current.Map.IsEnabled &&
                    Configuration.Current.Map.shareMapProgression)
                {
                    MapSyncSaveTimer.AutoReset = true;
                    MapSyncSaveTimer.Elapsed += (_, _) => VPlusMapSync.SaveMapDataToDisk();
                }

                Logger.LogInfo($"ValheimPlus done loading.");
            }
        }

        private static bool IsGameVersionTooOld() => Version.CurrentVersion < MinSupportedGameVersion;
        private static bool IsGameVersionNewerThanTarget() => Version.CurrentVersion > TargetGameVersion;

        private static bool CheckIsGameVersionExcluded()
        {
            bool excluded = ExcludeGameVersions.TryGetValue(Version.CurrentVersion, out var errorMessage);
            if (excluded) Logger.LogError(errorMessage);
            return excluded;
        }

        private static bool IsNewVersionAvailable()
        {
            var client = new WebClient();

            client.Headers.Add("User-Agent: V+ Server");

            try
            {
                var reply = client.DownloadString(ApiRepository);
                // newest version is the "latest" release in github
                newestVersion = new Regex("\"tag_name\":\"([^\"]*)?\"").Match(reply).Groups[1].Value;
            }
            catch
            {
                Logger.LogWarning("The newest version could not be determined.");
                newestVersion = "Unknown";
            }

            //Parse versions for proper version check
            if (System.Version.TryParse(newestVersion, out var newVersion))
            {
                if (System.Version.TryParse(NumericVersion, out var currentVersion))
                {
                    if (currentVersion < newVersion)
                    {
                        return true;
                    }
                }
                else
                {
                    Logger.LogWarning("Couldn't parse current version");
                }
            }
            else //Fallback version check if the version parsing fails
            {
                Logger.LogWarning("Couldn't parse newest version, comparing version strings with equality.");
                if (newestVersion != NumericVersion)
                {
                    return true;
                }
            }

            return false;
        }

        public static void PatchAll()
        {
            Logger.LogInfo("Applying patches.");
            try
            {
                // handles annotations
                Harmony.PatchAll();

                // manual patches that only should run in certain conditions, that otherwise would just cause errors.

                // steam only patches
                if (AppDomain.CurrentDomain.GetAssemblies()
                    .Any(assembly => assembly.FullName.Contains("assembly_steamworks")))
                {
                    Harmony.Patch(
                        original: AccessTools.TypeByName("SteamGameServer").GetMethod("SetMaxPlayerCount"),
                        prefix: new HarmonyMethod(typeof(ChangeSteamServerVariables).GetMethod("Prefix")));
                }

                // enable mod enforcement with VersionCheck from ServerSync
                ((VersionCheck)VersionCheck).ModRequired = Configuration.Current.Server.enforceMod;
                Logger.LogInfo("Patches successfully applied.");
            }
            catch (Exception)
            {
                Logger.LogError("Failed to apply patches.");
                if (IsGameVersionTooOld()) LogTooOld();
                else if (IsGameVersionNewerThanTarget())
                {
                    Logger.LogWarning(
                        $"This version of Valheim Plus ({FullVersion}) was compiled with a game version of " +
                        $"\"{TargetGameVersion}\", but this game version is newer at \"{Version.CurrentVersion}\". " +
                        "If you are using the PTB, you likely need to use the non-beta version of the game. " +
                        "Otherwise, the errors seen above likely will require the Valheim Plus mod to be updated. " +
                        "If a game update just came out for Valheim, this may take some time for the mod to be updated. " +
                        "See https://github.com/Grantapher/ValheimPlus/blob/grantapher-development/COMPATIBILITY.md " +
                        "for what game versions are compatible with what mod versions.");
                }
                else
                {
                    Logger.LogWarning(
                        $"Valheim Plus failed to apply patches. " +
                        $"Please ensure the game version ({Version.GetVersionString()}) is compatible with " +
                        $"the Valheim Plus version ({FullVersion}) at " +
                        "https://github.com/Grantapher/ValheimPlus/blob/grantapher-development/COMPATIBILITY.md. " +
                        "If it already is, please report a bug at https://github.com/Grantapher/ValheimPlus/issues.");
                }

                // rethrow, otherwise it may not be obvious to the user that patching failed
                throw;
            }
        }

        private static void LogTooOld()
        {
            Logger.LogError(
                $"This version of Valheim Plus ({FullVersion}) expects a minimum game version of " +
                $"\"{MinSupportedGameVersion}\", but this game version is older at \"{Version.CurrentVersion}\". " +
                "Please either update the Valheim game, or use an older version of Valheim Plus as per " +
                "https://github.com/Grantapher/ValheimPlus/blob/grantapher-development/COMPATIBILITY.md.");
        }

        public static void UnpatchSelf()
        {
            Logger.LogInfo("Unpatching.");
            try
            {
                Harmony.UnpatchSelf();
                Logger.LogInfo("Successfully unpatched.");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to unpatch. Exception: {e}");
            }
        }
    }
}