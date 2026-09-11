using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace ValheimPlus.Configurations
{
    /// <summary>
    /// Base for a config section backed by BepInEx <see cref="ConfigEntry{T}"/> objects.
    /// </summary>
    public abstract class BaseConfig
    {
        /// <summary>Configuration Manager tag shared by this section's settings.</summary>
        private readonly ConfigurationManagerAttributes attributes = new();

        /// <summary>Tag for "enabled", ordered to the top of its section.</summary>
        private readonly ConfigurationManagerAttributes enabledAttributes = new() { Order = int.MaxValue };

        /// <summary>One call per entry, run when this section is handed to ServerSync.</summary>
        private readonly List<Action> syncRegistrations = new();

        private string sectionName;
        private ConfigEntry<bool> enabledEntry;

        /// <summary>Whether the user turned this section on. False until <see cref="Bind"/> has run.</summary>
        public bool IsEnabled => enabledEntry?.Value ?? false;

        /// <summary>Declares this section's config entries, <see cref="BindEnabled"/> first.</summary>
        public abstract void Bind(ConfigFile config);

        /// <summary>True for a section a server neither pushes to clients nor takes from one.</summary>
        protected virtual bool ClientSide => false;

        /// <summary>Binds the section's "enabled" key, which backs <see cref="IsEnabled"/>.</summary>
        protected void BindEnabled(ConfigFile config, string section, bool defaultValue, string description)
        {
            sectionName = section;
            enabledEntry = config.Bind(section, "enabled", defaultValue,
                new ConfigDescription(description, null, enabledAttributes));
            if (!ClientSide) syncRegistrations.Add(() => ConfigSyncGlue.Register(enabledEntry, true));
        }

        /// <summary>Binds a setting a server pushes to its clients, unless it is a keybind.</summary>
        protected ConfigEntry<T> Bind<T>(
            ConfigFile config, string section, string key, T defaultValue, string description)
        {
            return Bind(config, section, key, defaultValue, description, local: false, range: null);
        }

        /// <summary>Binds a pushed setting limited to min..max, shown as a slider in Configuration Manager.</summary>
        protected ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue,
            T min, T max, string description) where T : IComparable
        {
            return Bind(config, section, key, defaultValue, description, local: false,
                range: new AcceptableValueRange<T>(min, max));
        }

        /// <summary>Binds a setting that stays on the machine it is set on.</summary>
        protected ConfigEntry<T> BindLocal<T>(
            ConfigFile config, string section, string key, T defaultValue, string description)
        {
            return Bind(config, section, key, defaultValue, description, local: true, range: null);
        }

        private ConfigEntry<T> Bind<T>(ConfigFile config, string section, string key, T defaultValue,
            string description, bool local, AcceptableValueBase range)
        {
            sectionName = section;
            var entry = config.Bind(section, key, defaultValue,
                new ConfigDescription(description, range, attributes));

            // Keybinds, and anything else personal, never cross the network.
            if (!local && !ClientSide && typeof(T) != typeof(KeyCode))
            {
                syncRegistrations.Add(() => ConfigSyncGlue.Register(entry, true));
            }

            return entry;
        }

        /// <summary>Hands every entry in this section to ServerSync. Returns how many were registered.</summary>
        internal int RegisterForServerSync()
        {
            foreach (var register in syncRegistrations) register();
            return syncRegistrations.Count;
        }

        /// <summary>
        /// Locks or unlocks this section in Configuration Manager, <paramref name="lockNote"/> saying
        /// why in the category header. A null category falls back to the plain section name.
        /// </summary>
        internal void SetEditable(bool editable, string lockNote)
        {
            attributes.ReadOnly = !editable;
            enabledAttributes.ReadOnly = !editable;
            attributes.Category = editable ? null : $"{sectionName} ({lockNote})";
            enabledAttributes.Category = attributes.Category;
        }
    }

    /// <summary>A section that stays local: a server neither pushes nor receives it.</summary>
    public abstract class ClientConfig : BaseConfig
    {
        protected override bool ClientSide => true;
    }
}
