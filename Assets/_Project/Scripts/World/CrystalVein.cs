using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.World
{
    public sealed class CrystalVein : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _saveId;
        [SerializeField] private ItemDefinition _pickaxe, _raw;
        [SerializeField] private Transform _growth;
        [SerializeField] private AudioClip _strikeSound;
        private float _nextStrike;
        private Vector3 _growthScale;
        private bool _wasAvailable;
        public string SaveId => _saveId;
        public CrystalDeposit Deposit { get; } = new CrystalDeposit();
        internal void Configure(string id, ItemDefinition pickaxe, ItemDefinition raw, Transform growth, AudioClip sound)
        { _saveId = id; _pickaxe = pickaxe; _raw = raw; _growth = growth; _strikeSound = sound; }
        private void Awake() { if (_growth != null) _growthScale = _growth.localScale; _wasAvailable = true; }
        private void Update()
        {
            Deposit.Tick(Time.deltaTime);
            if (_growth != null)
            {
                _growth.localScale = _growthScale * (Deposit.Available ? 1 : .16f);
                if (Deposit.Available && _wasAvailable)
                    _growth.localRotation = Quaternion.Euler(0, 0, Mathf.Sin((Time.time - _nextStrike + .65f) * 45) * Mathf.Max(0, _nextStrike - Time.time) * 5);
            }
            _wasAvailable = Deposit.Available;
        }
        public string GetPrompt(InteractionController interactor)
        {
            if (!Deposit.Available) return $"Crystal regrowth: {Mathf.CeilToInt(Deposit.RegrowRemaining)}s";
            if (interactor.Inventory == null || interactor.Inventory.Model.Count(_pickaxe) == 0) return "Pickaxe required - take the spare by the entrance";
            return $"Mine core crystal ({Deposit.Hits}/{CrystalDeposit.Strikes} strikes)";
        }
        public void Interact(InteractionController interactor)
        {
            if (interactor == null || Time.time < _nextStrike || Time.timeScale <= 0) return;
            bool hit = Deposit.Strike(interactor.Inventory?.Model, _pickaxe, _raw, out string message);
            if (hit) { _nextStrike = Time.time + .65f; Sfx.PlayAt(_strikeSound, transform.position, .65f, 1.35f); }
            interactor.Notify(message);
        }
    }
}
