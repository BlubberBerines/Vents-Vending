using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VentsAndVending.Player.Appearance
{
    /// <summary>
    /// The player's visible body (GDD §3.1 "their body when looking down"): a cartoon rig whose clothing is a set of
    /// prebuilt variants switched on by <see cref="ClothingDefinition.StyleId"/> and tinted from the item's colours.
    /// The head (and anything worn on it) only casts shadows in first person so it never blocks the camera.
    /// </summary>
    public sealed class PlayerBody : MonoBehaviour
    {
        [Serializable]
        public struct Variant
        {
            public string StyleId;
            public GameObject[] Parts;
            public Renderer[] Primary;
            public Renderer[] Secondary;
        }

        /// <summary>Skin palette: four human tones and four alien ones.</summary>
        public static readonly Color[] SkinPalette =
        {
            new Color(0.98f, 0.84f, 0.72f), new Color(0.86f, 0.64f, 0.48f), new Color(0.62f, 0.42f, 0.3f), new Color(0.36f, 0.24f, 0.17f),
            new Color(0.45f, 0.72f, 0.95f), new Color(0.55f, 0.85f, 0.45f), new Color(0.78f, 0.6f, 0.95f), new Color(1f, 0.62f, 0.32f),
        };

        [SerializeField] private List<ClothingDefinition> _catalogue = new List<ClothingDefinition>();
        [SerializeField] private Renderer[] _skin = new Renderer[0];
        [Tooltip("Base layer (vest + shorts) shown under whatever is worn.")]
        [SerializeField] private Renderer[] _baseLayer = new Renderer[0];
        [Tooltip("Skin renderers on the forearms: hidden when a long-sleeved top covers them.")]
        [SerializeField] private Renderer[] _forearms = new Renderer[0];
        [SerializeField] private Variant[] _variants = new Variant[0];
        [Tooltip("Head and head-slot pieces: shadows only in first person.")]
        [SerializeField] private Renderer[] _headRenderers = new Renderer[0];

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private MaterialPropertyBlock _block;
        private bool _preview;

        public AppearanceState State { get; } = new AppearanceState();
        public IReadOnlyList<ClothingDefinition> Catalogue => _catalogue;

        internal void Configure(List<ClothingDefinition> catalogue, Renderer[] skin, Renderer[] baseLayer, Renderer[] forearms, Variant[] variants, Renderer[] head)
        {
            _catalogue = catalogue;
            _skin = skin;
            _baseLayer = baseLayer;
            _forearms = forearms;
            _variants = variants;
            _headRenderers = head;
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            State.Changed += Apply;
            GiveStarterKit();
        }

        private void OnDestroy() => State.Changed -= Apply;

        public ClothingDefinition Find(string id) => _catalogue.Find(c => c != null && c.Id == id);

        /// <summary>Fresh game (or an old save): own the starter items and wear the first of each slot.</summary>
        public void GiveStarterKit()
        {
            foreach (ClothingDefinition c in _catalogue)
            {
                if (c != null && c.OwnedFromStart)
                {
                    State.Own(c.Id);
                    if (State.Equipped(c.Slot) == null)
                    {
                        State.Equip(c.Slot, c.Id);
                    }
                }
            }
            Apply();
        }

        public AppearanceSave Capture() => State.Capture();

        public void Restore(AppearanceSave save)
        {
            if (AppearanceState.IsBlank(save))
            {
                GiveStarterKit();
                return;
            }
            State.Restore(save);
        }

        /// <summary>Show the head normally (wardrobe preview) or as shadow only (first person).</summary>
        public void SetPreview(bool preview)
        {
            _preview = preview;
            ApplyHeadVisibility();
        }

        private void Apply()
        {
            if (_block == null)
            {
                _block = new MaterialPropertyBlock();
            }

            Color skin = SkinPalette[State.SkinIndex];
            Tint(_skin, skin);

            var active = new HashSet<string>();
            bool bareArms = true;
            foreach (OutfitSlot slot in Enum.GetValues(typeof(OutfitSlot)))
            {
                ClothingDefinition item = Find(State.Equipped(slot));
                if (item == null)
                {
                    continue;
                }
                active.Add(item.StyleId);
                if (slot == OutfitSlot.Torso && !item.BareArms)
                {
                    bareArms = false;
                }
            }

            foreach (Variant v in _variants)
            {
                bool on = active.Contains(v.StyleId);
                foreach (GameObject part in v.Parts)
                {
                    if (part != null)
                    {
                        part.SetActive(on);
                    }
                }
                if (!on)
                {
                    continue;
                }
                ClothingDefinition item = FindByStyle(v.StyleId);
                if (item != null)
                {
                    Tint(v.Primary, item.Primary);
                    Tint(v.Secondary, item.Secondary);
                }
            }

            foreach (Renderer r in _forearms)
            {
                if (r != null)
                {
                    r.enabled = bareArms;
                }
            }
            ApplyHeadVisibility();
        }

        private ClothingDefinition FindByStyle(string styleId)
        {
            foreach (OutfitSlot slot in Enum.GetValues(typeof(OutfitSlot)))
            {
                ClothingDefinition item = Find(State.Equipped(slot));
                if (item != null && item.StyleId == styleId)
                {
                    return item;
                }
            }
            return null;
        }

        private void ApplyHeadVisibility()
        {
            var mode = _preview ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
            foreach (Renderer r in _headRenderers)
            {
                if (r != null)
                {
                    r.shadowCastingMode = mode;
                }
            }
        }

        private void Tint(Renderer[] renderers, Color colour)
        {
            foreach (Renderer r in renderers)
            {
                if (r == null)
                {
                    continue;
                }
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, colour);
                r.SetPropertyBlock(_block);
            }
        }
    }
}
