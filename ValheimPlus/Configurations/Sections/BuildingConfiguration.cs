using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class BuildingConfiguration : BaseConfig
    {
        private const string Section = "Building";

        private ConfigEntry<bool> noInvalidPlacementRestrictionEntry;
        private ConfigEntry<bool> noMysticalForcesPreventPlacementRestrictionEntry;
        private ConfigEntry<bool> noWeatherDamageEntry;
        private ConfigEntry<float> maximumPlacementDistanceEntry;
        private ConfigEntry<float> pieceComfortRadiusEntry;
        private ConfigEntry<bool> alwaysDropResourcesEntry;
        private ConfigEntry<bool> alwaysDropExcludedResourcesEntry;
        private ConfigEntry<bool> enableAreaRepairEntry;
        private ConfigEntry<float> areaRepairRadiusEntry;

        public bool noInvalidPlacementRestriction => noInvalidPlacementRestrictionEntry.Value;
        public bool noMysticalForcesPreventPlacementRestriction => noMysticalForcesPreventPlacementRestrictionEntry.Value;
        public bool noWeatherDamage => noWeatherDamageEntry.Value;
        public float maximumPlacementDistance => maximumPlacementDistanceEntry.Value;
        public float pieceComfortRadius => pieceComfortRadiusEntry.Value;
        public bool alwaysDropResources => alwaysDropResourcesEntry.Value;
        public bool alwaysDropExcludedResources => alwaysDropExcludedResourcesEntry.Value;
        public bool enableAreaRepair => enableAreaRepairEntry.Value;
        public float areaRepairRadius => areaRepairRadiusEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.");
            noInvalidPlacementRestrictionEntry = Bind(config, Section, "noInvalidPlacementRestriction", false,
                "Remove some of the Invalid placement messages, most notably provides the ability to place objects into other objects");
            noMysticalForcesPreventPlacementRestrictionEntry = Bind(config, Section, "noMysticalForcesPreventPlacementRestriction", false,
                "Removes the \"Mystical forces\" building prevention and allows destruction of build objects in those areas with the hammer.");
            noWeatherDamageEntry = Bind(config, Section, "noWeatherDamage", false,
                "Removes the weather damage from rain and water erosion.");
            maximumPlacementDistanceEntry = Bind(config, Section, "maximumPlacementDistance", 8f,
                "The maximum range in meters that you can place build objects at inside the hammer build mode.");
            pieceComfortRadiusEntry = Bind(config, Section, "pieceComfortRadius", 10f, 1f, 300f,
                "The radius, in meters, in which a piece must be to contribute to the comfort level.");
            alwaysDropResourcesEntry = Bind(config, Section, "alwaysDropResources", false,
                "When destroying a building piece, setting this to true will ensure it always drops full resources.\nWe recommend to enable this if you use this section.");
            alwaysDropExcludedResourcesEntry = Bind(config, Section, "alwaysDropExcludedResources", false,
                "When destroying a building piece, setting this to true will ensure it always drops pieces that the devs have marked as \"do not drop\".\nWe recommend to enable this if you use this section.");
            enableAreaRepairEntry = Bind(config, Section, "enableAreaRepair", false,
                "Setting this to true will cause repairing with the hammer to repair in a radius instead of a single piece.");
            areaRepairRadiusEntry = Bind(config, Section, "areaRepairRadius", 7.5f,
                "Sets the area repair radius of enableAreaRepair. A value of 7.5 would mean your repair radius is 7.5 meters.\nRequires enableAreaRepair=true");
        }
    }
}
