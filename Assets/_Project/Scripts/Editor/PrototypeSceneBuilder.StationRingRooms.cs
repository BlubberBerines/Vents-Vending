using System.Linq;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Saving;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        // Layout positions are deliberately independent of persistent room identities.
        private static readonly float[] StationAccessibleRoomAngles={39,58,219,242,337};
        private static readonly float[] StationRoomVentAngles={58-Mathf.Atan2(4.2f,71.2f)*Mathf.Rad2Deg,242-Mathf.Atan2(4.2f,71.2f)*Mathf.Rad2Deg};
        private static void CreateStationSaveSystem()
        {
            var database=AssetDatabase.LoadAssetAtPath<GameDatabase>(DatabasePath);
            if(database==null){database=ScriptableObject.CreateInstance<GameDatabase>();AssetDatabase.CreateAsset(database,DatabasePath);}
            var items=AssetDatabase.FindAssets("t:ItemDefinition",new[]{"Assets/_Project/ScriptableObjects"})
                .Select(id=>AssetDatabase.LoadAssetAtPath<VentsAndVending.Inventory.ItemDefinition>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
            var upgrades=AssetDatabase.FindAssets("t:UpgradeDefinition",new[]{"Assets/_Project/ScriptableObjects"})
                .Select(id=>AssetDatabase.LoadAssetAtPath<VentsAndVending.Progression.UpgradeDefinition>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
            database.Configure(items,upgrades);EditorUtility.SetDirty(database);
            new GameObject("Station Progression").AddComponent<VentsAndVending.Progression.ProgressionManager>().SetCatalogue(upgrades);
            new GameObject("Station Save System").AddComponent<SaveManager>().SetDatabase(database);
            SetBuildScenes();
        }

        private static void BuildStationRingRooms()
        {
            var rust=StationFineFinish("Station_Rusted_Bulkhead","M_RustyMetal",new Color(.27f,.20f,.14f),.8f,.34f);
            var blast=RingGroup("Den Culdesac Blast Door",_stationFrame);blast.localPosition=new Vector3(0,0,8);
            var leaves=new Transform[2];
            for(int i=0;i<2;i++)
            {
                float sign=i==0?-1:1;
                var leaf=StationLocalBox(blast,"Armoured Blast Leaf",new Vector3(sign*.675f,1.25f,0),new Vector3(1.35f,2.5f,.24f),rust);leaves[i]=leaf.transform;
                foreach(float z in new[]{-.57f,.57f})
                {
                    Part("Raised Armour Panel",leaf.transform,new Vector3(0,.04f,z),new Vector3(.78f,.7f,.12f),rust);
                    Part("Caution Stripe",leaf.transform,new Vector3(0,-.3f,z),new Vector3(.9f,.065f,.13f),ArtLibrary.Get("M_Env_HazardStripe"));
                }
            }
            foreach(float x in new[]{-1.42f,1.42f})StationLocalBox(blast,"Blast Jamb",new Vector3(x,1.25f,0),new Vector3(.16f,2.5f,.42f),StationMetal);
            StationLocalBox(blast,"Blast Header",new Vector3(0,2.75f,0),new Vector3(3,.5f,.42f),rust);
            var blastDoor=blast.gameObject.AddComponent<SlidingDoor>();blastDoor.Configure(leaves,new[]{Vector3.left*1.45f,Vector3.right*1.45f});blastDoor.ConfigureManual(5);StationDoorMotion(blastDoor);
            var settings=new SerializedObject(blastDoor);settings.FindProperty("_openSeconds").floatValue=1.25f;settings.FindProperty("_closeSeconds").floatValue=1.5f;settings.FindProperty("_doorwaySize").vector3Value=new Vector3(2.7f,2.5f,1.2f);settings.ApplyModifiedPropertiesWithoutUndo();
            StationLabel(_stationFrame,"MAINTENANCE RING / PRESS E\nKEEP ACCESS CLEAR",new Vector3(0,2.65f,7.75f),0,.018f);

            float[] angles={39,58,126,193,219,242,254,337};
            int[] identities={45,90,135,180,225,270,315,345};
            string[] names={"PIPEFITTER BREAK ROOM","PUMP SERVICE","SEALED RELAY VAULT","COLLAPSED ACCESS","STEAM SERVICE","TOOL STORE","DECOMMISSIONED","MAINTENANCE REST BAY"};
            for(int i=0;i<angles.Length;i++)
            {
                var room=RingGroup($"Ring Room {angles[i]:000} {names[i]}",_stationFrame.parent);
                room.SetPositionAndRotation(RingPoint(70,angles[i],0),Quaternion.Euler(0,90-angles[i],0));
                bool usable=StationAccessibleRoomAngles.Contains(angles[i]);
                if(usable)
                {
                    string id=$"MAINT_ROOM_{identities[i]:000}";
                    string dataPath=$"Assets/_Project/ScriptableObjects/Tunnels/StationRing/{id}.asset";
                    var definition=AssetDatabase.LoadAssetAtPath<VentsAndVending.Tunnels.VentRoomDefinition>(dataPath);
                    if(definition==null){definition=ScriptableObject.CreateInstance<VentsAndVending.Tunnels.VentRoomDefinition>();AssetDatabase.CreateAsset(definition,dataPath);}
                    var connections=new System.Collections.Generic.List<VentsAndVending.Tunnels.VentRoomDefinition.Connection>{new(){DestinationRoomId="MAINT_RING",GateId=i==4?"STEAM_RING_225":null,GateOpensWhenOff=i==4}};
                    if(i==1||i==5)connections.Add(new(){DestinationRoomId="AC_RING"});
                    definition.Configure(id,names[i],"station_ring",3,connections.ToArray());EditorUtility.SetDirty(definition);
                    var volume=RingGroup("Room Discovery",room);volume.localPosition=new Vector3(0,1.15f,-4);
                    var trigger=volume.gameObject.AddComponent<BoxCollider>();trigger.size=new Vector3(5.8f,2.3f,5.8f);trigger.isTrigger=true;volume.gameObject.AddComponent<VentsAndVending.Tunnels.RoomVolume>().Configure(definition);
                    // Build at origin because prop helpers derive placement heights in world space.
                    var pos=room.position;var rot=room.rotation;room.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                    bool expanded=i==1||i==5;
                    if(expanded)
                    {
                        volume.localPosition=new Vector3(0,1.15f,-5.5f);trigger.size=new Vector3(5.8f,2.3f,8.8f);
                        var annexVolume=RingGroup("Annex Discovery",room);annexVolume.localPosition=new Vector3(i==1?-5.5f:-5,1.15f,i==1?-7.5f:-4);
                        var annexTrigger=annexVolume.gameObject.AddComponent<BoxCollider>();annexTrigger.isTrigger=true;annexTrigger.size=new Vector3(i==1?4.8f:3.8f,2.3f,i==1?4.8f:3.8f);annexVolume.gameObject.AddComponent<VentsAndVending.Tunnels.RoomVolume>().Configure(definition);
                        if(i==5){var deep=RingGroup("Deep Stores Discovery",room);deep.localPosition=new Vector3(-1,1.15f,-12);var box=deep.gameObject.AddComponent<BoxCollider>();box.isTrigger=true;box.size=new Vector3(3.8f,2.3f,3.8f);deep.gameObject.AddComponent<VentsAndVending.Tunnels.RoomVolume>().Configure(definition);}
                    }
                    var cells=new System.Collections.Generic.HashSet<Vector2Int>();StationRect(cells,-3,3,expanded?-10:-7,-1);
                    if(i==1)StationRect(cells,-8,-3,-10,-5);
                    if(i==5){StationRect(cells,-7,-3,-6,-2);StationRect(cells,-3,1,-14,-10);}
                    StationCellSpace(room,cells,0,2.7f,null,null,(p,n)=>(n.z>0 && p.z> -1.1f && Mathf.Abs(p.x)<1.05f) || (expanded && n.x>0 && Mathf.Abs(p.x-3)<.1f && Mathf.Abs(p.z+8)<.7f) || (i==5 && n.z<0 && p.z< -13.9f && Mathf.Abs(p.x+1)<1.05f));
                    StationTube(room,"Room Vestibule",new[]{new Vector3(0,0,0),new Vector3(0,0,-1)},2.1f,2.7f,false);
                    Prop(room,Props+"/"+(i==0||i==7?"PROP_Desk_Terminal":"PROP_Tool_Cart")+".prefab",-2.05f,0,-5.7f,180);
                    Prop(room,Props+"/PROP_Locker_Row.prefab",2.1f,0,-3.6f,90);
                    // Shells own collision; don't dress a solid panel across the new crawl mouth.
                    if(!expanded)foreach(float z in new[]{-6f,-4,-2})foreach(float x in new[]{-3f,3f})MaintenanceKit(room,"ENV_Wall_Standard_A",new Vector3(x,0,z),x<0?90:-90,new Vector3(1,.9f,1));
                    if(expanded)BuildStationRoomAnnex(room,rust,i==1);
                    BuildStationRoomFinish(room,cells);
                    StationLight(room,"Room Worklight",new Vector3(0,2.2f,-4),i==4?new Color(.3f,.7f,.8f):new Color(1,.7f,.4f),7,5);
                    room.SetPositionAndRotation(pos,rot);
                }
                var door=RingDoor(room,rust,usable,i==0||i==7);
                if(!usable){var locked=new SerializedObject(door);locked.FindProperty("_lockedMessage").stringValue=i==3?"Access collapsed. No safe way through.":"Permanently sealed service space. No access.";locked.ApplyModifiedPropertiesWithoutUndo();}
                Part("Service Door Nameplate",room,new Vector3(0,2.44f,.66f),new Vector3(2.6f,.34f,.08f),ArtLibrary.Get("M_Env_FrameDark"));
                StationLabel(room,names[i],new Vector3(0,2.44f,.715f),180,.017f);
                if(i==3)
                {
                    for(int n=0;n<7;n++)
                    {
                        var rubble=StationLocalBox(room,"Collapsed Masonry",new Vector3((n%3-1)*.65f,.2f+(n/3)*.3f,.45f),new Vector3(.8f,.55f,.55f),rust);
                        rubble.transform.localRotation=Quaternion.Euler(n*13,n*31,n*17);
                    }
                    Part("Bent Fallen Brace",room,new Vector3(0,1.1f,.35f),new Vector3(2.4f,.15f,.2f),StationMetal).transform.localRotation=Quaternion.Euler(0,0,25);
                }
                if(i==4)StationSteam(room,new Vector3(0,2.1f,-.7f),"STEAM_RING_225",true);
            }
            var roomDefinitions=AssetDatabase.FindAssets("t:VentRoomDefinition",new[]{"Assets/_Project/ScriptableObjects/Tunnels/StationRing"})
                .Select(id=>AssetDatabase.LoadAssetAtPath<VentsAndVending.Tunnels.VentRoomDefinition>(AssetDatabase.GUIDToAssetPath(id))).ToArray();
            var ringDefinition=roomDefinitions.First(r=>r.RoomId=="MAINT_RING");
            var acDefinition=roomDefinitions.First(r=>r.RoomId=="AC_RING");
            acDefinition.Configure("AC_RING",acDefinition.DisplayName,"station_ring",2,acDefinition.Connections.Where(c=>c.DestinationRoomId!="MAINT_ROOM_090" && c.DestinationRoomId!="MAINT_ROOM_270").Concat(new[]{new VentsAndVending.Tunnels.VentRoomDefinition.Connection{DestinationRoomId="MAINT_ROOM_090"},new VentsAndVending.Tunnels.VentRoomDefinition.Connection{DestinationRoomId="MAINT_ROOM_270"}}).ToArray());EditorUtility.SetDirty(acDefinition);
            ringDefinition.Configure("MAINT_RING","Maintenance Ring","station_ring",3,ringDefinition.Connections.Concat(roomDefinitions.Where(r=>r.RoomId.StartsWith("MAINT_ROOM_")).Select(r=>new VentsAndVending.Tunnels.VentRoomDefinition.Connection{DestinationRoomId=r.RoomId,GateId=r.RoomId=="MAINT_ROOM_225"?"STEAM_RING_225":null,GateOpensWhenOff=r.RoomId=="MAINT_ROOM_225"})).ToArray());EditorUtility.SetDirty(ringDefinition);
            Object.FindAnyObjectByType<VentsAndVending.Tunnels.TunnelManager>().SetRooms(roomDefinitions);
            for(int i=0;i<12;i++)
            {
                float a=28+i*30;var vent=RingGroup("Ambient Steam Vent",_stationFrame.parent);vent.SetPositionAndRotation(RingPoint(70.4f,a,0),Quaternion.Euler(0,90-a,0));
                StationSteam(vent,new Vector3(0,2.3f,.1f),null,false);
            }
        }

        private static void BuildStationRoomAnnex(Transform room,Material rust,bool pump)
        {
            // Two recognisable landmarks, not copies of the ring's generic square rooms.
            StationLocalBox(room,"Rear Bay Partition Left",new Vector3(-2.025f,1.35f,-5),new Vector3(1.95f,2.7f,.16f),StationWall);
            StationLocalBox(room,"Rear Bay Partition Right",new Vector3(2.025f,1.35f,-5),new Vector3(1.95f,2.7f,.16f),StationWall);
            var rearDoor=RingGroup(pump?"Pump Machinery Bay Door":"Workshop Rear Stores Door",room);rearDoor.localPosition=new Vector3(0,0,-5);
            RingDoor(rearDoor,rust,true,false);
            float annexZ=pump?-7.5f:-4;
            float annexLength=pump?5:4;
            float sideLength=(annexLength-2.1f)*.5f;
            foreach(float sign in new[]{-1f,1f})StationLocalBox(room,"Annex Partition",new Vector3(-3,1.35f,annexZ+sign*(1.05f+sideLength*.5f)),new Vector3(.16f,2.7f,sideLength),StationWall);
            var annexDoor=RingGroup(pump?"Pump Control Annex Door":"Tool Calibration Annex Door",room);annexDoor.localPosition=new Vector3(-3,0,annexZ);annexDoor.localRotation=Quaternion.Euler(0,90,0);
            RingDoor(annexDoor,rust,true,false);
            if(pump)
            {
                Prop(room,Props+"/PROP_Generator.prefab",-1.7f,0,-8.5f,0);
                Prop(room,Props+"/PROP_server_rack.prefab",-7,0,-8.5f,90);
                Prop(room,Props+"/PROP_Desk_Terminal.prefab",-6,0,-5.8f,180);
            }
            else
            {
                Prop(room,Props+"/PROP_Tool_Cart.prefab",-5.5f,0,-3,90);
                Prop(room,Props+"/PROP_Locker_Row.prefab",-2.3f,0,-12.5f,90);
                StationLight(room,"Deep Stores Marker",new Vector3(-.5f,2.2f,-12),new Color(.65f,.8f,1),6,5);
            }
            StationLight(room,"Annex Worklight",new Vector3(-5,2.2f,annexZ),pump?new Color(.55f,.8f,1):new Color(1,.6f,.3f),7,6);
            StationLabel(room,pump?"PUMP CONTROL / REAR MACHINERY":"CALIBRATION / DEEP STORES",new Vector3(0,2.45f,-4.7f),180,.016f);
            // Low side opening is sealed above the hatch. Duct rises outside the room shell,
            // reaches 3.3m before crossing the maintenance wall, and joins the existing L2 ring.
            StationLocalBox(room,"Crawl Mouth Header",new Vector3(3,1.85f,-8),new Vector3(.16f,1.7f,1.4f),StationWall);
            StationCrawlVolume(room,new Vector3(2.5f,.5f,-8),new Vector3(2.4f,1.05f,1.4f));
            StationTube(room,"Annex AC Riser",new[]{new Vector3(3,0,-8),new Vector3(4.2f,0,-8),new Vector3(4.2f,3.3f,-.8f),new Vector3(4.2f,3.3f,1.6f)},1.4f,1,true);
            StationHatch(room,"Rear Bay Crawl Hatch",new Vector3(3,0,-8),Vector3.right,1.4f,1);
            StationHatch(room,"Annex AC Ring Hatch",new Vector3(4.2f,3.3f,1.6f),Vector3.forward,1.4f,1);
            StationLabel(room,"AC RETURN / CRAWL TO RING",new Vector3(2.85f,1.4f,-8),90,.013f);
        }

        private static SlidingDoor RingDoor(Transform parent,Material rust,bool usable,bool open)
        {
            var root=RingGroup("Service Sliding Door",parent);
            var leaf=StationLocalBox(root,"Door Leaf",new Vector3(0,1.2f,0),new Vector3(2,2.4f,.16f),rust);
            // Thin attached stiffeners break up the slab without entering the clear opening.
            foreach(float face in new[]{-.58f,.58f})
            {
                foreach(float x in new[]{-.45f,.45f})Part("Door Edge Stiffener",leaf.transform,new Vector3(x,0,face),new Vector3(.035f,.96f,.16f),StationMetal);
                foreach(float y in new[]{-.28f,.28f})Part("Door Cross Brace",leaf.transform,new Vector3(0,y,face),new Vector3(.9f,.035f,.16f),StationMetal);
            }
            foreach(float x in new[]{-1.08f,1.08f})StationLocalBox(root,"Door Frame",new Vector3(x,1.2f,0),new Vector3(.16f,2.4f,.3f),StationMetal);
            StationLocalBox(root,"Door Header",new Vector3(0,2.55f,0),new Vector3(2.3f,.3f,.3f),StationMetal);
            if(usable)Part("Service Threshold",root,new Vector3(0,.012f,0),new Vector3(2.12f,.024f,.34f),StationMetal);
            if(usable)StationLocalBox(root,"Portal Crown Seal",new Vector3(0,2.85f,0),new Vector3(2.1f,.3f,.2f),StationWall);
            var door=root.gameObject.AddComponent<SlidingDoor>();door.Configure(new[]{leaf.transform},new[]{Vector3.right*2.2f});door.ConfigureManual(4);door.Locked=!usable;StationDoorMotion(door);
            var so=new SerializedObject(door);so.FindProperty("_securedOpen").boolValue=open;so.FindProperty("_doorwaySize").vector3Value=new Vector3(2,2.4f,1);so.ApplyModifiedPropertiesWithoutUndo();
            return door;
        }

        private static void StationSteam(Transform parent,Vector3 nozzle,string id,bool blocks)
        {
            var jetObject=new GameObject("Steam Jet");jetObject.transform.SetParent(parent,false);jetObject.transform.localPosition=nozzle;jetObject.transform.localRotation=Quaternion.Euler(65,0,0);
            var jet=jetObject.AddComponent<ParticleSystem>();jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=jet.main;main.loop=true;main.playOnAwake=false;main.startLifetime=1.2f;main.startSpeed=1.1f;main.startSize=blocks?.7f:.35f;main.startColor=new Color(.8f,.85f,.85f,.22f);main.maxParticles=70;
            var shape=jet.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=18;shape.radius=.055f;
            var emission=jet.emission;emission.rateOverTime=blocks?30:10;
            var fade=jet.colorOverLifetime;fade.enabled=true;var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.8f,.2f),new GradientAlphaKey(0,1)});fade.color=gradient;
            jet.GetComponent<ParticleSystemRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(ParticleMaterialPath);
            Part("Steam Nozzle",parent,nozzle,new Vector3(.3f,.15f,.22f),StationMetal);
            var audio=jetObject.AddComponent<AudioSource>();audio.clip=ProceduralAudio.DuctAir;audio.loop=true;audio.playOnAwake=false;audio.spatialBlend=1;audio.volume=blocks?.3f:.12f;audio.minDistance=1;audio.maxDistance=7;
            Collider blocker=null;
            if(blocks){var block=new GameObject("Steam Passage Blocker");block.transform.SetParent(parent,false);block.transform.localPosition=new Vector3(0,1.2f,-.7f);var box=block.AddComponent<BoxCollider>();box.size=new Vector3(2.1f,2.4f,.4f);blocker=box;}
            var valve=RingGroup(blocks?"Steam Isolation Valve":"Ambient Steam",parent);valve.localPosition=new Vector3(1.6f,1.25f,.3f);
            Transform wheel=null;
            if(blocks){valve.gameObject.AddComponent<BoxCollider>().size=Vector3.one*.4f;wheel=RingGroup("Valve Wheel",valve);foreach(float roll in new[]{0f,60,120})Part("Valve Spoke",wheel,Vector3.zero,new Vector3(.32f,.045f,.05f),ArtLibrary.Get("M_Env_TrimOrange")).transform.localRotation=Quaternion.Euler(0,0,roll);StationLabel(parent,"STEAM ISOLATION / E",new Vector3(1.6f,1.7f,.3f),180,.012f);}
            valve.gameObject.AddComponent<SteamLeak>().Configure(id,jet,audio,blocker,wheel);
        }
    }
}
