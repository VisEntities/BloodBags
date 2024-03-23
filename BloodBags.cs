using Network;
using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
 * Rewritten from scratch and maintained to present by VisEntities
 * Originally created by Default, up to version 1.8.0
 */

namespace Oxide.Plugins
{
    [Info("Blood Bags", "VisEntities", "2.3.0")]
    [Description("Craft and use blood bags to restore health, stop bleeding, boost hydration, and more.")]
    public class BloodBags : RustPlugin
    {
        #region Fields

        private static BloodBags _plugin;
        private static Configuration _config;
        private BloodUsageListenerManager _manager;

        private List<Timer> _activeCraftingTimers = new List<Timer>();

        private const int ITEM_ID_BLOOD = 1776460938;
        private const string ITEM_SHORTNAME_BLOOD = "blood";

        private const string FX_DRINK = "assets/bundled/prefabs/fx/gestures/drink_generic.prefab";
        private const string FX_TAKE_DAMAGE = "assets/bundled/prefabs/fx/takedamage_generic.prefab";
        
        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Instant Health Increase")]
            public float InstantHealthIncrease { get; set; }

            [JsonProperty("Health Increase Over Time")]
            public float HealthIncreaseOverTime { get; set; }

            [JsonProperty("Calorie Boost")]
            public float CalorieBoost { get; set; }

            [JsonProperty("Hydration Boost")]
            public float HydrationBoost { get; set; }

            [JsonProperty("Stop Bleeding")]
            public bool StopBleeding { get; set; }

            [JsonProperty("Temperature Target")]
            public float TemperatureTarget { get; set; }

            [JsonProperty("Radiation Poisoning Reduction")]
            public float RadiationPoisoningReduction { get; set; }

            [JsonProperty("Amount To Consume")]
            public int AmountToConsume { get; set; }

            [JsonProperty("Crafting")]
            public CraftingConfig Crafting { get; set; }
        }

        private class CraftingConfig
        {
            [JsonProperty("Command")]
            public string Command { get; set; }

            [JsonProperty("Workbench Level Required")]
            public float WorkbenchLevelRequired { get; set; }

            [JsonProperty("Health Sacrifice Amount")]
            public float HealthSacrificeAmount { get; set; }

            [JsonProperty("Crafting Time Seconds")]
            public float CraftingTimeSeconds { get; set; }

            [JsonProperty("Crafting Amount")]
            public int CraftingAmount { get; set; }

            [JsonProperty("Ingredients")]
            public List<ItemInfo> Ingredients { get; set; }
        }

        public class ItemInfo
        {
            [JsonProperty("Shortname")]
            public string Shortname { get; set; }

            [JsonProperty("Amount")]
            public int Amount { get; set; }

            [JsonIgnore]
            private bool _validated;

            [JsonIgnore]
            private ItemDefinition _itemDefinition;

            [JsonIgnore]
            public ItemDefinition ItemDefinition
            {
                get
                {
                    if (!_validated)
                    {
                        ItemDefinition matchedItemDefinition = ItemManager.FindItemDefinition(Shortname);
                        if (matchedItemDefinition != null)
                            _itemDefinition = matchedItemDefinition;
                        else
                            return null;

                        _validated = true;
                    }

                    return _itemDefinition;
                }
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Config changes detected! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            if (string.Compare(_config.Version, "2.1.0") < 0)
            {
                _config.Crafting.HealthSacrificeAmount = defaultConfig.Crafting.HealthSacrificeAmount;
            }

            if (string.Compare(_config.Version, "2.2.0") < 0)
            {
                _config.InstantHealthIncrease = defaultConfig.InstantHealthIncrease;
                _config.HealthIncreaseOverTime = defaultConfig.HealthIncreaseOverTime;
            }

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                InstantHealthIncrease = 20f,
                HealthIncreaseOverTime = 20f,
                CalorieBoost = 100f,
                HydrationBoost = 50f,
                StopBleeding = true,
                TemperatureTarget = 25f,
                RadiationPoisoningReduction = 25f,
                AmountToConsume = 2,
                Crafting = new CraftingConfig
                {
                    Command = "craftblood",
                    WorkbenchLevelRequired = 1,
                    HealthSacrificeAmount = 15f,
                    CraftingTimeSeconds = 10f,
                    Ingredients = new List<ItemInfo>
                    {
                        new ItemInfo
                        {
                            Shortname = "cloth",
                            Amount = 20
                        },
                        new ItemInfo
                        {
                            Shortname = "ducttape",
                            Amount = 1
                        },
                        new ItemInfo
                        {
                            Shortname = "skull.human",
                            Amount = 1
                        }
                    }
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            _manager = new BloodUsageListenerManager();

            PermissionUtil.RegisterPermissions();
            cmd.AddChatCommand(_config.Crafting.Command, this, nameof(cmdCraftBloodBag));
        }

        private void Unload()
        {
            foreach (Timer timer in _activeCraftingTimers)
            {
                if (timer != null)
                    timer.Destroy();
            }

            _manager.Unload();
            _config = null;
            _plugin = null;
        }

        private void OnActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
            if (player == null || !PermissionUtil.VerifyHasPermission(player))
                return;

            bool isOldItemBloodBag = oldItem != null && oldItem.info.shortname == ITEM_SHORTNAME_BLOOD;
            bool isNewItemBloodBag = newItem != null && newItem.info.shortname == ITEM_SHORTNAME_BLOOD;

            if (isOldItemBloodBag && isNewItemBloodBag)
                return;

            if (isNewItemBloodBag)
            {
                BloodUsageListenerComponent component = _manager.GetBloodUsageListener(player);
                if (component == null)
                {
                    _manager.AddBloodUsageListener(player, newItem);
                    SendGameTip(player, lang.GetMessage(Lang.UseInstruction, this, player.UserIDString), 5f);
                }
            }
            else if (isOldItemBloodBag && !isNewItemBloodBag)
            {
                BloodUsageListenerComponent component = _manager.GetBloodUsageListener(player);
                if (component != null)
                {
                    _manager.DestroyBloodUsageListener(player);
                }
            }
        }

        #endregion Oxide Hooks

        #region Blood Usage Listener Component

        public class BloodUsageListenerManager
        {
            private Dictionary<BasePlayer, BloodUsageListenerComponent> _bloodUsageListeners = new Dictionary<BasePlayer, BloodUsageListenerComponent>();

            public bool AddBloodUsageListener(BasePlayer player, Item bloodItem)
            {
                if (_bloodUsageListeners.ContainsKey(player))
                    return false;

                BloodUsageListenerComponent component = BloodUsageListenerComponent.InstallComponent(player, this, bloodItem);
                _bloodUsageListeners[player] = component;
                return true;
            }

            public void HandleBloodUsageListenerDestroyed(BasePlayer player)
            {
                if (_bloodUsageListeners.ContainsKey(player))
                    _bloodUsageListeners.Remove(player);
            }

            public void Unload()
            {
                // Convert to an array to avoid 'InvalidOperationException' due to modifying the collection during iteration. This creates a snapshot of the collection.
                foreach (BloodUsageListenerComponent component in _bloodUsageListeners.Values.ToArray())
                {
                    if (component != null)
                        component.DestroyComponent();
                }
            }

            public void DestroyBloodUsageListener(BasePlayer player)
            {
                if (_bloodUsageListeners.TryGetValue(player, out BloodUsageListenerComponent component) && component != null)
                    component.DestroyComponent();
            }

            public BloodUsageListenerComponent GetBloodUsageListener(BasePlayer player)
            {
                if (_bloodUsageListeners.TryGetValue(player, out BloodUsageListenerComponent component))
                    return component;

                return null;
            }

            public bool BloodUsageListenerCurrentlyActive(BasePlayer player)
            {
                return _bloodUsageListeners.ContainsKey(player);
            }
        }

        public class BloodUsageListenerComponent : FacepunchBehaviour
        {
            #region Fields

            public BasePlayer Player { get; set; }

            private BloodUsageListenerManager _manager;
            private InputState _playerInput;
            private Item _bloodItem;

            private bool _useButtonPressed = false;

            #endregion Fields

            #region Component Management

            public static BloodUsageListenerComponent InstallComponent(BasePlayer player, BloodUsageListenerManager manager, Item bloodItem)
            {
                BloodUsageListenerComponent component = player.gameObject.AddComponent<BloodUsageListenerComponent>();
                component.InitializeComponent(manager, bloodItem);
                return component;
            }

            public BloodUsageListenerComponent InitializeComponent(BloodUsageListenerManager manager, Item bloodItem)
            {
                Player = GetComponent<BasePlayer>();

                _manager = manager;
                _bloodItem = bloodItem;
                _playerInput = Player.serverInput;

                return this;
            }

            public static BloodUsageListenerComponent GetComponent(BasePlayer player)
            {
                return player.gameObject.GetComponent<BloodUsageListenerComponent>();
            }

            public void DestroyComponent()
            {
                DestroyImmediate(this);
            }

            #endregion Component Management

            #region Component Lifecycle

            private void Update()
            {
                if (_bloodItem != null && _playerInput.WasJustPressed(BUTTON.USE) && !_useButtonPressed)
                {
                    int amount = GetItemAmount(ITEM_ID_BLOOD, Player.inventory.containerBelt);
                    if (amount < _config.AmountToConsume)
                    {
                        _plugin.SendReplyToPlayer(Player, Lang.NotEnoughBloodBags, _config.AmountToConsume);
                    }
                    else
                    {
                        ConsumeBlood();
                    }
                    _useButtonPressed = true;
                }
                else if (_playerInput.WasJustReleased(BUTTON.USE))
                {
                    _useButtonPressed = false;
                }
            }

            private void OnDestroy()
            {
                _manager.HandleBloodUsageListenerDestroyed(Player);
            }

            #endregion Component Lifecycle

            #region Consuming

            private void ConsumeBlood()
            {
                float healthIncrease = _config.InstantHealthIncrease;
                Player.Heal(healthIncrease);
                Player.metabolism.ApplyChange(MetabolismAttribute.Type.HealthOverTime, _config.HealthIncreaseOverTime, 1f);

                float calorieBoost = _config.CalorieBoost;
                Player.metabolism.calories.Add(calorieBoost);

                float hydrationBoost = _config.HydrationBoost;
                Player.metabolism.hydration.Add(hydrationBoost);

                if (_config.StopBleeding)
                    Player.metabolism.bleeding.value = 0f;

                float targetTemperature = _config.TemperatureTarget;
                Player.metabolism.temperature.value = Mathf.Lerp(Player.metabolism.temperature.value, targetTemperature, 0.5f);

                float radiationReduction = _config.RadiationPoisoningReduction;
                Player.metabolism.radiation_poison.Subtract(radiationReduction);

                TakeItem(Player, ITEM_ID_BLOOD, _config.AmountToConsume, Player.inventory.containerBelt);
                RunEffect(FX_DRINK, Player, boneId: 698017942);
            }

            #endregion Consuming
        }

        #endregion Blood Usage Listener Component

        #region Utility Classes

        private static class PermissionUtil
        {
            public const string USE = "bloodbags.use";

            public static void RegisterPermissions()
            {
                _plugin.permission.RegisterPermission(USE, _plugin);
            }

            public static bool VerifyHasPermission(BasePlayer player, string permissionName = USE)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Utility Classes

        #region Helper Functions

        private void SendGameTip(BasePlayer player, string message, float durationSeconds, params object[] args)
        {
            message = string.Format(message, args);

            player.SendConsoleCommand("gametip.showgametip", message);
            timer.Once(durationSeconds, () =>
            {
                if (player != null)
                    player.SendConsoleCommand("gametip.hidegametip");
            });
        }


        private static void RunEffect(string prefab, BaseEntity entity, uint boneId = 0, Vector3 localPosition = default(Vector3), Vector3 localDirection = default(Vector3), Connection effectRecipient = null, bool sendToAll = false)
        {
            Effect.server.Run(prefab, entity, boneId, localPosition, localDirection, effectRecipient, sendToAll);
        }

        public static int GetItemAmount(int itemId, ItemContainer container)
        {
            return container.GetAmount(itemId, true);
        }

        public static void GiveItem(BasePlayer player, int itemId, int amount, ItemContainer container)
        {
            container.GiveItem(ItemManager.CreateByItemID(itemId, amount));
            player.Command("note.inv", itemId, amount);
        }

        public static int TakeItem(BasePlayer player, int itemId, int amount, ItemContainer container)
        {
            int amountTaken = container.Take(null, itemId, amount);
            player.Command("note.inv", itemId, -amountTaken);
            return amountTaken;
        }

        #endregion Helper Functions

        #region Commands

        private void cmdCraftBloodBag(BasePlayer player, string command, string[] args)
        {
            if (!PermissionUtil.VerifyHasPermission(player))
            {
                SendReplyToPlayer(player, Lang.NoPermission);
                return;
            }

            string requiredWorkbench = $"Workbench{_config.Crafting.WorkbenchLevelRequired}";
            if (!player.HasPlayerFlag((BasePlayer.PlayerFlags)Enum.Parse(typeof(BasePlayer.PlayerFlags), requiredWorkbench, true)))
            {
                SendReplyToPlayer(player, Lang.NeedWorkbench, _config.Crafting.WorkbenchLevelRequired);
                return;
            }

            foreach (ItemInfo ingredient in _config.Crafting.Ingredients)
            {
                int amount = GetItemAmount(ingredient.ItemDefinition.itemid, player.inventory.containerMain);
                if (amount < ingredient.Amount)
                {
                    SendReplyToPlayer(player, Lang.NotEnoughIngredient, ingredient.Shortname, ingredient.Amount);
                    return;
                }
            }

            if (player.health > _config.Crafting.HealthSacrificeAmount)
            {
                player.Hurt(_config.Crafting.HealthSacrificeAmount);
            }
            else
            {
                SendReplyToPlayer(player, Lang.InsufficientHealth, _config.Crafting.HealthSacrificeAmount);
                return;
            }

            foreach (ItemInfo ingredient in _config.Crafting.Ingredients)
            {
                TakeItem(player, ingredient.ItemDefinition.itemid, ingredient.Amount, player.inventory.containerMain);
            }

            float craftingTimeLeft = _config.Crafting.CraftingTimeSeconds;
            Timer countdownTimer = timer.Repeat(1f, (int)craftingTimeLeft, () =>
            {
                if (player != null && craftingTimeLeft > 0)
                {
                    SendGameTip(player, lang.GetMessage(Lang.CraftingCountdown, this, player.UserIDString), 1f, craftingTimeLeft);
                    craftingTimeLeft--;
                }
            });

            Timer craftingTimer = timer.Once(_config.Crafting.CraftingTimeSeconds, () =>
            {
                if (player != null)
                {
                    GiveItem(player, ITEM_ID_BLOOD, _config.Crafting.CraftingAmount, player.inventory.containerMain);
                }
            });

            _activeCraftingTimers.Add(craftingTimer);
            _activeCraftingTimers.Add(countdownTimer);

            RunEffect(FX_TAKE_DAMAGE, player, boneId: 698017942);
            SendReplyToPlayer(player, Lang.CraftingStart, _config.Crafting.CraftingTimeSeconds);
        }

        #endregion Commands
        
        #region Localization

        private class Lang
        {
            public const string NoPermission = "NoPermission";
            public const string NeedWorkbench = "NeedWorkbench";
            public const string NotEnoughIngredient = "NotEnoughIngredient";
            public const string CraftingStart = "CraftingStart";
            public const string NotEnoughBloodBags = "NotEnoughBloodBags";
            public const string InsufficientHealth = "InsufficientHealth";
            public const string UseInstruction = "UseInstruction";
            public const string CraftingCountdown = "CraftingCountdown";
        }

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                [Lang.NoPermission] = "You do not have permission to craft this item.",
                [Lang.NeedWorkbench] = "You need to be near a workbench level <color=#FFD700>{0}</color> to craft this item.",
                [Lang.NotEnoughIngredient] = "You do not have enough <color=#FFD700>{0}</color>. Required: <color=#FFD700>{1}</color>.",
                [Lang.CraftingStart] = "Crafting blood bag... Please wait <color=#FFD700>{0}</color> seconds.",
                [Lang.NotEnoughBloodBags] = "Not enough blood bags. Required: <color=#FFD700>{0}</color>.",
                [Lang.InsufficientHealth] = "You don't have enough health to craft a blood bag. Required health: <color=#FFD700>{0}</color>.",
                [Lang.UseInstruction] = "Press <color=#FFD700>use</color> to consume",
                [Lang.CraftingCountdown] = "Crafting blood bag, <color=#FFD700>{0}</color> seconds remaining",
            }, this, "en");
        }

        private void SendReplyToPlayer(BasePlayer player, string messageKey, params object[] args)
        {
            string message = lang.GetMessage(messageKey, this, player.UserIDString);
            if (args.Length > 0)
                message = string.Format(message, args);

            SendReply(player, message);
        }

        #endregion Localization
    }
}