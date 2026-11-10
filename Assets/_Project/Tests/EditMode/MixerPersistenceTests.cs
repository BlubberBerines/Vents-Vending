using NUnit.Framework;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;
using VentsAndVending.Saving;

namespace VentsAndVending.Tests
{
    public sealed class MixerPersistenceTests
    {
        private ItemDefinition _ingredient;
        private ProductDefinition _product;
        private RecipeDefinition _recipe;
        [SetUp] public void Setup()
        {
            _ingredient=ScriptableObject.CreateInstance<ItemDefinition>();_ingredient.Configure("gel","Gel","",ItemCategory.Ingredient,10,1,Color.green);
            _product=ScriptableObject.CreateInstance<ProductDefinition>();_product.Configure("juice","Juice","",ItemCategory.Product,10,4,Color.green);
            _recipe=ScriptableObject.CreateInstance<RecipeDefinition>();_recipe.Configure("recipe",_product,1,CraftingEquipment.MixingStation,6,2,(_ingredient,1));
        }
        [TearDown] public void Cleanup(){Object.DestroyImmediate(_ingredient);Object.DestroyImmediate(_product);Object.DestroyImmediate(_recipe);}
        private MixingProcess RoundTrip(MixingProcess source)
        {
            var save=new SaveData{SceneName="Station_Concourse_Ring"};save.Mixers.Add(source.Capture("den"));
            Assert.IsTrue(SaveCodec.TryParse(SaveCodec.ToJson(save),out var parsed,out _));
            var restored=new MixingProcess(6);restored.Restore(parsed.Mixers[0],id=>id==_ingredient.Id?_ingredient:null,new[]{_recipe});return restored;
        }
        [Test] public void LoadedIdleIngredientsSurvive()
        {var process=new MixingProcess(6);process.Load(_ingredient);var restored=RoundTrip(process);Assert.AreEqual(MixingPhase.Idle,restored.Phase);Assert.AreEqual(1,restored.Loaded.Count);}
        [Test] public void InFlightProgressSurvives()
        {var process=new MixingProcess(6);process.Load(_ingredient);process.TryStart(_recipe);process.Tick(2);var restored=RoundTrip(process);Assert.AreEqual(MixingPhase.Mixing,restored.Phase);Assert.AreEqual(process.Progress,restored.Progress,.0001);}
        [Test] public void ReadyBatchCanBeCollectedExactlyOnce()
        {var process=new MixingProcess(6);process.Load(_ingredient);process.TryStart(_recipe);process.Tick(5.5f);process.StopMixing();process.Tick(3);var restored=RoundTrip(process);Assert.IsTrue(restored.Collect(out var product,out int count,out _));Assert.AreSame(_product,product);Assert.AreEqual(1,count);Assert.IsFalse(restored.Collect(out _,out _,out _));}
        [Test] public void UnknownRecipeFallsBackWithoutLosingKnownIngredients()
        {var process=new MixingProcess(6);process.Load(_ingredient);process.TryStart(_recipe);var save=process.Capture("den");save.RecipeId="unknown";var restored=new MixingProcess(6);restored.Restore(save,id=>_ingredient,new[]{_recipe});Assert.AreEqual(MixingPhase.Idle,restored.Phase);Assert.AreEqual(1,restored.Loaded.Count);}
        [Test] public void OldSavesMigrateWithEmptyMixerState()
        {Assert.IsTrue(SaveCodec.TryParse("{\"Version\":4,\"Credits\":20}",out var save,out _));Assert.NotNull(save.Mixers);Assert.AreEqual(0,save.Mixers.Count);}
        [Test] public void LoadingWithoutSavedMixerClearsCurrentBatch()
        {var process=new MixingProcess(6);process.Load(_ingredient);process.TryStart(_recipe);process.Tick(2);process.Restore(null,id=>_ingredient,new[]{_recipe});Assert.AreEqual(MixingPhase.Idle,process.Phase);Assert.IsEmpty(process.Loaded);Assert.IsNull(process.ActiveRecipe);}
        [Test] public void FastMatchingAgreesWithDetailedRecipeCheck()
        {var loaded=new System.Collections.Generic.List<ItemDefinition>();Assert.AreEqual(_recipe.Check(loaded).Matches,_recipe.MatchesIngredients(loaded));loaded.Add(_ingredient);Assert.IsTrue(_recipe.MatchesIngredients(loaded));loaded.Add(_ingredient);Assert.IsFalse(_recipe.MatchesIngredients(loaded));}
    }
}
