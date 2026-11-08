using System;
using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;

namespace VentsAndVending.Crafting
{
    /// <summary>
    /// Basic Mixing Station (GDD §6.2). Load ingredients (via the panel), start the mixer, stop it in the green zone,
    /// let it settle, collect the product. Visual state is driven every frame from <see cref="MixingProcess"/>.
    /// </summary>
    public sealed class MixingStation : MonoBehaviour, IInteractable
    {
        /// <summary>Raised when the player opens the station's loading panel.</summary>
        public static event Action<MixingStation, InteractionController> PanelRequested;

        [SerializeField] private string _displayName = "Mixing Station";
        [SerializeField] private List<RecipeDefinition> _recipes = new List<RecipeDefinition>();
        [SerializeField, Min(1)] private int _vesselCapacity = 6;

        [Header("Visuals (optional)")]
        [Tooltip("Pivot at the bottom of the vessel; scaled on Y by fill level.")]
        [SerializeField] private Transform _liquidPivot;
        [SerializeField] private Renderer _liquidRenderer;
        [SerializeField] private Transform _stirrer;
        [Tooltip("Pivot at the bottom of the gauge; scaled on Y by mix progress.")]
        [SerializeField] private Transform _gaugeFillPivot;
        [SerializeField] private Renderer _gaugeFillRenderer;
        [SerializeField] private Renderer _statusLight;
        [SerializeField] private ParticleSystem _smoke;
        [Tooltip("Shakes while the mix is past 100% (GDD §7.6: 'a mixing station starts shaking').")]
        [SerializeField] private Transform _shakeRoot;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private MixingProcess _process;
        private MaterialPropertyBlock _block;
        private Vector3 _shakeRootOrigin;
        private InteractionController _lastUser;
        private float _liquidLevel;

        public string DisplayName => _displayName;
        public MixingProcess Process => _process;
        public IReadOnlyList<RecipeDefinition> Recipes => _recipes;

        private void Awake()
        {
            _process = new MixingProcess(_vesselCapacity);
            _process.Stopped += OnMixStopped;
            _process.Finished += OnMixFinished;
            _block = new MaterialPropertyBlock();
            if (_shakeRoot != null)
            {
                _shakeRootOrigin = _shakeRoot.localPosition;
            }
        }

        private void OnDestroy()
        {
            if (_process != null)
            {
                _process.Stopped -= OnMixStopped;
                _process.Finished -= OnMixFinished;
            }
        }

        // ------------------------------------------------------------------ IInteractable

        public string GetPrompt(InteractionController interactor)
        {
            switch (_process.Phase)
            {
                case MixingPhase.Mixing:
                    return "Stop mixing";
                case MixingPhase.Settling:
                    return $"Settling… {Mathf.CeilToInt(_process.SettleRemaining)}s";
                case MixingPhase.Ready:
                    RecipeDefinition r = _process.ActiveRecipe;
                    return $"Collect {r.Output.DisplayName} x{r.OutputCount} ({_process.ResultQuality})";
                default:
                    return $"Use {_displayName}";
            }
        }

        public void Interact(InteractionController interactor)
        {
            _lastUser = interactor;
            switch (_process.Phase)
            {
                case MixingPhase.Idle:
                    PanelRequested?.Invoke(this, interactor);
                    break;
                case MixingPhase.Mixing:
                    _process.StopMixing();
                    break;
                case MixingPhase.Settling:
                    interactor.Notify("Let it settle…");
                    break;
                case MixingPhase.Ready:
                    Collect(interactor);
                    break;
            }
        }

        // ------------------------------------------------------------------ Panel API

        public RecipeDefinition MatchedRecipe => _process.FindMatch(_recipes);
        public RecipeDefinition ClosestRecipe => _process.FindClosest(_recipes);

        /// <summary>Moves one unit from the player's inventory into the vessel.</summary>
        public bool TryLoad(InteractionController interactor, ItemDefinition item)
        {
            if (!_process.CanLoad)
            {
                interactor.Notify("The vessel is full");
                return false;
            }

            if (interactor.Inventory.Model.Remove(item, 1) == 0)
            {
                return false;
            }

            _process.Load(item);
            return true;
        }

        public bool TryStartMixing(InteractionController interactor)
        {
            _lastUser = interactor;
            return _process.TryStart(MatchedRecipe);
        }

        /// <summary>Returns the vessel's contents to the player; anything that doesn't fit is dropped beside the station.</summary>
        public void EmptyVessel(InteractionController interactor)
        {
            foreach (ItemDefinition item in _process.EmptyVessel())
            {
                if (interactor.Inventory.Model.Add(item, 1) > 0)
                {
                    PickupItem.Spawn(item, 1, ItemQuality.Standard, DropPoint(), Quaternion.identity);
                }
            }
        }

        // ------------------------------------------------------------------ Flow

        private void Collect(InteractionController interactor)
        {
            RecipeDefinition recipe = _process.ActiveRecipe;
            if (interactor.Inventory.Model.SpaceFor(recipe.Output, _process.ResultQuality) < recipe.OutputCount)
            {
                interactor.Notify("Your pockets are full");
                return;
            }

            _process.Collect(out ProductDefinition product, out int count, out ItemQuality quality);
            interactor.Inventory.Model.Add(product, count, quality);
            interactor.Notify($"+{count} {product.DisplayName} ({quality})");
        }

        private void OnMixStopped(ItemQuality quality, bool overheated)
        {
            if (overheated)
            {
                if (_smoke != null)
                {
                    _smoke.Play();
                }
                Tell("OVERHEATED! The batch has become… an unexpected variation.");
            }
            else
            {
                Tell($"Mix stopped: {quality}. Letting it settle…");
            }
        }

        private void OnMixFinished() => Tell($"{_process.ActiveRecipe.Output.DisplayName} is ready");

        private void Update()
        {
            _process.Tick(Time.deltaTime);
            UpdateVisuals();
        }

        private void Tell(string message)
        {
            if (_lastUser != null)
            {
                _lastUser.Notify(message);
            }
        }

        private Vector3 DropPoint() => transform.position + transform.forward * 0.6f + Vector3.up * 1.1f;

        // ------------------------------------------------------------------ Visuals

        private void UpdateVisuals()
        {
            MixingPhase phase = _process.Phase;
            float progress = _process.Progress;

            // Liquid level & colour
            float targetLevel = (float)_process.Loaded.Count / _process.Capacity;
            _liquidLevel = Mathf.MoveTowards(_liquidLevel, targetLevel, Time.deltaTime * 1.5f);
            if (_liquidPivot != null)
            {
                _liquidPivot.localScale = new Vector3(1f, Mathf.Max(0.001f, _liquidLevel), 1f);
                _liquidPivot.gameObject.SetActive(_liquidLevel > 0.002f);
            }

            Color ingredients = AverageIngredientColor();
            Color product = _process.ActiveRecipe != null ? _process.ActiveRecipe.Output.UiColor : ingredients;
            Color liquid = phase switch
            {
                MixingPhase.Mixing => Color.Lerp(ingredients, product, Mathf.Clamp01(progress)),
                MixingPhase.Settling or MixingPhase.Ready => _process.Overheated ? Color.Lerp(product, new Color(0.5f, 0.2f, 0.6f), 0.6f) : product,
                _ => ingredients
            };
            float glow = phase == MixingPhase.Mixing ? 1.2f + Mathf.Sin(Time.time * 12f) * 0.3f : 0.8f;
            SetColor(_liquidRenderer, liquid * 0.7f, liquid * glow);

            // Stirrer
            if (_stirrer != null && phase == MixingPhase.Mixing)
            {
                float speed = Mathf.Lerp(360f, 1080f, Mathf.Clamp01(progress));
                _stirrer.Rotate(0f, speed * Time.deltaTime, 0f, Space.Self);
            }

            // Gauge
            float gauge = phase == MixingPhase.Mixing || phase == MixingPhase.Settling ? progress / MixingRules.AutoStopAt : 0f;
            if (_gaugeFillPivot != null)
            {
                _gaugeFillPivot.localScale = new Vector3(1f, Mathf.Max(0.001f, gauge), 1f);
            }
            SetColor(_gaugeFillRenderer, GaugeColor(progress) * 0.5f, GaugeColor(progress) * 1.5f);

            // Status light
            Color status = phase switch
            {
                MixingPhase.Mixing => progress >= MixingRules.PremiumTo
                    ? (Mathf.Repeat(Time.time * 6f, 1f) > 0.5f ? Color.red : new Color(0.3f, 0f, 0f))
                    : GaugeColor(progress),
                MixingPhase.Settling => new Color(1f, 0.6f, 0.1f),
                MixingPhase.Ready => Color.green * (0.6f + Mathf.PingPong(Time.time * 2f, 0.6f)),
                _ => MatchedRecipe != null ? new Color(1f, 0.75f, 0.2f) : new Color(0.15f, 0.15f, 0.15f)
            };
            SetColor(_statusLight, status * 0.4f, status * 2f);

            // Shake when pushing past 100%
            if (_shakeRoot != null)
            {
                float danger = phase == MixingPhase.Mixing ? Mathf.InverseLerp(MixingRules.PremiumTo, MixingRules.AutoStopAt, progress) : 0f;
                _shakeRoot.localPosition = _shakeRootOrigin + (danger > 0f ? UnityEngine.Random.insideUnitSphere * 0.012f * danger : Vector3.zero);
            }
        }

        private static Color GaugeColor(float progress)
        {
            if (progress >= MixingRules.OverheatFrom)
            {
                return Color.red;
            }
            if (progress > MixingRules.PremiumTo)
            {
                return new Color(1f, 0.45f, 0.05f);
            }
            if (progress >= MixingRules.PremiumFrom)
            {
                return Color.green;
            }
            return new Color(0.1f, 0.8f, 1f);
        }

        private Color AverageIngredientColor()
        {
            IReadOnlyList<ItemDefinition> loaded = _process.Loaded;
            if (loaded.Count == 0)
            {
                return Color.gray;
            }

            Color sum = Color.black;
            foreach (ItemDefinition item in loaded)
            {
                sum += item.UiColor;
            }
            return sum / loaded.Count;
        }

        private void SetColor(Renderer target, Color baseColor, Color emission)
        {
            if (target == null)
            {
                return;
            }

            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, baseColor);
            _block.SetColor(EmissionColorId, emission);
            target.SetPropertyBlock(_block);
        }

        internal void ConfigureRecipes(IEnumerable<RecipeDefinition> recipes)
        {
            _recipes = new List<RecipeDefinition>(recipes);
        }
    }
}
