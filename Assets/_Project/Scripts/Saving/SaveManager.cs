using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VentsAndVending.Core;
using VentsAndVending.Economy;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.Player;
using VentsAndVending.Progression;
using VentsAndVending.Security;
using VentsAndVending.Tunnels;

namespace VentsAndVending.Saving
{
    /// <summary>
    /// Saving and loading (GDD §18.5): manual save, autosave, save-on-exit, versioned JSON, a backup of the previous
    /// save and fallback to it if the main file is damaged. Runs after other systems' Start so bonuses are in place.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class SaveManager : MonoBehaviour
    {
        [SerializeField] private GameDatabase _database;
        [SerializeField, Min(10f)] private float _autosaveSeconds = 120f;
        [SerializeField, Min(0)] private int _slot;

        private float _autosaveTimer;

        public static SaveManager Instance { get; private set; }

        internal static string ReviewDirectory;
        public static string SaveDirectory => ReviewDirectory ?? Path.Combine(Application.persistentDataPath, "Saves");
        public static string SavedSceneName()
        {
            if(TryRead(PathFor(0),out var data,out _) || TryRead(PathFor(0)+".bak",out data,out _))
                return data.SceneName==Core.SceneFlow.GameScene ? Core.SceneFlow.GameScene : "Greybox_Movement";
            return Core.SceneFlow.GameScene;
        }
        public static string PathFor(int slot) => Path.Combine(SaveDirectory, $"save_{slot}.json");
        public static bool HasSave(int slot = 0) => File.Exists(PathFor(slot)) || File.Exists(PathFor(slot) + ".bak");

        private void Awake() => Instance = this;

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            if (SceneFlow.ContinueRequested)
            {
                SceneFlow.ContinueRequested = false;
                Load();
            }
        }

        private void Update()
        {
            _autosaveTimer += Time.deltaTime; // paused game (timeScale 0) doesn't autosave
            if (_autosaveTimer >= _autosaveSeconds)
            {
                _autosaveTimer = 0f;
                Save("Autosaved");
            }
        }

        private void OnApplicationQuit() => Save(null);

        // ------------------------------------------------------------------ Save

        /// <summary>Writes the save. <paramref name="toast"/> is shown to the player (null = silent).</summary>
        public bool Save(string toast = "Game saved")
        {
            try
            {
                SaveData data = Capture();
                if (data == null)
                {
                    return false;
                }

                Directory.CreateDirectory(SaveDirectory);
                string path = PathFor(_slot);
                string temp = path + ".tmp";
                File.WriteAllText(temp, SaveCodec.ToJson(data));
                if (TryRead(path, out _, out _))
                {
                    File.Copy(path, path + ".bak", true); // keep the previous save — only if it was actually valid
                }
                File.Copy(temp, path, true);
                File.Delete(temp);
                _autosaveTimer = 0f;

                if (toast != null)
                {
                    Notify(toast);
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e}");
                Notify("<color=#FF6060>Save failed</color>");
                return false;
            }
        }

        private SaveData Capture()
        {
            var interactor = FindAnyObjectByType<InteractionController>();
            if (interactor == null)
            {
                return null;
            }

            var data = new SaveData { SavedAtUtc = DateTime.UtcNow.ToString("o") };
            data.SceneName=UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            foreach(var station in FindObjectsByType<Crafting.MixingStation>(FindObjectsSortMode.None))
                data.Mixers.Add(station.Process.Capture(station.name));
            foreach(var vein in FindObjectsByType<World.CrystalVein>(FindObjectsSortMode.None))
                data.Deposits.Add(vein.Deposit.Capture(vein.SaveId));
            foreach(var refinery in FindObjectsByType<Crafting.CrystalRefinery>(FindObjectsSortMode.None))
                data.Refineries.Add(refinery.Process.Capture(refinery.SaveId));
            Transform player = interactor.transform;
            data.Player = new PlayerSave { X = player.position.x, Y = player.position.y, Z = player.position.z, Yaw = player.eulerAngles.y };

            var wallet = interactor.GetComponent<PlayerWallet>();
            data.Credits = wallet != null ? wallet.Wallet.Balance : 0;

            if (SecurityManager.Instance != null)
            {
                data.SecurityIncidents = SecurityManager.Instance.Incidents;
            }

            if (TunnelManager.Instance != null)
            {
                data.Tunnels = TunnelManager.Instance.Capture();
            }

            if (VentsAndVending.Orders.OrderManager.Instance != null)
            {
                data.Orders = VentsAndVending.Orders.OrderManager.Instance.Capture();
            }

            var body = FindAnyObjectByType<VentsAndVending.Player.Appearance.PlayerBody>();
            if (body != null)
            {
                data.Appearance = body.Capture();
            }

            if (ProgressionManager.Instance != null)
            {
                foreach (UpgradeDefinition upgrade in ProgressionManager.Instance.Ledger.Owned)
                {
                    data.Upgrades.Add(upgrade.Id);
                }
            }

            foreach (InventoryComponent inventory in FindObjectsByType<InventoryComponent>(FindObjectsSortMode.None))
            {
                if (!string.IsNullOrEmpty(inventory.SaveId))
                {
                    data.Inventories.Add(new InventorySave { Id = inventory.SaveId, Stacks = SaveCodec.CaptureInventory(inventory.Model) });
                }
            }

            return data;
        }

        // ------------------------------------------------------------------ Load

        public bool Load()
        {
            string path = PathFor(_slot);
            if (TryRead(path, out SaveData data, out string error))
            {
                Restore(data);
                Notify("Save loaded");
                return true;
            }

            if (TryRead(path + ".bak", out data, out string backupError))
            {
                Debug.LogWarning($"[SaveManager] Main save {error}; loaded backup instead.");
                Restore(data);
                Notify("<color=#FFB040>Save file was damaged — loaded the backup</color>");
                return true;
            }

            Debug.LogWarning($"[SaveManager] Could not load save ({error}; backup: {backupError}). Starting fresh.");
            Notify("<color=#FF6060>Couldn't load the save — starting fresh</color>");
            return false;
        }

        private static bool TryRead(string path, out SaveData data, out string error)
        {
            data = null;
            if (!File.Exists(path))
            {
                error = "missing";
                return false;
            }

            try
            {
                return SaveCodec.TryParse(File.ReadAllText(path), out data, out error);
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        private void Restore(SaveData data)
        {
            if (_database == null)
            {
                Debug.LogError("[SaveManager] No GameDatabase assigned; cannot restore items or upgrades.");
                return;
            }

            // Upgrades first so capacity bonuses exist before inventories are filled.
            if (ProgressionManager.Instance != null)
            {
                var owned = new List<UpgradeDefinition>();
                foreach (string id in data.Upgrades)
                {
                    UpgradeDefinition upgrade = _database.FindUpgrade(id);
                    if (upgrade != null)
                    {
                        owned.Add(upgrade);
                    }
                }
                ProgressionManager.Instance.RestoreOwned(owned);
            }

            if (SecurityManager.Instance != null)
            {
                SecurityManager.Instance.RestoreIncidents(data.SecurityIncidents);
            }

            if (TunnelManager.Instance != null)
            {
                TunnelManager.Instance.Restore(data.Tunnels);
            }

            if (VentsAndVending.Orders.OrderManager.Instance != null)
            {
                VentsAndVending.Orders.OrderManager.Instance.Restore(data.Orders);
            }

            var playerBody = FindAnyObjectByType<VentsAndVending.Player.Appearance.PlayerBody>();
            if (playerBody != null)
            {
                playerBody.Restore(data.Appearance);
            }

            var interactor = FindAnyObjectByType<InteractionController>();
            if (interactor != null)
            {
                var wallet = interactor.GetComponent<PlayerWallet>();
                if (wallet != null)
                {
                    wallet.Wallet.Restore(data.Credits);
                }

                var movement = interactor.GetComponent<PlayerMovement>();
                var saved = new Vector3(data.Player.X, data.Player.Y, data.Player.Z);
                // Saves from an older level layout may point into the void: keep the scene spawn if there's no floor there.
                bool onFloor = Physics.Raycast(saved + Vector3.up * 0.5f, Vector3.down, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                Physics.SyncTransforms();
                foreach(var blocker in Physics.OverlapCapsule(saved+Vector3.up*.31f,saved+Vector3.up*.39f,.28f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore))
                    if(!blocker.transform.IsChildOf(interactor.transform)){onFloor=false;break;}
                if (movement != null && onFloor)
                {
                    movement.Teleport(saved, Quaternion.Euler(0f, data.Player.Yaw, 0f));
                }
                else if (movement != null)
                {
                    Debug.LogWarning($"[SaveManager] Saved position {saved} has no floor under it (old layout?) — using the spawn point.");
                }
            }

            var byId = new Dictionary<string, InventoryComponent>();
            foreach (InventoryComponent inventory in FindObjectsByType<InventoryComponent>(FindObjectsSortMode.None))
            {
                if (!string.IsNullOrEmpty(inventory.SaveId))
                {
                    byId[inventory.SaveId] = inventory;
                }
            }

            foreach (InventorySave saved in data.Inventories)
            {
                if (saved != null && saved.Id != null && byId.TryGetValue(saved.Id, out InventoryComponent inventory))
                {
                    int skipped = SaveCodec.RestoreInventory(inventory.Model, saved.Stacks, _database.FindItem);
                    if (skipped > 0)
                    {
                        Debug.LogWarning($"[SaveManager] Skipped {skipped} unknown/invalid entries in '{saved.Id}'.");
                    }
                }
            }
            foreach(var station in FindObjectsByType<Crafting.MixingStation>(FindObjectsSortMode.None))
            {
                var saved=data.Mixers.Find(m=>m!=null && m.Id==station.name);
                if(saved!=null)station.Process.Restore(saved,_database.FindItem,station.Recipes);
            }
            foreach(var vein in FindObjectsByType<World.CrystalVein>(FindObjectsSortMode.None))
                vein.Deposit.Restore(data.Deposits.Find(s=>s!=null && s.Id==vein.SaveId));
            foreach(var refinery in FindObjectsByType<Crafting.CrystalRefinery>(FindObjectsSortMode.None))
                refinery.Process.Restore(data.Refineries.Find(s=>s!=null && s.Id==refinery.SaveId));
        }

        private static void Notify(string message)
        {
            var interactor = FindAnyObjectByType<InteractionController>();
            if (interactor != null)
            {
                interactor.Notify(message);
            }
        }

        internal void SetDatabase(GameDatabase database) => _database = database;
    }
}
