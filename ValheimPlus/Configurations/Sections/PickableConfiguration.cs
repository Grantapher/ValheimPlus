using BepInEx.Configuration;

namespace ValheimPlus.Configurations.Sections
{
    public class PickableConfiguration : BaseConfig
    {
        private const string Section = "Pickable";

        private ConfigEntry<float> ediblesEntry;
        private ConfigEntry<float> flowersAndIngredientsEntry;
        private ConfigEntry<float> seedsEntry;
        private ConfigEntry<float> materialsEntry;
        private ConfigEntry<float> valuablesEntry;
        private ConfigEntry<float> surtlingCoresEntry;
        private ConfigEntry<float> blackCoresEntry;
        private ConfigEntry<float> moltenCoresEntry;
        private ConfigEntry<float> frostCoresEntry;
        private ConfigEntry<float> questItemsEntry;

        public float edibles => ediblesEntry.Value;
        public float flowersAndIngredients => flowersAndIngredientsEntry.Value;
        public float seeds => seedsEntry.Value;
        public float materials => materialsEntry.Value;
        public float valuables => valuablesEntry.Value;
        public float surtlingCores => surtlingCoresEntry.Value;
        public float blackCores => blackCoresEntry.Value;
        public float moltenCores => moltenCoresEntry.Value;
        public float frostCores => frostCoresEntry.Value;
        public float questItems => questItemsEntry.Value;

        public override void Bind(ConfigFile config)
        {
            BindEnabled(config, Section, false,
                "Change false to true to enable this section.\nEach value below (in percent) will modify the yield when \"picking\" items (default key E) such as berries and flowers.\nA value of 100 will double drops, 200 will triple and so on.");
            ediblesEntry = Bind(config, Section, "edibles", 0f,
                "All berries, all mushrooms, onions, carrots, kale and poteitr");
            flowersAndIngredientsEntry = Bind(config, Section, "flowersAndIngredients", 0f,
                "Barley, Flax, Dandelion, Thistle, Turnip, Royal Jelly, Volture Eggs and Entrails");
            seedsEntry = Bind(config, Section, "seeds", 0f,
                "Carrot, Turnip, Onion, Kale and Oat Seeds");
            materialsEntry = Bind(config, Section, "materials", 0f,
                "Bone Fragments, Flint, Stone, branches on the ground, Crystal, Tar, Wolf Hair and Claws, Obsidian, Grausten, ore and other minerals and Charred Skulls");
            valuablesEntry = Bind(config, Section, "valuables", 0f,
                "Amber, Amber Pearl, Coins, Ruby and gemstones");
            surtlingCoresEntry = Bind(config, Section, "surtlingCores", 0f,
                "Surtling Core only");
            blackCoresEntry = Bind(config, Section, "blackCores", 0f,
                "Black Core only");
            moltenCoresEntry = Bind(config, Section, "moltenCores", 0f,
                "Molten Core only");
            frostCoresEntry = Bind(config, Section, "frostCores", 0f,
                "Frostcore only");
            questItemsEntry = Bind(config, Section, "questItems", 0f,
                "Items needed to sacrifice to bosses (excluding the first two bosses)");
        }
    }
}
