﻿using System;
using System.Xml;
using AATool.Data.Categories;
using AATool.Data.Progress;
using AATool.Net;

namespace AATool.Data.Objectives.Complex
{
    class GoldBlocks : ComplexObjective
    {
        public const string ItemId = "minecraft:gold_block";
        public const string LegacyItemId = "minecraft.gold_block";
        public const string GoldIngotId = "minecraft:gold_ingot";
        public const int IngotsPerBlock = 9;

        public const int Required = 164;

        private const string Beaconator = "minecraft:nether/create_full_beacon";
        private const string LegacyBeaconator = "achievement.fullBeacon";

        private static readonly Version BlockIdChanged = new ("1.13");
        private static readonly Version TextureChanged = new ("1.12");

        private static bool ModernId => !Version.TryParse(Tracker.CurrentVersion, out Version current)
            || current >= BlockIdChanged;
        private static bool ModernTexture => !Version.TryParse(Tracker.CurrentVersion, out Version current)
            || current >= TextureChanged;

        public GoldBlocks() : base()
        {
            this.Name = "GoldBlocks";
            this.RefreshIcon();
        }

        private bool fullBeaconComplete;
        private int estimatedBlocks;

        protected override void UpdateAdvancedState(ProgressState progress)
        {
            this.RefreshIcon();

            this.UpdatePreciseGoldEstimate(progress);

            this.fullBeaconComplete = Tracker.Category is AllAchievements
                ? progress.AdvancementCompleted(LegacyBeaconator)
                : progress.AdvancementCompleted(Beaconator);

            this.CompletionOverride = this.estimatedBlocks >= Required
                || this.fullBeaconComplete || this.ManuallyChecked;
        }

        private void UpdatePreciseGoldEstimate(ProgressState progress)
        {
            //account for ingots
            int ingots = progress.TimesPickedUp(GoldIngotId);
            ingots -= progress.TimesDropped(GoldIngotId);
            //account for blocks
            ingots += progress.TimesPickedUp(ModernId ? ItemId : LegacyItemId) * 9;
            ingots -= progress.TimesDropped(ModernId ? ItemId : LegacyItemId) * 9;
            ingots -= progress.TimesUsed(ModernId ? ItemId : LegacyItemId) * 9;
            //account for crafting of armor/tools
            ingots -= progress.TimesCrafted("minecraft:golden_pickaxe") * 3;
            ingots -= progress.TimesCrafted("minecraft:golden_helmet") * 5;
            ingots -= progress.TimesCrafted("minecraft:golden_chestplate") * 8;
            ingots -= progress.TimesCrafted("minecraft:golden_leggings") * 7;
            ingots -= progress.TimesCrafted("minecraft:golden_boots") * 4;
            //account for crafting of foods
            ingots -= (int)(progress.TimesCrafted("minecraft:golden_carrot") * (8f / 9));
            ingots -= progress.TimesCrafted("minecraft:golden_apple") * 8;

            int blocks = (int)Math.Round(ingots / 9f, MidpointRounding.AwayFromZero);
            this.estimatedBlocks = Math.Max(0, blocks);
        }

        protected override void ClearAdvancedState()
        {
            this.fullBeaconComplete = false;
            this.estimatedBlocks = 0;
        }

        protected override string GetShortStatus()
        {
            return $"{this.estimatedBlocks}\0/\0{Required}";
        }

        protected override string GetLongStatus()
        {
            if (this.fullBeaconComplete)
                return "Full\0Beacon\nConstructed";

            if (this.estimatedBlocks >= Required)
            {
                return $"Gold\0Done\n{this.estimatedBlocks}\0/\0{Required}";
            }
            else if (this.estimatedBlocks > 0)
            {
                return $"Gold\0Estimate\n{this.estimatedBlocks}\0/\0{Required}";
            }
            else
            {
                return $"Gold\0Blocks\n0\0/\0{Required}";
            }
                
        }

        private void RefreshIcon()
        {
            this.Icon = ModernTexture ? "gold_blocks" : "gold_block_1.12";
        }
    }
}
