using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Interaction;
using VentsAndVending.Inventory;
using VentsAndVending.Tunnels;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildStationResourceLoop()
        {
            var raw=StationResourceItem("RawCoreCrystal","raw_core_crystal","Raw Core Crystal",
                "Unstable mineral growth from the corrupted core. Refine 3 into 2 Lumina Gel at the Den refinery. Not a mixer ingredient.",ItemCategory.Misc,20,new Color(.2f,.95f,1));
            var axe=StationResourceItem("MiningPickaxe","mining_pickaxe","Mining Pickaxe",
                "Reusable salvage pickaxe. Carry it and press E three times on a core crystal vein. Spare tools are beside the core entrance.",ItemCategory.Equipment,1,new Color(.9f,.6f,.12f));
            var crystal=GetOrCreateMaterial("Core_Crystal",new Color(.07f,.45f,.48f),.7f,new Color(.1f,.85f,1)*1.4f);
            var corrupt=GetOrCreateMaterial("Core_Corruption",new Color(.30f,.08f,.25f),.4f,new Color(.65f,.05f,.3f)*.65f);
            StationResourcePickup(raw,crystal,false);StationResourcePickup(axe,StationMetal,true);
            var branch=RingGroup("L3_Corrupted_Core_Recovery",_stationFrame);
            var route=new[]{new Vector3(10,0,-4),new Vector3(13,0,-4),new Vector3(17,-.9f,-7),new Vector3(17,-1.8f,-13),new Vector3(24,-1.8f,-13),new Vector3(24,-1.8f,-18)};
            StationTube(branch,"Winding Core Access",route,2.6f,2.6f,false);
            StationLocalBox(branch,"Supply Exit Header",new Vector3(10,2.8f,-4),new Vector3(.16f,.4f,2.6f),StationWall);
            StationSideDoor("Core Recovery Access Door",new Vector3(10,0,-4),90);
            StationLabel(_stationFrame,"MATERIAL RECOVERY\nCORRUPTED CORE ->",new Vector3(9.8f,2.35f,-4),90,.022f);
            var cells=new HashSet<Vector2Int>();StationRect(cells,19,29,-28,-18);
            StationCellSpace(branch,cells,-1.8f,5,null,null,(p,n)=>n.z>0 && p.z> -18.1f && Mathf.Abs(p.x-24)<1.05f);
            StationLocalBox(branch,"Core Entry Crown",new Vector3(24,2,-18),new Vector3(2.6f,2.4f,.16f),StationWall);
            // Low pipes and inset red guides lead around two blind bends, away from the Den.
            for(int i=0;i<route.Length-1;i++)
            {
                var a=route[i];var b=route[i+1];var side=Vector3.Cross(Vector3.up,(b-a).normalized).normalized;
                MaintenancePipe(branch,a+side*1.05f+Vector3.up*2.25f,b+side*1.05f+Vector3.up*2.25f,.065f,StationMetal);
                Part("Recovery Route Guide",branch,(a+b)*.5f+side*1.20f+Vector3.up*.25f,new Vector3(.12f,.14f,.12f),corrupt);
            }
            StationLabel(branch,"CORE RECOVERY\nPICKAXE BY THE ENTRANCE\n3 STRIKES = 3 RAW CRYSTALS",new Vector3(24,.35f,-27.8f),180,.035f);
            var core=RingGroup("Corrupted Auxiliary Power Core",branch);core.localPosition=new Vector3(24,-1.8f,-24);
            StationLocalBox(core,"Core Foundation",new Vector3(0,.18f,0),new Vector3(2.8f,.36f,2.8f),StationMetal);
            Part("Reactor Vessel",core,new Vector3(0,1.65f,0),new Vector3(1.9f,1.5f,1.9f),StationMetal,PrimitiveType.Cylinder);
            var obstacle=core.gameObject.AddComponent<BoxCollider>();obstacle.center=new Vector3(0,1.7f,0);obstacle.size=new Vector3(2.6f,3.4f,2.6f);
            for(int i=0;i<6;i++)
            {
                float angle=i*60*Mathf.Deg2Rad;var radial=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                MaintenancePipe(core,radial*1.08f+Vector3.up*.4f,radial*1.08f+Vector3.up*3.5f,.10f,corrupt);
                var growth=RingGroup("Fused Core Growth",core);growth.localPosition=radial*1.05f+Vector3.up*(.8f+(i%3)*.65f);growth.localRotation=Quaternion.Euler(0,-i*60,35);
                StationCrystal(growth,crystal);
            }
            StationLight(core,"Corrupted Core Glow",new Vector3(0,2.8f,0),new Color(.8f,.1f,.45f),9,5);
            StationLight(branch,"Recovery Entrance Lamp",new Vector3(24,.5f,-19),new Color(1,.7f,.35f),8,7);
            var positions=new[]{new Vector3(21,-1.8f,-23),new Vector3(21,-1.8f,-26),new Vector3(24,-1.8f,-26.8f),new Vector3(27,-1.8f,-25.5f),new Vector3(27,-1.8f,-22),new Vector3(24,-1.8f,-21)};
            for(int i=0;i<positions.Length;i++)
            {
                var vein=RingGroup($"Mineable Core Crystal {i+1}",branch);vein.localPosition=positions[i];
                Part("Mineral Crust",vein,new Vector3(0,.08f,0),new Vector3(1.15f,.16f,1.05f),corrupt);
                var growth=RingGroup("Harvestable Growth",vein);
                for(int j=0;j<3;j++)
                {
                    var shard=RingGroup("Crystal Shard",growth);shard.localPosition=new Vector3((j-1)*.3f,0,(j%2)*.25f);shard.localRotation=Quaternion.Euler(0,j*55,(j-1)*14);shard.localScale=Vector3.one*(j==1?1.15f:.75f);
                    StationCrystal(shard,crystal);
                }
                var box=vein.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.55f,0);box.size=new Vector3(1.2f,1.1f,1.1f);
                vein.gameObject.AddComponent<CrystalVein>().Configure($"CORE_CRYSTAL_{i+1:00}",axe,raw,growth,ProceduralAudio.Clunk);
                StationLight(vein,"Crystal Pool",new Vector3(0,.8f,0),new Color(.1f,.7f,.8f),3,.8f);
            }
            var rack=RingGroup("Spare Mining Pickaxe",branch);rack.localPosition=new Vector3(22,-1.8f,-19.8f);
            Part("Tool Rest",rack,new Vector3(0,.25f,0),new Vector3(.9f,.5f,.55f),StationMetal);
            var tool=RingGroup("Loaner Pickaxe",rack);tool.localPosition=new Vector3(0,.55f,0);tool.localRotation=Quaternion.Euler(75,0,-15);StationPickaxe(tool);
            var rackBox=rack.gameObject.AddComponent<BoxCollider>();rackBox.center=new Vector3(0,.55f,0);rackBox.size=new Vector3(1,1.1f,.8f);rack.gameObject.AddComponent<MiningToolRack>().Configure(axe);
            StationLabel(branch,"SPARE PICKAXE / E",new Vector3(22,.1f,-19.5f),180,.025f);
            BuildDenCrystalRefinery(raw);
            BuildCoreDiscovery(branch,route);
            StationLabel(_stationFrame.Find("L3_01_Den"),"FREE MATERIALS: SUPPLY STORE -> CORE\nMINE CRYSTALS / REFINE HERE / MIX / DELIVER\nOR BUY INGREDIENTS: PC > SUPPLIES",new Vector3(0,2.65f,-6.05f),0,.017f);
        }

        private static ItemDefinition StationResourceItem(string file,string id,string display,string description,ItemCategory category,int stack,Color color)
        {
            string path=$"{ItemFolder}/Item_{file}.asset";var item=AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
            if(item==null){item=ScriptableObject.CreateInstance<ItemDefinition>();AssetDatabase.CreateAsset(item,path);}
            item.Configure(id,display,description,category,stack,0,color);EditorUtility.SetDirty(item);return item;
        }
        private static void StationPickaxe(Transform parent)
        {
            Part("Pickaxe Handle",parent,new Vector3(0,.5f,0),new Vector3(.055f,.5f,.055f),ArtLibrary.Get("M_WorkshopOrange"),PrimitiveType.Cylinder);
            Part("Forged Pick Head",parent,new Vector3(0,.94f,0),new Vector3(.72f,.11f,.12f),StationMetal).transform.localRotation=Quaternion.Euler(0,0,-12);
            Part("Pick Grip",parent,new Vector3(0,.22f,0),new Vector3(.085f,.18f,.085f),ArtLibrary.Get("M_Env_FrameDark"),PrimitiveType.Cylinder);
        }
        private static void StationCrystal(Transform parent,Material material)
        {
            var vertices=new List<Vector3>{Vector3.up*1.1f,Vector3.zero};
            for(int i=0;i<6;i++){float a=i*60*Mathf.Deg2Rad;vertices.Add(new Vector3(Mathf.Cos(a)*.26f,.25f,Mathf.Sin(a)*.26f));}
            var triangles=new List<int>();for(int i=0;i<6;i++){int a=2+i,b=2+(i+1)%6;triangles.AddRange(new[]{a,0,b,a,b,1});}
            var go=StationMeshObject(parent,"Faceted Crystal",vertices.ToArray(),triangles.ToArray(),material);
            // Interaction/physics is owned by the enclosing node or pickup.
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
        }
        private static void StationResourcePickup(ItemDefinition item,Material material,bool pickaxe)
        {
            var go=new GameObject("Pickup_"+item.Id);
            if(pickaxe)StationPickaxe(go.transform);else {go.transform.localScale=Vector3.one*.3f;StationCrystal(go.transform,material);}
            var box=go.AddComponent<BoxCollider>();box.center=new Vector3(0,.5f,0);box.size=new Vector3(pickaxe?.75f:.6f,1.1f,pickaxe?.2f:.6f);
            go.AddComponent<Rigidbody>().mass=.6f;go.AddComponent<PickupItem>().Configure(item,1);
            item.SetWorldPrefab(PrefabUtility.SaveAsPrefabAsset(go,$"{ItemPrefabFolder}/Pickup_{item.Id}.prefab"));EditorUtility.SetDirty(item);Object.DestroyImmediate(go);
        }
        private static void BuildDenCrystalRefinery(ItemDefinition raw)
        {
            var root=RingGroup("Den Crystal Refinery",_stationFrame.Find("L3_01_Den"));root.localPosition=new Vector3(1.05f,0,-3.2f);root.localRotation=Quaternion.Euler(0,-90,0);
            var body=StationLocalBox(root,"Refinery Cabinet",new Vector3(0,.55f,0),new Vector3(.8f,1.1f,.9f),StationMetal);
            Part("Refining Vessel",root,new Vector3(0,1.24f,0),new Vector3(.5f,.22f,.5f),ArtLibrary.Get("M_BrushedSilver"),PrimitiveType.Cylinder);
            var rotor=RingGroup("Refinery Rotor",root);rotor.localPosition=new Vector3(0,1.47f,0);Part("Rotor Crossbar",rotor,Vector3.zero,new Vector3(.58f,.05f,.10f),StationMetal);
            StationLabel(root,"CRYSTAL REFINERY\n3 RAW -> 2 LUMINA GEL",new Vector3(0,1.75f,.5f),180,.014f);
            var status=root.GetComponentsInChildren<TextMesh>().Single();
            var output=AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemFolder}/Item_LuminaGel.asset");
            root.gameObject.AddComponent<CrystalRefinery>().Configure("den.crystal_refinery",raw,output,status,rotor,ProceduralAudio.Clunk);
            Part("Refinery Input Slot",root,new Vector3(0,.8f,.47f),new Vector3(.55f,.16f,.05f),ArtLibrary.Get("M_Env_FrameDark"));
        }
        private static void BuildCoreDiscovery(Transform branch,Vector3[] route)
        {
            const string folder="Assets/_Project/ScriptableObjects/Tunnels/StationRing";
            VentRoomDefinition Node(string id,string display,string[] links)
            {
                string path=$"{folder}/{id}.asset";var def=AssetDatabase.LoadAssetAtPath<VentRoomDefinition>(path);
                if(def==null){def=ScriptableObject.CreateInstance<VentRoomDefinition>();AssetDatabase.CreateAsset(def,path);}
                def.Configure(id,display,"station_ring",3,links.Select(l=>new VentRoomDefinition.Connection{DestinationRoomId=l}).ToArray());EditorUtility.SetDirty(def);return def;
            }
            var supply=Node("SUPPLY_STORE","Supply Store / Material Recovery",new[]{TunnelIds.ServiceJunction,"CORE_RECOVERY"});
            var core=Node("CORE_RECOVERY","Corrupted Auxiliary Core",new[]{"SUPPLY_STORE"});
            var junction=AssetDatabase.LoadAssetAtPath<VentRoomDefinition>($"{folder}/{TunnelIds.ServiceJunction}.asset");
            junction.Configure(junction.RoomId,junction.DisplayName,junction.DistrictId,junction.Plane,junction.Connections.Where(c=>c.DestinationRoomId!="SUPPLY_STORE").Append(new VentRoomDefinition.Connection{DestinationRoomId="SUPPLY_STORE"}).ToArray());EditorUtility.SetDirty(junction);
            void Volume(Transform parent,string name,Vector3 position,Vector3 size,VentRoomDefinition room)
            {var v=RingGroup(name,parent);v.localPosition=position;var b=v.gameObject.AddComponent<BoxCollider>();b.size=size;b.isTrigger=true;v.gameObject.AddComponent<RoomVolume>().Configure(room);}
            Volume(_stationFrame,"Supply Store Discovery",new Vector3(7,1.3f,-4),new Vector3(5.8f,2.6f,3.8f),supply);
            Volume(branch,"Core Chamber Discovery",new Vector3(24,.5f,-23),new Vector3(9.8f,4.6f,9.8f),core);
            for(int i=0;i<route.Length-1;i++)
            {
                var delta=route[i+1]-route[i];var v=RingGroup("Recovery Passage Discovery",branch);v.localPosition=(route[i]+route[i+1])*.5f+Vector3.up*1.3f;v.localRotation=Quaternion.LookRotation(delta);
                var b=v.gameObject.AddComponent<BoxCollider>();b.isTrigger=true;b.size=new Vector3(2.5f,2.5f,delta.magnitude+.1f);v.gameObject.AddComponent<RoomVolume>().Configure(core);
            }
            Object.FindAnyObjectByType<TunnelManager>().SetRooms(AssetDatabase.FindAssets("t:VentRoomDefinition",new[]{folder}).Select(id=>AssetDatabase.LoadAssetAtPath<VentRoomDefinition>(AssetDatabase.GUIDToAssetPath(id))));
        }
    }
}
