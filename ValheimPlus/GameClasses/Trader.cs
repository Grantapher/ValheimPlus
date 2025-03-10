using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using ValheimPlus.Configurations;

namespace ValheimPlus.GameClasses
{
    public static class TraderExtensions
    {
        public static bool SellsItem(this Trader trader, string itemName)
            => trader.m_items.Any(item => item.m_prefab.name == itemName);

        public static Trader.TradeItem GetTradeItem(this Trader trader, string itemName)
            => trader.m_items.FirstOrDefault(item => item.m_prefab.m_itemData.m_shared.m_name == itemName);
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.Start))]
    public static class Trader_Start_Patch
    {
        [UsedImplicitly]
        public static void Postfix(Trader __instance)
        {
            switch (__instance.m_name)
            {
                case "$npc_haldor":
                    AddHaldorItems(__instance);
                    break;
                case "$npc_hildir":
                    AddHildirItems(__instance);
                    break;
                case "$npc_bogwitch":
                    AddBogWitchItems(__instance);
                    break;
            }
        }

        private static void AddHaldorItems(Trader haldor)
        {
            if (!Configuration.Current.Egg.IsEnabled) return;
            var egg = haldor.GetTradeItem("$item_chicken_egg");
            egg.m_requiredGlobalKey = Configuration.Current.Egg.soldByDefault ? "" : "defeated_goblinking";
            egg.m_price = Configuration.Current.Egg.sellPrice;
        }

        private static void AddHildirItems(Trader hildir)
        {
        }

        private static void AddBogWitchItems(Trader bogWitch)
        {
        }
    }
}
