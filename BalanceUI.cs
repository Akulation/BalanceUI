using Rocket.Core.Plugins;
using Rocket.Unturned;
using Rocket.Unturned.Player;
using System;
using System.Linq;
using SDG.Unturned;
using Rocket.Core.Logging;
using System.Reflection;
using System.IO;
using Rocket.Core;
using BalanceUI.Providers;
using Rocket.Unturned.Events;
using Rocket.Unturned.Enumerations;
using System.Collections.Generic;
using static SDG.Unturned.ItemCurrencyAsset;

namespace BalanceUI
{
    public class BalanceUI : RocketPlugin<Config>
    {
        public static BalanceUI Instance { get; private set; }

        public static ICurrencyProvider CurrencyProvider { get; set; }

        public static int EnabledCurrencies(params bool[] booleans)
        {
            return booleans.Count(b => b);
        }
        public string EnabledCurrency = "";

        public Guid guid;

        public bool UconomyInstalled;

        protected override void Load()
        {
            Instance = this;

            CurrencyProvider = new UconomyCurrencyProvider();

            if (EnabledCurrencies(Configuration.Instance.UseEXP, Configuration.Instance.UseUconomy, Configuration.Instance.UseItemCurrency) > 1)
            {
                Logger.LogError("You can only have one type of currency enabled!");
                Logger.LogError("Unloading BalanceUI..");
                Unload();
                return;
            }
            if (EnabledCurrencies(Configuration.Instance.UseEXP, Configuration.Instance.UseUconomy, Configuration.Instance.UseItemCurrency) < 0)
            {
                Logger.Log("No enabled currency found, defaulting to currency 'EXP'");
                EnabledCurrency = "EXP";
            }
            if (Configuration.Instance.UseEXP)
            {
                Logger.Log("Selected currency 'EXP'");
                EnabledCurrency = "EXP";
            }
            if (Configuration.Instance.UseUconomy)
            {
                Logger.Log("Selected currency 'Uconomy'");
            }
            if (Configuration.Instance.UseItemCurrency)
            {
                Guid.TryParse(Configuration.Instance.ItemCurrencyGUID, out guid);
                if (guid == null)
                {
                    Logger.LogError("Invalid ItemCurrencyGUID");
                    Logger.LogError("Unloading BalanceUI..");
                    Unload();
                    return;
                }
                Asset asset = Assets.find(guid);
                if (asset is not ItemCurrencyAsset itemCurrencyAsset)
                {
                    Logger.LogError("Invalid ItemCurrencyGUID");
                    Unload();
                    return;
                }
                Logger.Log("Selected currency 'ItemCurrency'");
                EnabledCurrency = "ITEMCURRENCY";
                UnturnedPlayerEvents.OnPlayerInventoryAdded += OnPlayerInventoryAdded;
                UnturnedPlayerEvents.OnPlayerInventoryRemoved += OnPlayerInventoryRemoved;
            }
            R.Plugins.OnPluginsLoaded += OnPluginsLoaded;
            U.Events.OnPlayerConnected += OnPlayerConnected;
            U.Events.OnPlayerDisconnected += OnPlayerDisconnected;
            if (Configuration.Instance.UseEXP)
            {
                UnturnedPlayerEvents.OnPlayerUpdateExperience += OnPlayerUpdateExperience;
            }
        }

        protected override void Unload()
        {
            U.Events.OnPlayerConnected -= OnPlayerConnected;
            U.Events.OnPlayerDisconnected -= OnPlayerDisconnected;
            if (Configuration.Instance.UseEXP)
            {
                UnturnedPlayerEvents.OnPlayerUpdateExperience -= OnPlayerUpdateExperience;
            }
            if (EnabledCurrency == "UCONOMY")
            {
                CurrencyProvider.Dispose();
            }
            if (EnabledCurrency == "ITEMCURRENCY")
            {
                UnturnedPlayerEvents.OnPlayerInventoryAdded -= OnPlayerInventoryAdded;
                UnturnedPlayerEvents.OnPlayerInventoryRemoved -= OnPlayerInventoryRemoved;
            }
        }

        private void OnPluginsLoaded()
        {
            if (Configuration.Instance.UseUconomy)
            {
                try
                {
                    Assembly.Load("Uconomy, Version=1.0.4.0, Culture=neutral, PublicKeyToken=null");
                    UconomyInstalled = true;
                    EnabledCurrency = "UCONOMY";

                    CurrencyProvider.Init();
                }
                catch (FileNotFoundException)
                {
                    UconomyInstalled = false;
                    Logger.LogError("Uconomy is enabled in configuration but you do not have Uconomy installed!");
                    Logger.LogError("Unloading BalanceUI..");
                    Unload();
                    return;
                }
            }
        }

        public uint GetPlayerItemCurrency(UnturnedPlayer player, Guid guid)
        {
            Asset asset = Assets.find(guid);
            ItemCurrencyAsset itemCurrencyAsset = asset as ItemCurrencyAsset;
            Player uplayer = PlayerTool.getPlayer(player.CSteamID);
            uint balance = itemCurrencyAsset.getInventoryValue(uplayer);
            return balance;
        }

        public uint GetItemValue(ushort id, Guid guid)
        {
            Asset asset = Assets.find(guid);
            ItemCurrencyAsset itemCurrencyAsset = asset as ItemCurrencyAsset;
            List<Guid> CurrencyGUIDs = new List<Guid>();
            List<uint> CurrencyValues = new List<uint>();
            foreach (Entry entry in itemCurrencyAsset.entries)
            {
                CurrencyGUIDs.Add(entry.item.GUID);
                CurrencyValues.Add(entry.value);
            }
            Asset itemasset = Assets.find(EAssetType.ITEM, id);
            if (CurrencyGUIDs.Contains(itemasset.GUID))
            {
                int ListIndex = CurrencyGUIDs.FindIndex(x => x == itemasset.GUID);
                uint amount = CurrencyValues[ListIndex];
                return amount;
            }
            else
            {
                return 0;
            }
        }

        public Guid ToGuid(string input)
        {
            Guid guid;
            Guid.TryParse(input, out guid);
            return guid;
        }

        public async void OnPlayerConnected(UnturnedPlayer player)
        {
            EffectManager.sendUIEffect(Configuration.Instance.UIEffectID, 3174, true);
            switch (EnabledCurrency)
            {
                case "EXP":
                    EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", Configuration.Instance.BalancePrefix + player.Experience.ToString() + Configuration.Instance.BalanceSuffix);
                    break;
                case "UCONOMY":
                    decimal bal = await CurrencyProvider.GetBalance(player.CSteamID.ToString());
                    EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", Configuration.Instance.BalancePrefix + bal.ToString() + Configuration.Instance.BalanceSuffix);
                    break;
                case "ITEMCURRENCY":
                    Guid guid;
                    Guid.TryParse(Configuration.Instance.ItemCurrencyGUID, out guid);

                    EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", Configuration.Instance.BalancePrefix + GetPlayerItemCurrency(player, guid).ToString() + Configuration.Instance.BalanceSuffix);
                    break;
            }
        }

        public void OnPlayerUpdateExperience(UnturnedPlayer player, uint amount)
        {
            if (EnabledCurrency == "EXP")
            {
                EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", Configuration.Instance.BalancePrefix + player.Experience.ToString() + Configuration.Instance.BalanceSuffix);
            }
        }

        private void OnPlayerInventoryAdded(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            if (EnabledCurrency == "ITEMCURRENCY")
            {
                Guid guid = ToGuid(Configuration.Instance.ItemCurrencyGUID);
                uint value = GetItemValue(P.item.id, guid);
                uint fullvalue = GetPlayerItemCurrency(player, guid);
                if (fullvalue == value)
                {
                    EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", Configuration.Instance.BalancePrefix + value.ToString() + Configuration.Instance.BalanceSuffix);
                    return;
                }
                EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", Configuration.Instance.BalancePrefix + fullvalue.ToString() + Configuration.Instance.BalanceSuffix);
            }
        }

        private void OnPlayerInventoryRemoved(UnturnedPlayer player, InventoryGroup inventoryGroup, byte inventoryIndex, ItemJar P)
        {
            if (EnabledCurrency == "ITEMCURRENCY")
            {
                Guid guid = ToGuid(Configuration.Instance.ItemCurrencyGUID);
                uint value = GetItemValue(P.item.id, guid);
                uint fullvalue = GetPlayerItemCurrency(player, guid) - value;
                EffectManager.sendUIEffectText(3174, player.Player.channel.owner.transportConnection, true, "BalanceUI_Text", Configuration.Instance.BalancePrefix + fullvalue.ToString() + Configuration.Instance.BalanceSuffix);
            }
        }

        private void OnPlayerDisconnected(UnturnedPlayer player)
        {
            EffectManager.askEffectClearByID(Configuration.Instance.UIEffectID, player.Player.channel.owner.transportConnection);
        }
    }
}
