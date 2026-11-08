using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VentsAndVending.Economy;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.Security
{
    /// <summary>
    /// Coordinates security units (GDD §18.3): overall alert level for the HUD, incident count / vigilance,
    /// and consequences when the player is caught (GDD §11.5).
    /// </summary>
    [DefaultExecutionOrder(-150)]
    [DisallowMultipleComponent]
    public sealed class SecurityManager : MonoBehaviour
    {
        private readonly List<SecurityDrone> _units = new List<SecurityDrone>();

        public static SecurityManager Instance { get; private set; }

        public int Incidents { get; private set; }
        public float Vigilance => SecurityRules.Vigilance(Incidents);
        public IReadOnlyList<SecurityDrone> Units => _units;

        /// <summary>(items confiscated, fine paid)</summary>
        public event Action<int, int> PlayerCaught;

        public AlertState HighestAlert
        {
            get
            {
                AlertState highest = AlertState.Unaware;
                foreach (SecurityDrone unit in _units)
                {
                    if (unit != null && Rank(unit.Alert.State) > Rank(highest))
                    {
                        highest = unit.Alert.State;
                    }
                }
                return highest;
            }
        }

        public float HighestSuspicion => _units.Count == 0 ? 0f : _units.Where(u => u != null).Select(u => u.Alert.Suspicion).DefaultIfEmpty(0f).Max();

        /// <summary>Order of severity for the HUD (Searching sits between Suspicious and Pursuing).</summary>
        public static int Rank(AlertState state) => state switch
        {
            AlertState.Suspicious => 1,
            AlertState.Searching => 2,
            AlertState.Pursuing => 3,
            _ => 0
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[SecurityManager] More than one in the scene; ignoring this one.", this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Register(SecurityDrone unit)
        {
            if (!_units.Contains(unit))
            {
                _units.Add(unit);
            }
        }

        public void Unregister(SecurityDrone unit) => _units.Remove(unit);

        public void RestoreIncidents(int incidents) => Incidents = Mathf.Max(0, incidents);

        /// <summary>Confiscate contraband, fine the player, and raise vigilance.</summary>
        public void HandleCaught(PlayerExposure player)
        {
            int confiscated = 0;
            if (player.Inventory != null)
            {
                foreach (ItemStack stack in SecurityRules.ConfiscateContraband(player.Inventory.Model))
                {
                    confiscated += stack.Count;
                }
            }

            int fine = 0;
            var wallet = player.GetComponent<PlayerWallet>();
            if (wallet != null)
            {
                fine = SecurityRules.Fine(confiscated, wallet.Wallet.Balance);
                wallet.Wallet.TrySpend(fine);
            }

            Incidents++;
            PlayerCaught?.Invoke(confiscated, fine);

            var interactor = player.GetComponent<InteractionController>();
            if (interactor != null)
            {
                string goods = confiscated > 0 ? $"{confiscated} illegal item(s) confiscated. " : string.Empty;
                interactor.Notify($"<color=#FF5050>DETAINED!</color> {goods}Fined {fine} CC. \"Unlicensed molecular condensation is a serious administrative inconvenience.\"");
            }
        }
    }
}
