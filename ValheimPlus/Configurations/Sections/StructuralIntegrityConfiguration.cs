using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class StructuralIntegrityConfiguration : BaseConfig
    {
        private const string Section = "StructuralIntegrity";

        private ConfigEntry<float> woodEntry;
        private ConfigEntry<float> stoneEntry;
        private ConfigEntry<float> ironEntry;
        private ConfigEntry<float> hardWoodEntry;
        private ConfigEntry<float> marbleEntry;
        private ConfigEntry<float> ashstoneEntry;
        private ConfigEntry<float> ancientEntry;
        private ConfigEntry<float> iceEntry;
        private ConfigEntry<float> timberwoodEntry;
        private ConfigEntry<bool> disableStructuralIntegrityEntry;
        private ConfigEntry<bool> disableDamageToPlayerStructuresEntry;
        private ConfigEntry<bool> disableDamageToPlayerBoatsEntry;
        private ConfigEntry<bool> disableDamageToPlayerCartsEntry;
        private ConfigEntry<bool> disableWaterDamageToPlayerBoatsEntry;
        private ConfigEntry<bool> disableWaterDamageToPlayerCartsEntry;

        public float wood => woodEntry.Value;
        public float stone => stoneEntry.Value;
        public float iron => ironEntry.Value;
        public float hardWood => hardWoodEntry.Value;
        public float marble => marbleEntry.Value;
        public float ashstone => ashstoneEntry.Value;
        public float ancient => ancientEntry.Value;
        public float ice => iceEntry.Value;
        public float timberwood => timberwoodEntry.Value;
        public bool disableStructuralIntegrity => disableStructuralIntegrityEntry.Value;
        public bool disableDamageToPlayerStructures => disableDamageToPlayerStructuresEntry.Value;
        public bool disableDamageToPlayerBoats => disableDamageToPlayerBoatsEntry.Value;
        public bool disableDamageToPlayerCarts => disableDamageToPlayerCartsEntry.Value;
        public bool disableWaterDamageToPlayerBoats => disableWaterDamageToPlayerBoatsEntry.Value;
        public bool disableWaterDamageToPlayerCarts => disableWaterDamageToPlayerCartsEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            woodEntry = Bind(config, Section, "wood", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            stoneEntry = Bind(config, Section, "stone", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            ironEntry = Bind(config, Section, "iron", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            hardWoodEntry = Bind(config, Section, "hardWood", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            marbleEntry = Bind(config, Section, "marble", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            ashstoneEntry = Bind(config, Section, "ashstone", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            ancientEntry = Bind(config, Section, "ancient", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            iceEntry = Bind(config, Section, "ice", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            timberwoodEntry = Bind(config, Section, "timberwood", 0f, 0f, 100f,
                "Each of these values reduce the loss of structural integrity by distance by % less.\nThe value 100 would result in disabled structural integrity over distance, does not allow for placement in free air without disableStructuralIntegrity.");
            disableStructuralIntegrityEntry = Bind(config, Section, "disableStructuralIntegrity", false,
                "Disables the entire structural integrity system and allows for placement in free air, does not prevent building damage.");
            disableDamageToPlayerStructuresEntry = Bind(config, Section, "disableDamageToPlayerStructures", false,
                "Disables any damage from anything to all player built structures. Does not prevent damage from structural integrity.");
            disableDamageToPlayerBoatsEntry = Bind(config, Section, "disableDamageToPlayerBoats", false,
                "Disables any damage from anything to all player built boats.");
            disableDamageToPlayerCartsEntry = Bind(config, Section, "disableDamageToPlayerCarts", false,
                "Disables any damage from anything to all player built carts.");
            disableWaterDamageToPlayerBoatsEntry = Bind(config, Section, "disableWaterDamageToPlayerBoats", false,
                "Disables water force damage to all player built boats.");
            disableWaterDamageToPlayerCartsEntry = Bind(config, Section, "disableWaterDamageToPlayerCarts", false,
                "Disables water force damage to all player built carts.");
        }
    }
}
