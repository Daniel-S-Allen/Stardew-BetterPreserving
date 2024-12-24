using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Tools;

namespace BetterPreserving
{
    internal sealed class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.Patch(
               original: AccessTools.Method(typeof(FishingRod), "doDoneFishing"),
               prefix: new HarmonyMethod(typeof(ModEntry), nameof(DoDoneFishing_Prefix)),
               postfix: new HarmonyMethod(typeof(ModEntry), nameof(DoDoneFishing_Postfix))
            );
        }

        /// <summary>
        /// Harmony Patch that forces original method to not consume bait and tackle so it can be handled in DoDoneFishing_Postfix
        /// </summary>
        /// <param name="consumeBaitAndTackle">ref to parameter of original method</param>
        /// <param name="__state">Stores the original value of consumeBaitAndTackle</param>
        private static void DoDoneFishing_Prefix(ref bool consumeBaitAndTackle, out bool __state)
        {
            __state = consumeBaitAndTackle;
            consumeBaitAndTackle = false;
        }

        /// <summary>
        /// Reimplements the original durability method with the drain of all tackles controlled by one random check, rather than a random check for all of them
        /// </summary>
        /// <param name="__state">Whether to consume bait or not</param>
        /// <param name="__instance">The fishing rod that triggered this event</param>
        private static void DoDoneFishing_Postfix(bool __state, FishingRod __instance)
        {
            if (__state && __instance.getLastFarmerToUse() != null && __instance.getLastFarmerToUse().IsLocalPlayer)
            {
                float num = 1f;
                if (__instance.hasEnchantmentOfType<PreservingEnchantment>())
                {
                    num = 0.5f;
                }

                StardewValley.Object bait = __instance.GetBait();
                if (bait != null && Game1.random.NextDouble() < (double)num && bait.ConsumeStack(1) == null)
                {
                    __instance.attachments[0] = null;
                    Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14085"));
                }

                if (!__instance.lastCatchWasJunk && Game1.random.NextDouble() < (double)num)
                {
                    int tackleIndex = 1;
                    foreach (StardewValley.Object item in __instance.GetTackle())
                    {
                        if (item != null)
                        {
                            if (item.QualifiedItemId == "(O)789")
                            {
                                break;
                            }

                            item.uses.Value += 1;
                            if (item.uses.Value >= FishingRod.maxTackleUses)
                            {
                                item.Stack -= 1;
                                item.uses.Value = 0;
                                if (item.Stack <= 0){
                                    __instance.attachments[tackleIndex] = null;
                                }
                                Game1.showGlobalMessage(Game1.content.LoadString("Strings\\StringsFromCSFiles:FishingRod.cs.14086"));
                            }
                        }

                        tackleIndex++;
                    }
                }
            }
        }

    }
}
