using System;
using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Inventory;

namespace VentsAndVending.Crafting
{
    [Serializable]
    public sealed class MixingSave
    {
        public string Id,RecipeId;
        public List<string> Ingredients=new List<string>();
        public MixingPhase Phase;
        public float Progress,SettleRemaining;
        public ItemQuality Quality;
        public bool Overheated;
    }
    public enum MixingPhase
    {
        Idle,
        Mixing,
        Settling,
        Ready
    }

    /// <summary>
    /// Mixing quality rules (GDD §7.5–7.6). Progress runs 0 → 1 over the recipe's mix duration and keeps rising;
    /// the player stops it. Starting values — tune in playtests.
    /// </summary>
    public static class MixingRules
    {
        /// <summary>Below this the batch is under-mixed (Poor).</summary>
        public const float UndermixedBelow = 0.6f;
        /// <summary>Premium window start.</summary>
        public const float PremiumFrom = 0.85f;
        /// <summary>Premium window end (also 100% on the gauge's label).</summary>
        public const float PremiumTo = 1.0f;
        /// <summary>Exceptional sweet spot inside the premium window — needs the correct order too.</summary>
        public const float ExceptionalFrom = 0.9f;
        public const float ExceptionalTo = 0.97f;
        /// <summary>Stopping at or beyond this counts as overheated.</summary>
        public const float OverheatFrom = 1.15f;
        /// <summary>The mixer cuts out by itself here (always overheated).</summary>
        public const float AutoStopAt = 1.25f;

        public static bool IsOverheated(float progress) => progress >= OverheatFrom;

        /// <summary>Premium window after upgrades widen it by <paramref name="windowBonus"/> on each side.</summary>
        public static float PremiumStart(float windowBonus) => PremiumFrom - Mathf.Max(0f, windowBonus);
        public static float PremiumEnd(float windowBonus) => Mathf.Min(OverheatFrom - 0.01f, PremiumTo + Mathf.Max(0f, windowBonus));

        public static ItemQuality Grade(float progress, bool correctOrder, float windowBonus = 0f)
        {
            float bonus = Mathf.Max(0f, windowBonus);
            ItemQuality quality;
            if (IsOverheated(progress) || progress < UndermixedBelow)
            {
                quality = ItemQuality.Poor;
            }
            else if (progress >= ExceptionalFrom - bonus * 0.5f && progress <= ExceptionalTo + bonus * 0.5f && correctOrder)
            {
                quality = ItemQuality.Exceptional;
            }
            else if (progress >= PremiumStart(bonus) && progress <= PremiumEnd(bonus))
            {
                quality = ItemQuality.Premium;
            }
            else
            {
                quality = ItemQuality.Standard;
            }

            if (!correctOrder && quality > ItemQuality.Poor)
            {
                quality--;
            }

            return quality;
        }
    }

    /// <summary>
    /// Engine-independent state machine for one mixing vessel: load → mix → stop → settle → collect.
    /// </summary>
    public sealed class MixingProcess
    {
        private readonly List<ItemDefinition> _loaded = new List<ItemDefinition>();

        public MixingProcess(int capacity)
        {
            Capacity = Math.Max(1, capacity);
        }

        /// <summary>Raised when mixing stops: (quality, overheated).</summary>
        public event Action<ItemQuality, bool> Stopped;

        /// <summary>Raised when settling finishes and the product can be collected.</summary>
        public event Action Finished;

        public int Capacity { get; }
        public IReadOnlyList<ItemDefinition> Loaded => _loaded;
        public MixingPhase Phase { get; private set; } = MixingPhase.Idle;
        public RecipeDefinition ActiveRecipe { get; private set; }
        public float Progress { get; private set; }
        public float SettleRemaining { get; private set; }
        public ItemQuality ResultQuality { get; private set; }
        public bool Overheated { get; private set; }

        /// <summary>Upgrade bonus widening the premium window (progress units per side).</summary>
        public float WindowBonus { get; set; }

        /// <summary>Settle speed multiplier (upgrades). 2 = settles twice as fast.</summary>
        public float SettleSpeed { get; set; } = 1f;

        public bool CanLoad => Phase == MixingPhase.Idle && _loaded.Count < Capacity;

        public MixingSave Capture(string id)
        {
            var save=new MixingSave{Id=id,RecipeId=ActiveRecipe!=null?ActiveRecipe.Id:null,Phase=Phase,Progress=Progress,SettleRemaining=SettleRemaining,Quality=ResultQuality,Overheated=Overheated};
            foreach(var item in _loaded)save.Ingredients.Add(item.Id);
            return save;
        }
        public void Restore(MixingSave save,Func<string,ItemDefinition> lookup,IEnumerable<RecipeDefinition> recipes)
        {
            _loaded.Clear();ActiveRecipe=null;Phase=MixingPhase.Idle;Progress=0;SettleRemaining=0;Overheated=false;
            ResultQuality=ItemQuality.Standard;
            if(save==null)return;
            foreach(var id in save.Ingredients??new List<string>()){var item=lookup(id);if(item!=null && _loaded.Count<Capacity)_loaded.Add(item);}
            foreach(var recipe in recipes)if(recipe!=null && recipe.Id==save.RecipeId){ActiveRecipe=recipe;break;}
            if(ActiveRecipe==null || !ActiveRecipe.Check(_loaded).Matches){ActiveRecipe=null;return;}
            Phase=Enum.IsDefined(typeof(MixingPhase),save.Phase)?save.Phase:MixingPhase.Idle;
            Progress=float.IsNaN(save.Progress)?0:Mathf.Clamp(save.Progress,0,MixingRules.AutoStopAt);
            SettleRemaining=float.IsNaN(save.SettleRemaining)?0:Mathf.Clamp(save.SettleRemaining,0,ActiveRecipe.SettleDuration);
            ResultQuality=Enum.IsDefined(typeof(ItemQuality),save.Quality)?save.Quality:ItemQuality.Standard;Overheated=save.Overheated;
        }

        public bool Load(ItemDefinition item)
        {
            if (item == null || !CanLoad)
            {
                return false;
            }

            _loaded.Add(item);
            return true;
        }

        /// <summary>Removes and returns everything in the vessel (Idle only).</summary>
        public List<ItemDefinition> EmptyVessel()
        {
            if (Phase != MixingPhase.Idle)
            {
                return new List<ItemDefinition>();
            }

            var items = new List<ItemDefinition>(_loaded);
            _loaded.Clear();
            return items;
        }

        /// <summary>The recipe whose ingredients exactly match the vessel, or null.</summary>
        public RecipeDefinition FindMatch(IEnumerable<RecipeDefinition> recipes)
        {
            foreach (RecipeDefinition recipe in recipes)
            {
                if (recipe != null && recipe.MatchesIngredients(_loaded))
                {
                    return recipe;
                }
            }
            return null;
        }

        /// <summary>The recipe sharing most ingredients with the vessel (for hints), or null.</summary>
        public RecipeDefinition FindClosest(IEnumerable<RecipeDefinition> recipes)
        {
            RecipeDefinition best = null;
            int bestOverlap = 0;
            foreach (RecipeDefinition recipe in recipes)
            {
                int overlap = recipe != null ? recipe.Overlap(_loaded) : 0;
                if (overlap > bestOverlap)
                {
                    best = recipe;
                    bestOverlap = overlap;
                }
            }
            return best;
        }

        public bool TryStart(RecipeDefinition recipe)
        {
            if (Phase != MixingPhase.Idle || recipe == null || !recipe.Check(_loaded).Matches)
            {
                return false;
            }

            ActiveRecipe = recipe;
            Progress = 0f;
            Overheated = false;
            Phase = MixingPhase.Mixing;
            return true;
        }

        public void Tick(float deltaTime)
        {
            switch (Phase)
            {
                case MixingPhase.Mixing:
                    Progress += deltaTime / ActiveRecipe.MixDuration;
                    if (Progress >= MixingRules.AutoStopAt)
                    {
                        Progress = MixingRules.AutoStopAt;
                        StopMixing();
                    }
                    break;

                case MixingPhase.Settling:
                    SettleRemaining -= deltaTime;
                    if (SettleRemaining <= 0f)
                    {
                        SettleRemaining = 0f;
                        Phase = MixingPhase.Ready;
                        Finished?.Invoke();
                    }
                    break;
            }
        }

        public void StopMixing()
        {
            if (Phase != MixingPhase.Mixing)
            {
                return;
            }

            bool correctOrder = ActiveRecipe.IsCorrectOrder(_loaded);
            Overheated = MixingRules.IsOverheated(Progress);
            ResultQuality = MixingRules.Grade(Progress, correctOrder, WindowBonus);
            SettleRemaining = ActiveRecipe.SettleDuration / Mathf.Max(0.1f, SettleSpeed);
            Phase = MixingPhase.Settling;
            Stopped?.Invoke(ResultQuality, Overheated);

            if (SettleRemaining <= 0f)
            {
                Tick(0f);
            }
        }

        /// <summary>Takes the finished product out (Ready only). Returns false if nothing is ready.</summary>
        public bool Collect(out ProductDefinition product, out int count, out ItemQuality quality)
        {
            product = null;
            count = 0;
            quality = ItemQuality.Standard;
            if (Phase != MixingPhase.Ready)
            {
                return false;
            }

            product = ActiveRecipe.Output;
            count = ActiveRecipe.OutputCount;
            quality = ResultQuality;

            _loaded.Clear();
            ActiveRecipe = null;
            Progress = 0f;
            Phase = MixingPhase.Idle;
            return true;
        }
    }
}
