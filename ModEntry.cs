﻿using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SM_bqms
{
    public class ModEntry : Mod 
    {
        /*
        * Constants
        */
        private readonly List<SeedIds> SeedsToSkip = new List<SeedIds>
        {
            SeedIds.MixedSeed,
            SeedIds.AncientSeed
        };
        private readonly List<CategoryIds> CategoriesToMatch = new List<CategoryIds>
        {
            CategoryIds.Crop,
            CategoryIds.Fruit
        };
        /*
        * State
        */
        private bool IsModInitialized;
        private Farmer Player;
        private List<SeedMaker> SeedMakers;
        private StardewValley.Object LastHeldCropOrFruit;
        /*
        * Mod Entry
        */
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.ReturnedToTitle += this.ResetMod;
            helper.Events.GameLoop.SaveLoaded += this.InitMod;

            helper.Events.World.ObjectListChanged += this.UpdateSeedMakers;
            helper.Events.GameLoop.UpdateTicking += this.WatchSeedMakers;

            if (helper.ModRegistry.IsLoaded("Pathoschild.Automate")) {
                this.Monitor.Log("This mod patches Automate. If you notice issues with Automate, make sure it happens without this mod before reporting it to the Automate page.", LogLevel.Debug);
                AutomateSeedMakerMachinePatcher.Initialize(helper, this.Monitor, this.ModManifest.UniqueID);
            }
        }
        /*
        * Mod state handling
        */
        private void ResetMod(object sender, ReturnedToTitleEventArgs e)
        {
            this.SeedMakers = null;
            this.LastHeldCropOrFruit = null;
            this.Player = null;
            this.IsModInitialized = false;
        }
        private void InitMod(object sender, SaveLoadedEventArgs e)
        {
            List<SeedMaker> seedMakers = new List<SeedMaker>();
            foreach(GameLocation loc in Game1.locations)
            {
                foreach (SerializableDictionary<Vector2, StardewValley.Object> gameObjects in loc.Objects) {
                    foreach (var o in gameObjects)
                    {
                        if (o.Value.Name == "Seed Maker")
                        {
                            seedMakers.Add(new SeedMaker {
                                GameObject = o.Value,
                                isHandled = false,
                            });
                        }
                    }
                }
            }
            this.SeedMakers = seedMakers;
            this.Player = Game1.player;
            this.IsModInitialized = true;
        }
        /*
        * Seed Maker handlers
        */
        private void UpdateSeedMakers(object sender, ObjectListChangedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            foreach (KeyValuePair<Vector2, StardewValley.Object> gameObject in e.Added)
            {
                if (gameObject.Value.Name == "Seed Maker") {
                    SeedMaker seedMaker = new SeedMaker()
                    {
                        GameObject = gameObject.Value,
                        isHandled = false,
                    };
                    this.SeedMakers.Add(seedMaker);
                }
            }
            foreach (KeyValuePair<Vector2, StardewValley.Object> gameObject in e.Removed)
            {
                if (gameObject.Value.Name == "Seed Maker") {
                    SeedMaker seedMaker = new SeedMaker()
                    {
                        GameObject = gameObject.Value,
                    };
                    this.SeedMakers.Remove(seedMaker);
                }
            }
        }
        private void WatchSeedMakers(object sender, UpdateTickingEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            if (!this.IsModInitialized) return;
            foreach (SeedMaker seedMaker in this.SeedMakers)
            {
                if (!seedMaker.isHandled
                    && seedMaker.GameObject.heldObject.Value != null
                    && this.LastHeldCropOrFruit != null)
                {
                    int gameObjectId = seedMaker.GameObject.heldObject.Value.ParentSheetIndex;
                    if ((FruitIds)this.LastHeldCropOrFruit.ParentSheetIndex != FruitIds.AncientFruit) {
                        if (this.SeedsToSkip.Contains((SeedIds)gameObjectId)) return;
                    }
                    seedMaker.GameObject.heldObject.Value = new StardewValley.Object(gameObjectId, this.generateSeedAmountBasedOnQuality(seedMaker.GameObject.TileLocation, this.LastHeldCropOrFruit.Quality));
                    seedMaker.isHandled = true;
                    this.LastHeldCropOrFruit = null;
                }
                if (seedMaker.isHandled
                    && seedMaker.GameObject.heldObject.Value == null)
                {
                    seedMaker.isHandled = false;
                }
            }
            StardewValley.Object activeObject = this.Player.ActiveObject;
            if (activeObject == null)
            {
                this.LastHeldCropOrFruit = null;
                return;
            }
            if (this.CategoriesToMatch.Contains((CategoryIds)activeObject.Category))
            {
                this.LastHeldCropOrFruit = activeObject;
            }
        }
        private int generateSeedAmountBasedOnQuality(Vector2 location, int quality)
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
