using System;
using System.Collections.Generic;

namespace ValheimPlus.Utility
{
    internal static class ChestDeposit
    {
        /// <summary>
        /// Tries existing stacks before other chests, preserving distance order in each pass.
        /// add returns the amount NOT accepted, including when an inventory accepts only part of a stack.
        /// Returns the remainder for the machine's normal world-drop fallback.
        /// </summary>
        internal static int Distribute<TChest>(IReadOnlyList<TChest> chests, int amount,
            Func<TChest, bool> hasItem, Func<TChest, int, int> add, Action<TChest> synchronize)
        {
            for (int pass = 0; pass < 2 && amount > 0; ++pass)
            {
                foreach (TChest chest in chests)
                {
                    if (hasItem(chest) != (pass == 0)) continue;

                    int remaining = add(chest, amount);
                    if (remaining < amount) synchronize(chest);
                    amount = remaining;
                    if (amount == 0) break;
                }
            }

            return amount;
        }
    }
}
