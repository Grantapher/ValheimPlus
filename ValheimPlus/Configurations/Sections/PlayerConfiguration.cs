using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class PlayerConfiguration : BaseConfig
    {
        private const string Section = "Player";

        private ConfigEntry<float> baseMaximumWeightEntry;
        private ConfigEntry<float> baseMegingjordBuffEntry;
        private ConfigEntry<float> baseAutoPickUpRangeEntry;
        private ConfigEntry<bool> disableCameraShakeEntry;
        private ConfigEntry<float> unarmedDamageScaleEntry;
        private ConfigEntry<bool> cropNotifierEntry;
        private ConfigEntry<float> restSecondsPerComfortLevelEntry;
        private ConfigEntry<float> deathPenaltyMultiplierEntry;
        private ConfigEntry<bool> autoRepairEntry;
        private ConfigEntry<float> guardianBuffDurationEntry;
        private ConfigEntry<float> guardianBuffCooldownEntry;
        private ConfigEntry<bool> disableGuardianBuffAnimationEntry;
        private ConfigEntry<bool> autoEquipShieldEntry;
        private ConfigEntry<bool> autoUnequipShieldEntry;
        private ConfigEntry<bool> skipIntroEntry;
        private ConfigEntry<bool> iHaveArrivedOnSpawnEntry;
        private ConfigEntry<bool> queueWeaponChangesEntry;
        private ConfigEntry<bool> dontUnequipItemsWhenSwimmingEntry;
        private ConfigEntry<bool> reequipItemsAfterSwimmingEntry;
        private ConfigEntry<float> fallDamageScalePercentEntry;
        private ConfigEntry<float> maxFallDamageEntry;
        private ConfigEntry<bool> skipTutorialsEntry;
        private ConfigEntry<bool> disableEncumberedEntry;
        private ConfigEntry<bool> autoPickUpWhenEncumberedEntry;
        private ConfigEntry<bool> disableEightSecondTeleportEntry;

        public float baseMaximumWeight => baseMaximumWeightEntry.Value;
        public float baseMegingjordBuff => baseMegingjordBuffEntry.Value;
        public float baseAutoPickUpRange => baseAutoPickUpRangeEntry.Value;
        public bool disableCameraShake => disableCameraShakeEntry.Value;
        public float unarmedDamageScale => unarmedDamageScaleEntry.Value;
        public bool cropNotifier => cropNotifierEntry.Value;
        public float restSecondsPerComfortLevel => restSecondsPerComfortLevelEntry.Value;
        public float deathPenaltyMultiplier => deathPenaltyMultiplierEntry.Value;
        public bool autoRepair => autoRepairEntry.Value;
        public float guardianBuffDuration => guardianBuffDurationEntry.Value;
        public float guardianBuffCooldown => guardianBuffCooldownEntry.Value;
        public bool disableGuardianBuffAnimation => disableGuardianBuffAnimationEntry.Value;
        public bool autoEquipShield => autoEquipShieldEntry.Value;
        public bool autoUnequipShield => autoUnequipShieldEntry.Value;
        public bool skipIntro => skipIntroEntry.Value;
        public bool iHaveArrivedOnSpawn => iHaveArrivedOnSpawnEntry.Value;
        public bool queueWeaponChanges => queueWeaponChangesEntry.Value;
        public bool dontUnequipItemsWhenSwimming => dontUnequipItemsWhenSwimmingEntry.Value;
        public bool reequipItemsAfterSwimming => reequipItemsAfterSwimmingEntry.Value;
        public float fallDamageScalePercent => fallDamageScalePercentEntry.Value;
        public float maxFallDamage => maxFallDamageEntry.Value;
        public bool skipTutorials => skipTutorialsEntry.Value;
        public bool disableEncumbered => disableEncumberedEntry.Value;
        public bool autoPickUpWhenEncumbered => autoPickUpWhenEncumberedEntry.Value;
        public bool disableEightSecondTeleport => disableEightSecondTeleportEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            baseMaximumWeightEntry = Bind(config, Section, "baseMaximumWeight", 300f,
                "The base amount of carry weight of your character.");
            baseMegingjordBuffEntry = Bind(config, Section, "baseMegingjordBuff", 150f,
                "Increase the buff you receive to your carry weight from Megingjord's girdle.");
            baseAutoPickUpRangeEntry = Bind(config, Section, "baseAutoPickUpRange", 2f,
                "Increase auto pickup range of all items.");
            disableCameraShakeEntry = Bind(config, Section, "disableCameraShake", false,
                "Disable all types of camera shake.");
            unarmedDamageScaleEntry = Bind(config, Section, "unarmedDamageScale", 0f,
                "This value represents how much unarmed (fist) damage should be scaled in +/- %. This is a modifier value.\nThe value 50 would result in 50% increased unarmed damage. The value -50 would result in 50% reduced unarmed damage.");
            cropNotifierEntry = Bind(config, Section, "cropNotifier", false,
                "When changed to true, you will not be permitted to place a crop within the grow radius of another crop.");
            restSecondsPerComfortLevelEntry = Bind(config, Section, "restSecondsPerComfortLevel", 60f,
                "How many seconds each comfort level contributes to the rested bonus.");
            deathPenaltyMultiplierEntry = Bind(config, Section, "deathPenaltyMultiplier", 0f,
                "Change the death penalty in percentage, where higher will increase the death penalty and lower will reduce it.\nThis is a modifier value. 50 will increase it by 50%, -50 will reduce it by 50%.");
            autoRepairEntry = Bind(config, Section, "autoRepair", false,
                "If set to true, this option will automatically repair your equipment when you interact with the appropriate workbench.");
            guardianBuffDurationEntry = Bind(config, Section, "guardianBuffDuration", 300f,
                "Boss buff duration (seconds)");
            guardianBuffCooldownEntry = Bind(config, Section, "guardianBuffCooldown", 1200f,
                "Boss buff cooldown (seconds)");
            disableGuardianBuffAnimationEntry = Bind(config, Section, "disableGuardianBuffAnimation", false,
                "Disable the Guardian Buff animation");
            autoEquipShieldEntry = Bind(config, Section, "autoEquipShield", false,
                "If set to true, when equipping a one-handed weapon, the best shield from your inventory is automatically equipped.\n(Best is determined by highest block power)");
            autoUnequipShieldEntry = Bind(config, Section, "autoUnequipShield", false,
                "When unequipping a one-handed weapon also unequip shield from inventory.");
            skipIntroEntry = Bind(config, Section, "skipIntro", false,
                "If set to true, you will always skip the intro of the game.");
            iHaveArrivedOnSpawnEntry = Bind(config, Section, "iHaveArrivedOnSpawn", true,
                "If set to false, disables the \"I have arrived!\" message on player spawn.");
            queueWeaponChangesEntry = Bind(config, Section, "queueWeaponChanges", false,
                "If set to true, weapon switches requested mid-attack will be carried out when the current attack is finished instead of being ignored.");
            dontUnequipItemsWhenSwimmingEntry = Bind(config, Section, "dontUnequipItemsWhenSwimming", false,
                "If set to true, you will not put away / unequip your items when swimming.");
            reequipItemsAfterSwimmingEntry = Bind(config, Section, "reequipItemsAfterSwimming", false,
                "If set to true, items will be re-equipped when you exit water after swimming (if they were hidden automatically)");
            fallDamageScalePercentEntry = Bind(config, Section, "fallDamageScalePercent", 0f,
                "This value represents how much the fall damage should be scaled in +/- %. This is a modifier value.\nThe value 50 would result in 50% increased fall damage. The value -50 would result in 50% reduced fall damage.");
            maxFallDamageEntry = Bind(config, Section, "maxFallDamage", 100f,
                "Max fall damage. Game default is 100 (so with enough health, falls can't kill).");
            skipTutorialsEntry = Bind(config, Section, "skipTutorials", false,
                "If set to true, all tutorials will skip from now on. You can turn this config off and reset the tutorial (in the settings) at any time.");
            disableEncumberedEntry = Bind(config, Section, "disableEncumbered", false,
                "Disable the encumbered state when you carry too many items (overweight)");
            autoPickUpWhenEncumberedEntry = Bind(config, Section, "autoPickUpWhenEncumbered", false,
                "Allow auto pickup of items when encumbered (overweight)");
            disableEightSecondTeleportEntry = Bind(config, Section, "disableEightSecondTeleport", false,
                "Shortens the teleport time as much as much as possible");
        }
    }
}
