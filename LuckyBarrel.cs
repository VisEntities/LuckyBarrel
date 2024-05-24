using Newtonsoft.Json;
using System.Collections.Generic;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Lucky Barrel", "VisEntities", "1.0.0")]
    [Description("Place a storage barrel, and with a bit of luck, it might turn into a random loot barrel.")]
    public class LuckyBarrel : RustPlugin
    {
        #region Fields

        private static LuckyBarrel _plugin;
        private static Configuration _config;
        private const string PREFAB_STORAGE_BARREL_B = "assets/prefabs/misc/decor_dlc/storagebarrel/storage_barrel_b.prefab";
        
        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty("Version")]
            public string Version { get; set; }

            [JsonProperty("Always Turn Into Loot Barrel")]
            public bool AlwaysTurnIntoLootBarrel { get; set; }

            [JsonProperty("Chance To Spawn Loot Barrel")]
            public int ChanceToSpawnLootBarrel { get; set; }

            [JsonProperty("Loot Barrel Prefabs")]
            public List<string> LootBarrelPrefabs { get; set; }
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

            PrintWarning("Config update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                AlwaysTurnIntoLootBarrel = true,
                ChanceToSpawnLootBarrel = 50,
                LootBarrelPrefabs = new List<string>
                {
                    "assets/content/structures/excavator/prefabs/diesel_collectable.prefab",
                    "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-1.prefab",
                    "assets/bundled/prefabs/autospawn/resource/loot/loot-barrel-2.prefab",
                    "assets/bundled/prefabs/radtown/loot_barrel_1.prefab",
                    "assets/bundled/prefabs/radtown/loot_barrel_2.prefab",
                    "assets/bundled/prefabs/radtown/oil_barrel.prefab"
                }
            };
        }

        #endregion Configuration

        #region Oxide Hooks

        private void Init()
        {
            _plugin = this;
            PermissionUtil.RegisterPermissions();
        }

        private void Unload()
        {
            _config = null;
            _plugin = null;
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            if (planner == null || gameObject == null)
                return;

            BasePlayer player = planner.GetOwnerPlayer();
            if (player == null)
                return;

            if (!PermissionUtil.HasPermission(player, PermissionUtil.USE))
                return;

            if (!_config.AlwaysTurnIntoLootBarrel && !player.serverInput.WasDown(BUTTON.FIRE_SECONDARY))
                return;

            if (_config.AlwaysTurnIntoLootBarrel && !ChanceSucceeded(_config.ChanceToSpawnLootBarrel))
                return;

            BaseEntity entity = gameObject.ToBaseEntity();
            if (entity == null)
                return;

            if (entity.PrefabName != PREFAB_STORAGE_BARREL_B)
                return;

            NextTick(() =>
            {
                if (entity != null)
                {
                    Vector3 position = entity.transform.position;
                    Quaternion rotation = entity.transform.rotation;

                    entity.Kill(BaseNetworkable.DestroyMode.Gib);
                    string randomBarrelPrefab = _config.LootBarrelPrefabs.GetRandom();
                    SpawnBarrel(randomBarrelPrefab, position, rotation);
                }
            });
        }

        #endregion Oxide Hooks

        #region Barrel Spawning

        private BaseEntity SpawnBarrel(string prefabPath, Vector3 position, Quaternion rotation, bool wakeUpNow = true)
        {
            BaseEntity barrel = GameManager.server.CreateEntity(prefabPath, position, rotation, wakeUpNow);
            if (barrel == null)
                return null;

            barrel.Spawn();
            return barrel;
        }

        #endregion Barrel Spawning

        #region Helper Functions

        private bool ChanceSucceeded(int chance)
        {
            return Random.Range(0, 100) < chance;
        }

        #endregion Helper Functions

        #region Permissions

        private static class PermissionUtil
        {
            public const string USE = "luckybarrel.use";
            private static readonly List<string> _permissions = new List<string>
            {
                USE,
            };

            public static void RegisterPermissions()
            {
                foreach (var permission in _permissions)
                {
                    _plugin.permission.RegisterPermission(permission, _plugin);
                }
            }

            public static bool HasPermission(BasePlayer player, string permissionName)
            {
                return _plugin.permission.UserHasPermission(player.UserIDString, permissionName);
            }
        }

        #endregion Permissions
    }
}