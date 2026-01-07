using System;
using BepInEx.Logging;

namespace ValheimPlus.Http
{
    internal static class SelfTests
    {
        public static void RunIfEnabled(ManualLogSource logger)
        {
            var enabled = Environment.GetEnvironmentVariable("VPLUS_SELFTEST");
            if (enabled != "1") return;

            try
            {
                // Expect failure on invalid scheme but ensure we catch and report
                try
                {
                    HttpHelper.DownloadString("htps://not-a-valid-scheme", "ValheimPlus/SelfTest", TimeSpan.FromSeconds(2));
                    logger.LogWarning("SelfTest: Unexpected success on invalid scheme test.");
                }
                catch (Exception)
                {
                    logger.LogInfo("SelfTest: Invalid scheme handling OK.");
                }

                // Best-effort quick fetch; allowed to fail (networkless environments)
                try
                {
                    var _ = HttpHelper.DownloadString("https://example.com", "ValheimPlus/SelfTest", TimeSpan.FromSeconds(3));
                    logger.LogInfo("SelfTest: Basic HTTPS fetch attempt executed.");
                }
                catch (Exception ex)
                {
                    logger.LogWarning($"SelfTest: HTTPS fetch attempt failed (non-fatal): {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning($"SelfTest: Unexpected error: {ex.Message}");
            }
        }
    }
}
