using System;
using System.IO;
using System.Reflection;

namespace ValheimPlus.Tests
{
    internal static class AssemblyResolver
    {
        static AssemblyResolver()
        {
            var install = Environment.GetEnvironmentVariable("VALHEIM_INSTALL");
            if (string.IsNullOrWhiteSpace(install)) return;

            var dataDir = Path.Combine(install, "valheim_server_Data");
            if (!Directory.Exists(Path.Combine(dataDir, "Managed")))
            {
                dataDir = Path.Combine(install, "valheim_Data");
            }

            var managedDir = Path.Combine(dataDir, "Managed");
            var pubDir = Path.Combine(managedDir, "publicized_assemblies");

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                var name = new AssemblyName(args.Name).Name + ".dll";
                var candidatePub = Path.Combine(pubDir, name);
                if (File.Exists(candidatePub)) return Assembly.LoadFrom(candidatePub);

                var candidateManaged = Path.Combine(managedDir, name);
                if (File.Exists(candidateManaged)) return Assembly.LoadFrom(candidateManaged);

                return null;
            };
        }
    }
}
