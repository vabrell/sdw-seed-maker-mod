using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using HarmonyLib;
using StardewValley;
using SM_bqms;

namespace SM_bqms
{
    public class AutomateSeedMakerMachinePatcher
    {
        private static IMonitor Monitor;
        private static IModHelper Helper;
        private static System.Object Instance;
        private static Type SeedMakerMachine;
        private static string ModID;
        private static bool Patched;
        public static void Initialize(IModHelper helper, IMonitor monitor, string modID)
        {
            Patched = false;
            Monitor = monitor;
            Helper = helper;
            ModID = modID;
            

            helper.Events.GameLoop.GameLaunched += RegisterPatch;
        }

        public static void RegisterPatch(object sender, EventArgs e)
        {
            var automate = Helper.ModRegistry.GetApi("PathosChild.Automate");
            if (automate != null && Patched == false) {
                Harmony harmony = new Harmony(ModID);
                Assembly assembly = automate.GetType().Assembly;
                SeedMakerMachine = assembly.GetType("Pathoschild.Stardew.Automate.Framework.Machines.Objects.SeedMakerMachine");

                var orginal = AccessTools.Method(SeedMakerMachine, "SetInput");
                var prefix = new HarmonyMethod(typeof(AutomateSeedMakerMachinePatcher), nameof(AutomateSeedMakerMachinePatcher.SetInput_prefix));
                harmony.Patch(
                    original: orginal,
                    prefix: prefix
                );
                Patched = true;
            }
        }

        public static bool SetInput_prefix(System.Object __instance, System.Object input, ref bool __result)
        {
            try
            {
                Instance = __instance;
                Type type = __instance.GetType();
                string MachineTypeID = (string)type.GetProperty("MachineTypeID").GetValue(__instance);
                // If not a seed maker use base method
                if (MachineTypeID != "SeedMaker") return true;
                BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

                MethodInfo UpdateSeedLookup = type.GetMethod("UpdateSeedLookup", bindingFlags);
                UpdateSeedLookup.Invoke(__instance, new object[] {});

                StardewValley.Object machine = (StardewValley.Object)type.GetProperty("Machine", bindingFlags).GetValue(__instance);

                // crop => seeds
                object crop;
                if (TryGetIngredient(input, IsValidCrop, 1, out crop))
                {
                    if (crop == null)
                    {
                        __result = false;
                        return false;
                    }
                    SeedMaker seedMaker = ModEntry.SeedMakers.First((sm) => sm.GameObject.TileLocation == machine.TileLocation);
                    if (seedMaker != null)
                    {
                        seedMaker.isHandled = true;
                    }
                    Type Consumable = type.Assembly.GetType("Pathoschild.Stardew.Automate.Framework.Consumable");
                    MethodInfo Take = Consumable.GetMethod("Take", bindingFlags);
                    StardewValley.Object item = (StardewValley.Object)Take.Invoke(crop, new object[] {});
                    IDictionary<int, int> SeedLookup = (IDictionary<int, int>)type.GetField("SeedLookup", bindingFlags).GetValue(Instance) ?? new Dictionary<int, int>();
                    if (SeedLookup == null)
                    {
                        __result = false;
                        return false;
                    }
                    int seedID = SeedLookup[item.ParentSheetIndex];

                    Random random = new Random((int)Game1.stats.DaysPlayed + (int)Game1.uniqueIDForThisGame / 2 + (int)machine.TileLocation.X + (int)machine.TileLocation.Y * 77 + Game1.timeOfDay);
                    machine.heldObject.Value = new StardewValley.Object(seedID, random.Next(1 + item.Quality, 4 + item.Quality));
                    if (random.NextDouble() < 0.005)
                        machine.heldObject.Value = new StardewValley.Object(499, 1);
                    else if (random.NextDouble() < 0.02)
                        machine.heldObject.Value = new StardewValley.Object(770, random.Next(1, 5));
                    machine.MinutesUntilReady = 20;
                    __result = true;
                    return false;
                }

                __result = false;
                return false;
            }
            catch (Exception e)
            {
                Monitor.Log($"Failed in [AutomateSeedMakerMachinePatcher.SetInput_prefix]:\n{e}", LogLevel.Error);
                return true;
            }
        }
        private static bool TryGetIngredient(object input, Func<object, bool> predicate, int count, out object consumable)
        {
            MethodInfo tryGetIngredient = input.GetType().GetMethods().ToList().Where((m) => m.ToString() == "Boolean TryGetIngredient(System.Func`2[Pathoschild.Stardew.Automate.ITrackedStack,System.Boolean], Int32, Pathoschild.Stardew.Automate.IConsumable ByRef)").First();

            object[] parameters = new object[] { predicate, count, null};
            object result = tryGetIngredient.Invoke(input, parameters);
            consumable = parameters[2];
            if (consumable == null)
            {
                return false;
            }
            return true;
        }
        private static bool IsValidCrop(object item)
        {
            Type type = Instance.GetType();
            BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            IDictionary<int, int> SeedLookup = (IDictionary<int, int>)type.GetField("SeedLookup", bindingFlags).GetValue(Instance);
            Enum ItemType = (Enum)item.GetType().GetProperty("Type", bindingFlags).GetValue(item);
            object sampleItem = item.GetType().GetProperty("Sample", bindingFlags).GetValue(item);
            if(sampleItem.GetType().ToString() != "StardewValley.Object")
            {
                return false;
            }
            StardewValley.Object Item = (StardewValley.Object)sampleItem;
            Type PItems = type.Assembly.GetType("Pathoschild.Stardew.Automate.ItemType");
            var result = ItemType.GetType() == PItems.GetField("Object").GetValue(null).GetType()
                && Item.ParentSheetIndex != 433 // coffee beans
                && Item.ParentSheetIndex != 771 // fiber
                && SeedLookup.ContainsKey(Item.ParentSheetIndex);
            return result;
        }
    }
}