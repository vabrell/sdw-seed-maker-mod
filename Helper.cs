using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Linq;
using System.Threading.Tasks;
using StardewValley;

namespace SM_bqms
{
    public class SM_Helper
    {
        private static int LastCacheCount;
        private static int LastCacheTick;
        public static readonly IDictionary<int, int> SeedLookupCache = new Dictionary<int, int>();
        public static void UpdateSeedLookupCache()
        {
            if (Game1.ticks > LastCacheTick)
            {
                IDictionary<int, int> cache = SeedLookupCache;
                Dictionary<int, string> crops = Game1.content.Load<Dictionary<int, string>>("Data\\Crops");
                if (LastCacheCount != crops.Count)
                {
                    cache.Clear();

                    foreach (KeyValuePair<int, string> entry in crops)
                    {
                        int seedId = entry.Key;
                        int produceId = Convert.ToInt32(entry.Value.Split('/')[3]);
                        if (!cache.ContainsKey(produceId)) // use first crop found per game logic
                            cache[produceId] = seedId;
                    }
                }
                LastCacheCount = crops.Count;
                LastCacheTick = Game1.ticks;
            }
        }
        public static int generateSeedAmountBasedOnQuality(Vector2 location, int quality)
        {
            Random r2 = new Random(
                (int)Game1.stats.DaysPlayed
                + (int)Game1.uniqueIDForThisGame / 2
                + (int)location.X
                + (int)location.Y * 77
                + Game1.timeOfDay);
            return r2.Next(quality + 1, 4 + quality);
        }
    }
}