using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
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

    [BepInPlugin("org.bepinex.plugins.valheim_plus", "Valheim Plus", numericVersion)]
    public class ValheimPlusPlugin : BaseUnityPlugin
    {
        // Version used when numeric is required (assembly info, bepinex, System.Version parsing).
        public const string numericVersion = "0.9.14.0";

        // Extra version, like alpha/beta/rc/stable. Can leave blank if a stable release.
        private const string VersionExtra = "-alpha02";

        // Version used when numeric is NOT required (Logging, config file lookup)
        public const string fullVersion = numericVersion + VersionExtra;

        // Minimum required version for full compatibility.
        private const string MinRequiredNumericVersion = numericVersion;

        // The lowest game version this version of V+ is known to work with.
        private static readonly GameVersion MinSupportedGameVersion = new(0, 218, 16);

        // The game version this version of V+ was compiled against.
        private static readonly GameVersion TargetGameVersion = new(0, 218, 16);

        // The last game version this will work with. If higher, the mod will not work.
        // This is useful for warning when the game is running on a PTB version we know this will fail on.
        // Otherwise, just keep this as null to disable the check.
        private static readonly GameVersion? MaxKnownWorkingGameVersion = null;

        internal static string newestVersion { get; private set; } = "";
        internal static bool isUpToDate { get; private set; }

        // ReSharper disable once InconsistentNaming
        public new static ManualLogSource Logger { get; private set; }

        public static readonly System.Timers.Timer MapSyncSaveTimer = new(TimeSpan.FromMinutes(5).TotalMilliseconds);

        public static readonly string VPlusDataDirectoryPath =
            Paths.BepInExRootPath + Path.DirectorySeparatorChar + "vplus-data";

        private static readonly Harmony Harmony = new("mod.valheim_plus");

        // Project Repository Info
        public const string repository = "https://github.com/Grantapher/ValheimPlus/releases/latest";
        private const string ApiRepository = "https://api.github.com/repos/grantapher/valheimPlus/releases/latest";

        // Website INI for auto update
        internal static readonly string IniFile =
            $"https://github.com/Grantapher/ValheimPlus/releases/download/{fullVersion}/valheim_plus.cfg";

        // mod fails to load when this type is correctly specified as VersionCheck,
        // so we'll just cast it as needed instead.
        private static readonly object VersionCheck = new VersionCheck("org.bepinex.plugins.valheim_plus")
        {
            DisplayName = "Valheim Plus",
            CurrentVersion = numericVersion,
            MinimumRequiredVersion = MinRequiredNumericVersion,
        };

        // Awake is called once when both the game and the plug-in are loaded
        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo($"Valheim Plus full version: {fullVersion}");

            if (IsGameVersionTooOld()) LogTooOld();
            else if (IsGameVersionTooNew()) LogTooNew();

            Logger.LogInfo($"Valheim Plus dll file location: '{GetType().Assembly.Location}'");
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
                    Logger.LogWarning($"There is a newer version available of ValheimPlus. Please visit {repository}.");
                }
                else
                {
                    Logger.LogInfo($"ValheimPlus [{fullVersion}] is up to date.");
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

        private static bool IsGameVersionTooNew() =>
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse Want to be able to set maxKnown* to null.
            MaxKnownWorkingGameVersion != null && Version.CurrentVersion > MaxKnownWorkingGameVersion;

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
                if (System.Version.TryParse(numericVersion, out var currentVersion))
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
                if (newestVersion != numericVersion)
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

                // HarmonyPriority wasn't loading in the order I wanted,
                // so manually load this one after the annotations are all loaded
                Harmony.Patch(
                    original: typeof(ZPlayFabMatchmaking).GetMethod("CreateLobby",
                        BindingFlags.NonPublic | BindingFlags.Instance),
                    transpiler: new HarmonyMethod(
                        typeof(ZPlayFabMatchmaking_CreateLobby_Transpiler).GetMethod("Transpiler")));

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
                Logger.LogError($"Failed to apply patches.");
                if (IsGameVersionTooOld()) LogTooOld();
                else if (IsGameVersionTooNew()) LogTooNew();
                else if (IsGameVersionNewerThanTarget())
                {
                    Logger.LogWarning(
                        $"This version of Valheim Plus ({fullVersion}) was compiled with a game version of " +
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
                        $"the Valheim Plus version ({fullVersion}) at " +
                        "https://github.com/Grantapher/ValheimPlus/blob/grantapher-development/COMPATIBILITY.md. " +
                        "If it already is, please report a bug at https://github.com/Grantapher/ValheimPlus/issues.");
                }

                // rethrow, otherwise it may not be obvious to the user that patching failed
                throw;
            }
        }

        private static void LogTooOld()
        {
            Logger.LogWarning(
                $"This version of Valheim Plus ({fullVersion}) expects a minimum game version of " +
                $"\"{MinSupportedGameVersion}\", but this game version is older at \"{Version.CurrentVersion}\". " +
                "Please either update the Valheim game, or use an older version of Valheim Plus as per " +
                "https://github.com/Grantapher/ValheimPlus/blob/grantapher-development/COMPATIBILITY.md.");
        }

        private static void LogTooNew()
        {
            Logger.LogWarning(
                $"This version of Valheim Plus ({fullVersion}) expects a maximum game version of " +
                $"\"{MaxKnownWorkingGameVersion}\", but this game version is newer at \"{Version.CurrentVersion}\". " +
                "Please update Valheim Plus via the releases: https://github.com/Grantapher/ValheimPlus/releases");
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