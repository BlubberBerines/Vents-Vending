using System.Linq;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.Tunnels;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private const string StationLocalLightId="STATION_DEN_APPROACH_LIGHTS";
        private static void BuildStationLocalControls()
        {
            var den=_stationFrame.Find("L3_01_Den");
            foreach(var door in den.GetComponentsInChildren<SlidingDoor>())
            {
                var settings=new SerializedObject(door);settings.FindProperty("_automatic").boolValue=false;
                settings.FindProperty("_holdOpenSeconds").floatValue=4;settings.ApplyModifiedPropertiesWithoutUndo();
                StationDoorMotion(door);
            }
            StationSideDoor("Power Distribution Door",new Vector3(-4,0,0),90);
            StationSideDoor("Supply Store Door",new Vector3(4,0,-4),90);
            var lights=Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>
            {
                var p=_stationFrame.InverseTransformPoint(l.transform.position);
                return !l.transform.IsChildOf(den) && p.y>=0 && p.y<3 && p.x>=-10 && p.x<=10 && p.z>=-6 && p.z<=16;
            }).ToArray();
            RingGroup("Den Approach Lighting Circuit",_stationFrame).gameObject.AddComponent<LightCircuit>()
                .Configure(StationLocalLightId,lights,GameObject.Find("Maintenance Architectural Finish").GetComponentsInChildren<Renderer>().Where(r=>
                {var p=_stationFrame.InverseTransformPoint(r.bounds.center);return p.x>=-10 && p.x<=10 && p.z>=-6 && p.z<=16;}).ToArray());
            // Safety glow and the Den's own light are deliberately outside this circuit.
            StationLight(_stationFrame,"Den Approach Emergency Glow",new Vector3(0,.35f,-4.8f),new Color(1,.12f,.05f),7,.5f);
            foreach(bool inside in new[]{true,false})
            {
                var mount=RingGroup(inside?"Den Local Lights Switch":"Hub Local Lights Switch",den);
                mount.localPosition=new Vector3(-1.65f,1.25f,inside?-.47f:-.15f);
                mount.localRotation=Quaternion.Euler(0,inside?180:0,0);
                var box=mount.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(.35f,.5f,.15f);
                Part("Switch Housing",mount,Vector3.zero,new Vector3(.3f,.45f,.1f),StationMetal);
                var lamp=Part("Switch Indicator",mount,new Vector3(0,0,.07f),new Vector3(.12f,.12f,.04f),ArtLibrary.Get("M_Env_TrimOrange"));
                mount.gameObject.AddComponent<LightSwitch>().Configure(StationLocalLightId,lamp.GetComponent<Renderer>());
            }
            StationLabel(den,"LOCAL MAINTENANCE LIGHTS",new Vector3(-1.65f,1.7f,-.48f),0,.012f);
        }

        private static void StationSideDoor(string name,Vector3 position,float yaw)
        {
            var root=RingGroup(name,_stationFrame);root.localPosition=position;root.localRotation=Quaternion.Euler(0,yaw,0);
            var leaf=StationLocalBox(root,"Sliding Door Leaf",new Vector3(0,1.15f,0),new Vector3(2,2.3f,.12f),StationWall);
            Part("Door Hazard Stripe",leaf.transform,new Vector3(0,.05f,.56f),new Vector3(.85f,.04f,.02f),ArtLibrary.Get("M_Env_TrimOrange"));
            var door=root.gameObject.AddComponent<SlidingDoor>();door.Configure(new[]{leaf.transform},new[]{Vector3.right*2.1f});door.ConfigureManual(4);
            StationDoorMotion(door);
            foreach(float x in new[]{-1.08f,1.08f})Part("Door Jamb",root,new Vector3(x,1.15f,0),new Vector3(.14f,2.3f,.26f),ArtLibrary.Get("M_Env_FrameDark"));
            Part("Door Track",root,new Vector3(0,2.35f,0),new Vector3(2.3f,.16f,.3f),StationMetal);
        }

        private static void StationDoorMotion(SlidingDoor door)
        {
            var settings=new SerializedObject(door);settings.FindProperty("_openSeconds").floatValue=.85f;
            settings.FindProperty("_closeSeconds").floatValue=1.05f;settings.FindProperty("_unsealDepth").floatValue=.035f;
            settings.FindProperty("_openSound").objectReferenceValue=ProceduralAudio.Clunk;
            settings.FindProperty("_closeSound").objectReferenceValue=ProceduralAudio.Clunk;
            settings.FindProperty("_latchSound").objectReferenceValue=ProceduralAudio.Clunk;settings.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildStationFanPuzzle()
        {
            var branch=RingGroup("R04_R17_Optional_Fan_Branch",_stationFrame);
            StationTube(branch,"Enclosed Fan Approach",new[]{new Vector3(-10,0,0),new Vector3(-14,0,0),new Vector3(-14,0,-5)},3,3,false);
            // This lower-roof cache fits below, rather than through, the existing emergency stairs.
            var cells=new System.Collections.Generic.HashSet<Vector2Int>();StationRect(cells,-16,-12,-10,-5);
            StationCellSpace(branch,cells,0,2.2f,null,null,(p,n)=>p.z> -5.1f && n.z>0 && Mathf.Abs(p.x+14)<1.5f);
            StationLocalBox(branch,"Cache Entrance Header",new Vector3(-14,2.6f,-5),new Vector3(3,.8f,.14f),StationWall);
            var fanRoot=RingGroup("FAN_17 Extraction Fan",branch);fanRoot.localPosition=new Vector3(-12,1.6f,0);fanRoot.localRotation=Quaternion.Euler(0,90,0);
            var rotor=RingGroup("Rotor",fanRoot);
            for(int i=0;i<3;i++)
            {
                var arm=RingGroup("Fan Blade Arm",rotor);arm.localRotation=Quaternion.Euler(0,0,i*120);
                StationLocalBox(arm,"Fan Blade",new Vector3(0,.8f,0),new Vector3(.25f,1.2f,.08f),StationMetal);
            }
            StationLocalBox(rotor,"Fan Hub",Vector3.zero,new Vector3(.4f,.4f,.24f),StationWall);
            foreach(float x in new[]{-1.4f,1.4f})StationLocalBox(fanRoot,"Fan Housing",new Vector3(x,-.1f,0),new Vector3(.2f,3,.35f),StationMetal);
            StationLocalBox(fanRoot,"Fan Housing Top",new Vector3(0,1.3f,0),new Vector3(3,.2f,.35f),StationMetal);
            var blocker=fanRoot.gameObject.AddComponent<BoxCollider>();blocker.center=new Vector3(0,-.1f,0);blocker.size=new Vector3(3,3,.5f);
            var lamp=Part("Fan Warning Lamp",fanRoot,new Vector3(1.25f,1,.25f),Vector3.one*.14f,GetOrCreateMaterial("Station_Fan_Warning",Color.red,.4f,Color.red*2));
            StationLight(fanRoot,"Fan Warning Light",new Vector3(1.1f,.8f,.3f),Color.red,5,3);
            var fan=fanRoot.gameObject.AddComponent<FanHazard>();var so=new SerializedObject(fan);
            so.FindProperty("_rotor").objectReferenceValue=rotor;so.FindProperty("_blocker").objectReferenceValue=blocker;
            SetList(so.FindProperty("_warningLamps"),lamp.GetComponent<Renderer>());
            so.FindProperty("_warningLight").objectReferenceValue=fanRoot.GetComponentInChildren<Light>();
            var hum=fanRoot.gameObject.AddComponent<AudioSource>();hum.clip=ProceduralAudio.BigFan;hum.loop=true;hum.playOnAwake=false;hum.spatialBlend=1;hum.minDistance=2;hum.maxDistance=20;
            so.FindProperty("_hum").objectReferenceValue=hum;so.FindProperty("_humVolume").floatValue=.45f;so.ApplyModifiedPropertiesWithoutUndo();
            // Clearly labelled cable leads from the power-room breaker to the matching fan housing.
            var power=GetOrCreateMaterial("Station_Fan_PowerCable",new Color(1,.4f,.04f),.3f,new Color(.3f,.06f,0));
            StationLocalBox(branch,"FAN 17 Orange Power Cable",new Vector3(-9.5f,2.5f,1.35f),new Vector3(5,.06f,.06f),power);
            StationLocalBox(branch,"FAN 17 Cable Bend",new Vector3(-7,2.5f,2.075f),new Vector3(.06f,.06f,1.45f),power);
            StationLocalBox(branch,"FAN 17 Breaker Cable Drop",new Vector3(-7,1.9f,2.8f),new Vector3(.06f,1.2f,.06f),power);
            var breakerMount=RingGroup("Fan Breaker Mount",_stationFrame);breakerMount.localPosition=new Vector3(-7,1.3f,2.8f);breakerMount.localRotation=Quaternion.Euler(0,90,0);
            BuildBreaker(breakerMount,Vector3.zero,TunnelIds.FanBreaker,"FAN 17 EXTRACTION",false,null,StationMetal,StationWall,power,power);
            var breaker=breakerMount.GetChild(0);breaker.localPosition=Vector3.zero;breaker.localRotation=Quaternion.Euler(0,180,0);
            SetClip(breaker.GetComponent<BreakerSwitch>(),"_clunk",ProceduralAudio.Clunk);
            StationLabel(_stationFrame,"BRK_FAN_04 / FAN 17\nISOLATE -> WAIT FOR AMBER -> CRAWL",new Vector3(-7,2.05f,2.82f),0,.02f);
            StationLabel(branch,"FAN 17 / RUNNING = NO ACCESS\nBREAKER IN 16 POWER DISTRIBUTION",new Vector3(-10.2f,2.25f,0),-90,.022f);
            StationLabel(branch,"17 EXTRACTION SERVICE CACHE\nOPTIONAL BRANCH / RETURN TO DEN",new Vector3(-14,1.8f,-9.8f),180,.025f);
            StationLight(branch,"Fan Approach Lamp",new Vector3(-10.7f,2.6f,0),new Color(1,.7f,.35f),6,4);
            StationLight(branch,"Fan Cache Lamp",new Vector3(-14,1.9f,-7.5f),new Color(.35f,.7f,1),7,6);
            var cache=Prop(branch,Props+"/PROP_Locker_Row.prefab",-15.5f,0,-8.8f,-90);
            cache.name="Fan Service Cache";var inventory=cache.AddComponent<InventoryComponent>();inventory.Configure("Fan service cache",12,saveId:"station.fan17.cache");
            foreach(var id in new[]{"Item_GlarbExtract","Item_LuminaGel","Item_StabiliserGel"})inventory.AddStartingItem(AssetDatabase.LoadAssetAtPath<ItemDefinition>($"{ItemFolder}/{id}.asset"),5);
            cache.AddComponent<VentsAndVending.Interaction.StorageContainer>();
            foreach(var entry in new[]{(TunnelIds.FanJunction,new Vector3(-10.8f,1.4f,0),new Vector3(1.4f,2.8f,2.8f)),(TunnelIds.FanRoom,new Vector3(-14,1.1f,-7.5f),new Vector3(3.8f,2.1f,4.8f))})
            {
                var volume=RingGroup("Discovery "+entry.Item1,branch);volume.localPosition=entry.Item2;
                volume.gameObject.AddComponent<BoxCollider>().size=entry.Item3;volume.gameObject.AddComponent<RoomVolume>().Configure(AssetDatabase.LoadAssetAtPath<VentRoomDefinition>($"Assets/_Project/ScriptableObjects/Tunnels/StationRing/{entry.Item1}.asset"));
            }
        }
    }
}
