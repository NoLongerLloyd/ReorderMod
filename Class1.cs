using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using UnityEngine;
using System;
using System.Collections.Generic;
using On.RoR2;
using BepInEx.Configuration;


namespace NoLongerLloyd
{
    [BepInDependency("com.bepis.r2api")]
    //Change these
    [BepInPlugin("com.NoLongerLloyd.Reorderer", "Reorders your inventory when you level up", "1.2.0")]


    public class Reorderer : BaseUnityPlugin
    {

		private static ConfigEntry<int> tier1Min { get; set; }
		private static ConfigEntry<int> tier1Max { get; set; }

		private static ConfigEntry<int> tier2Min { get; set; }
		private static ConfigEntry<int> tier2Max { get; set; }

		private static ConfigEntry<int> tier3Min { get; set; }
		private static ConfigEntry<int> tier3Max { get; set; }

		private static ConfigEntry<int> tierLunarMin { get; set; }
		private static ConfigEntry<int> tierLunarMax { get; set; }

		public void Awake()
		{
			tier1Min = Config.Bind<int>("settings","MinTier1Items",1,"Minnimum number of tier 1 items that could be created on randomize");
			tier1Max = Config.Bind<int>("settings", "MaxTier1Items", 5, "Maximum number of tier 1 items that could be created on randomize");

			tier2Min = Config.Bind<int>("settings", "MinTier2Items", 1, "Minnimum number of tier 2 items that could be created on randomize");
			tier2Max = Config.Bind<int>("settings", "MaxTier2Items", 3, "Maximum number of tier 2 items that could be created on randomize");

			tier3Min = Config.Bind<int>("settings", "MinTier3Items", 1, "Minnimum number of tier 3 items that could be created on randomize");
			tier3Max = Config.Bind<int>("settings", "MaxTier3Items", 3, "Maximum number of tier 3 items that could be created on randomize");

			tierLunarMin = Config.Bind<int>("settings", "MinTierLunarItems", 1, "Minnimum number of lunar items that could be created on randomize");
			tierLunarMax = Config.Bind<int>("settings", "MaxTierLunarItems", 3, "Maximum number of lunar items that could be created on randomize");

			currentLevel = 0;
			On.RoR2.GlobalEventManager.CreateLevelUpEffect += new On.RoR2.GlobalEventManager.hook_CreateLevelUpEffect(this.GlobalEventManager_CreateLevelUpEffect);
		}

		private System.Collections.IEnumerator GlobalEventManager_CreateLevelUpEffect(On.RoR2.GlobalEventManager.orig_CreateLevelUpEffect orig, RoR2.GlobalEventManager self, float delay, GameObject levelUpEffect, RoR2.EffectData effectData)
        {
			yield return new WaitForSeconds(delay);
			orig.Invoke(self, delay, levelUpEffect, effectData);

			foreach (RoR2.PlayerCharacterMasterController playerCharacterMasterController in RoR2.PlayerCharacterMasterController.instances)
			{
				bool isClient = playerCharacterMasterController.master.isClient;
				if (isClient)
				{
					string displayName = playerCharacterMasterController.GetDisplayName();
					System.Random rnd = new System.Random();

					RoR2.CharacterMaster localUser = playerCharacterMasterController.master;
					RoR2.CharacterBody playerBody = localUser.GetBody();
					int playerLevel = (int)playerBody.level;

					if (currentLevel < playerLevel) { 
						
						ItemTier itemTier1 = ItemTier.Tier1;
						int amountOfItemsInTier = this.getAmountOfItemsInTier(itemTier1, playerCharacterMasterController.master);
						this.removeAllItemsInTier(itemTier1, playerCharacterMasterController.master);
						this.addItems(itemTier1, amountOfItemsInTier, playerCharacterMasterController.master, rnd, tier1Min.Value, tier1Max.Value);

						ItemTier itemTier2 = ItemTier.Tier2;
						amountOfItemsInTier = this.getAmountOfItemsInTier(itemTier2, playerCharacterMasterController.master);
						this.removeAllItemsInTier(itemTier2, playerCharacterMasterController.master);
						this.addItems(itemTier2, amountOfItemsInTier, playerCharacterMasterController.master, rnd, tier2Min.Value, tier2Max.Value);

						ItemTier itemTier3 = ItemTier.Tier3;
						amountOfItemsInTier = this.getAmountOfItemsInTier(itemTier3, playerCharacterMasterController.master);
						this.removeAllItemsInTier(itemTier3, playerCharacterMasterController.master);
						this.addItems(itemTier3, amountOfItemsInTier, playerCharacterMasterController.master, rnd,tier3Min.Value,tier3Max.Value);

						ItemTier itemTierLunar = ItemTier.Lunar;
						amountOfItemsInTier = this.getAmountOfItemsInTier(itemTierLunar, playerCharacterMasterController.master);
						this.removeAllItemsInTier(itemTierLunar, playerCharacterMasterController.master);
						this.addItems(itemTierLunar, amountOfItemsInTier, playerCharacterMasterController.master, rnd,tierLunarMin.Value,tierLunarMax.Value);
						currentLevel = playerLevel;
					}
				}		
				
			}

			yield break;
		}

		private int getAmountOfItemsInTier(ItemTier tier, RoR2.CharacterMaster localUser)
		{
			return localUser.inventory.GetTotalItemCountOfTier(tier);
		}


		private void addItems(ItemTier item, int amount, RoR2.CharacterMaster localUser, System.Random rnd, int min, int max)
		{
			int amountOfItems;
			if(min > max)
            {
				max = min;
            }

			if (min <= 0)
            {
				amountOfItems = rnd.Next(1, max+1);
			}
            else
            {
				amountOfItems = rnd.Next(min, max+1);
			}

			int itemsToGive = amount / amountOfItems;
			int remainder = amount % amountOfItems;
			ItemIndex itemRolled;

			itemRolled = this.getRandomItem(item, localUser, rnd);
			localUser.inventory.GiveItem(itemRolled, itemsToGive + remainder);

			for (int i = 0; i < amountOfItems-1; i++)
            {

				itemRolled = this.getRandomItem(item, localUser, rnd);
				localUser.inventory.GiveItem(itemRolled, itemsToGive);
			}

		}

		private void removeAllItemsInTier(ItemTier tier, RoR2.CharacterMaster localUser)
		{
			switch (tier)
			{
				case ItemTier.Tier1:
					{
						List<ItemIndex> tier1ItemList = RoR2.ItemCatalog.tier1ItemList;
						for (int i = 0; i < tier1ItemList.Count; i++)
						{
							int itemCount = localUser.inventory.GetItemCount(tier1ItemList[i]);
							bool flag = itemCount > 0;
							if (flag)
							{
								localUser.inventory.RemoveItem(tier1ItemList[i], itemCount);
							}
						}
						break;
					}
				case ItemTier.Tier2:
					{
						List<ItemIndex> tier2ItemList = RoR2.ItemCatalog.tier2ItemList;
						for (int j = 0; j < tier2ItemList.Count; j++)
						{
							int itemCount2 = localUser.inventory.GetItemCount(tier2ItemList[j]);
							bool flag2 = itemCount2 > 0;
							if (flag2)
							{
								localUser.inventory.RemoveItem(tier2ItemList[j], itemCount2);
							}
						}
						break;
					}
				case ItemTier.Tier3:
					{
						List<ItemIndex> tier3ItemList = RoR2.ItemCatalog.tier3ItemList;
						for (int k = 0; k < tier3ItemList.Count; k++)
						{
							int itemCount3 = localUser.inventory.GetItemCount(tier3ItemList[k]);
							bool flag3 = itemCount3 > 0;
							if (flag3)
							{
								localUser.inventory.RemoveItem(tier3ItemList[k], itemCount3);
							}
						}
						break;
					}
				case ItemTier.Lunar:
					{
						List<ItemIndex> lunarItemList = RoR2.ItemCatalog.lunarItemList;
						for (int k = 0; k < lunarItemList.Count; k++)
						{
							int itemCount3 = localUser.inventory.GetItemCount(lunarItemList[k]);
							bool flag3 = itemCount3 > 0;
							if (flag3)
							{
								localUser.inventory.RemoveItem(lunarItemList[k], itemCount3);
							}
						}
						break;
					}

			}
		}

		private ItemIndex getRandomItem(ItemTier tier, RoR2.CharacterMaster localUser, System.Random rnd)
		{
			ItemIndex result = ItemIndex.None;

			switch (tier)
			{
				case ItemTier.Tier1:
					{
						List<ItemIndex> tier1ItemList = RoR2.ItemCatalog.tier1ItemList;
						int tierCount = tier1ItemList.Count;
						int rndCount = rnd.Next(0, tierCount);
						result = tier1ItemList[rndCount];
						Debug.Log(rndCount + " : Tier 1 " + result);
						break;
					}
				case ItemTier.Tier2:
					{
						List<ItemIndex> tier2ItemList = RoR2.ItemCatalog.tier2ItemList;
						int tierCount = tier2ItemList.Count;
						int rndCount = rnd.Next(0, tierCount);
						result = tier2ItemList[rndCount];
						Debug.Log(rndCount + " : Tier 2 " + result);
						break;
					}
				case ItemTier.Tier3:
					{
						List<ItemIndex> tier3ItemList = RoR2.ItemCatalog.tier3ItemList;
						int tierCount = tier3ItemList.Count;
						int rndCount = rnd.Next(0, tierCount);
						result = tier3ItemList[rndCount];
						Debug.Log(rndCount + " : Tier 3 " + result);
						break;
					}
				case ItemTier.Lunar:
					{
						List<ItemIndex> lunarItemList = RoR2.ItemCatalog.lunarItemList;
						int tierCount = lunarItemList.Count;
						int rndCount = rnd.Next(0, tierCount);
						result = lunarItemList[rndCount];
						Debug.Log(rndCount + " : Tier Lunar " + result);
						break;
					}
			}
			return result;
		}
		private int currentLevel;
	}

}

