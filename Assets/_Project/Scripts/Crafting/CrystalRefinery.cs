using UnityEngine;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.World;

namespace VentsAndVending.Crafting
{
    public sealed class CrystalRefinery : MonoBehaviour, IInteractable
    {
        [SerializeField] private string _saveId;
        [SerializeField] private ItemDefinition _raw, _output;
        [SerializeField] private TextMesh _display;
        [SerializeField] private Transform _rotor;
        [SerializeField] private AudioClip _sound;
        private float _nextDisplay;
        public string SaveId => _saveId;
        public ItemDefinition Raw => _raw;
        public ItemDefinition Output => _output;
        public RefiningProcess Process { get; } = new RefiningProcess();
        internal void Configure(string id,ItemDefinition raw,ItemDefinition output,TextMesh display,Transform rotor,AudioClip sound)
        { _saveId=id; _raw=raw; _output=output; _display=display; _rotor=rotor; _sound=sound; }
        private void Update()
        {
            Process.Tick(Time.deltaTime);
            if (Process.HasBatch && !Process.Ready && _rotor != null) _rotor.Rotate(0, 120 * Time.deltaTime, 0);
            if (_display == null || Time.unscaledTime < _nextDisplay) return;
            _nextDisplay = Time.unscaledTime + .1f;
            _display.text = Process.Ready ? "REFINERY / READY\nE: COLLECT 2 LUMINA GEL" : Process.HasBatch
                ? $"REFINING CRYSTALS\n{Mathf.CeilToInt(Process.Remaining)}s REMAINING" : "CRYSTAL REFINERY\n3 RAW -> 2 LUMINA GEL\nE: LOAD / START (8s)";
        }
        public string GetPrompt(InteractionController interactor) => Process.Ready ? "Collect 2 Lumina Gel" : Process.HasBatch
            ? $"Refining: {Mathf.CeilToInt(Process.Remaining)}s" : "Refine 3 raw core crystals into 2 Lumina Gel";
        public void Interact(InteractionController interactor)
        {
            if (interactor?.Inventory == null || Time.timeScale <= 0) return;
            if (Process.Ready)
            {
                if (Process.Collect(interactor.Inventory.Model,_output)) { interactor.Notify("+2 Lumina Gel. Ready for the mixing table."); Sfx.PlayAt(_sound,transform.position,.5f); }
                else interactor.Notify("Make room for 2 Lumina Gel. Your batch is safe in the refinery.");
            }
            else if (Process.HasBatch) interactor.Notify($"Refinery running: {Mathf.CeilToInt(Process.Remaining)} seconds remaining.");
            else if (Process.Start(interactor.Inventory.Model,_raw)) { interactor.Notify("Loaded 3 raw crystals. Refining takes 8 seconds; you can walk away."); Sfx.PlayAt(_sound,transform.position,.5f); }
            else interactor.Notify("Carry 3 raw core crystals. Follow the far-side ring Tool Store to the core; a pickaxe is beside it.");
        }
    }
}
