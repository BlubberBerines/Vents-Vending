using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.Orders;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildStationDenGameplay()
        {
            var den=_stationFrame.Find("L3_01_Den");
            var dressing=den.Find("Den_Dressing");
            var desk=dressing.Cast<Transform>().First(t=>t.name.StartsWith("PROP_Desk_Terminal"));
            desk.gameObject.AddComponent<VentNetTerminal>();
            var display=new GameObject("VentNet Live Screen");display.transform.SetParent(den,false);
            display.transform.localPosition=new Vector3(-1.2f,1.55f,-5.32f);
            display.transform.localRotation=Quaternion.Euler(0,180,0);
            var text=display.AddComponent<TextMeshPro>();text.fontSize=.65f;text.alignment=TextAlignmentOptions.Center;
            text.textWrappingMode=TextWrappingModes.NoWrap;
            text.rectTransform.sizeDelta=new Vector2(.85f,.38f);
            desk.GetComponent<VentNetTerminal>().Configure(text);
            // Use the existing, fully wired machine prefab rather than another decorative bench.
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{MachinePrefabFolder}/MixingStation.prefab");
            if(prefab==null)throw new InvalidOperationException("Missing MixingStation prefab.");
            var mixer=(GameObject)PrefabUtility.InstantiatePrefab(prefab,den);
            mixer.name="Den Mixing Table";mixer.transform.localPosition=new Vector3(1.05f,0,-5.45f);
            mixer.transform.localRotation=Quaternion.identity;
            var recipes=AssetDatabase.FindAssets("t:RecipeDefinition",new[]{RecipeFolder})
                .Select(id=>AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
            if(recipes.Length==0 || recipes.Any(r=>r.Output==null || r.Ingredients.Any(i=>i.Item==null)))
                throw new InvalidOperationException("Missing recipe output or ingredient data.");
            mixer.GetComponent<MixingStation>().ConfigureRecipes(recipes.Where(r=>r.Equipment==CraftingEquipment.MixingStation));
            var cupboard=dressing.Cast<Transform>().First(t=>t.name.StartsWith("PROP_Locker_Row"));
            cupboard.name="Den Ingredient Storage Cupboard";
            var inventory=cupboard.gameObject.AddComponent<InventoryComponent>();
            inventory.Configure("Ingredient cupboard",36,saveId:"hideout.locker");
            foreach(var item in recipes.SelectMany(r=>r.Ingredients).Select(i=>i.Item).Distinct())
                inventory.AddStartingItem(item,30);
            cupboard.gameObject.AddComponent<StorageContainer>();
            StationLabel(den,"MIXING / VENTNET\nINGREDIENTS IN CUPBOARD",new Vector3(0,2.3f,-6.1f),0,.027f);
        }
    }
}
