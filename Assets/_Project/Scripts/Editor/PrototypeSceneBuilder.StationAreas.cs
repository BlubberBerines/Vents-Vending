using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using VentsAndVending.Customers;
using VentsAndVending.Security;
using VentsAndVending.Tunnels;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private static Transform _stationLounge;
        private static Transform _stationBasement;
        private static Transform _stationEscort;

        private static void BuildStationLounge()
        {
            var lounge=RingGroup("L1_08_Customer_Lounge",_stationFrame);_stationLounge=lounge;
            var cells=new HashSet<Vector2Int>();StationRect(cells,-6,6,-6,6);
            StationCellSpace(lounge,cells,4.6f,4,null,null,(p,n)=>
                (n.z>0 && Mathf.Abs(p.x)<1.5f) || (n.x>0 && Mathf.Abs(p.z-5)<.65f) || (n.x<0 && Mathf.Abs(p.z)<1.25f));
            StationLocalBox(lounge,"Public Door Header",new Vector3(0,8.05f,6),new Vector3(3,1.1f,.14f),StationWall);
            StationLocalBox(lounge,"Customer Grille Sill",new Vector3(6,4.7f,5),new Vector3(.14f,.2f,1.4f),StationWall);
            StationLocalBox(lounge,"Customer Grille Header",new Vector3(6,7.2f,5),new Vector3(.14f,2.8f,1.4f),StationWall);
            StationLocalBox(lounge,"Emergency Door Header",new Vector3(-6,8,0),new Vector3(.14f,1.2f,2.5f),StationWall);
            Material velvet=GetOrCreateMaterial("Station_Lounge_Velvet",new Color(.34f,.12f,.3f),.15f);
            Material gold=GetOrCreateMaterial("Station_Lounge_Gold",new Color(.65f,.4f,.1f),.8f);
            foreach(float x in new[]{-4f,4f})
            {
                StationLocalBox(lounge,"Lounge Sofa Seat",new Vector3(x,4.9f,-2),new Vector3(1.3f,.6f,3),velvet);
                StationLocalBox(lounge,"Lounge Sofa Back",new Vector3(x+(x<0?-.55f:.55f),5.5f,-2),new Vector3(.2f,.9f,3),velvet);
            }
            StationLocalBox(lounge,"Coffee Table",new Vector3(0,5.05f,-2),new Vector3(2,.7f,1.2f),gold);
            Prop(lounge,Props+"/PROP_Plant_Pot.prefab",-5,4.6f,-5,0);
            Prop(lounge,Props+"/PROP_Plant_Pot.prefab",5,4.6f,-5,0);
            StationLight(lounge,"Lounge Warm",new Vector3(-2,7.7f,-1),new Color(1,.75f,.4f),12,18);
            StationLight(lounge,"Lounge Lilac",new Vector3(3,7.7f,2),new Color(.7f,.5f,1),12,14);
            StationLabel(lounge,"CONCOURSE LOUNGE\nCUSTOMER MEETING AREA",new Vector3(0,7.5f,5.8f),0,.055f);
            StationLabel(lounge,"CONCOURSE LOUNGE\nCUSTOMERS / SERVICE ACCESS",new Vector3(0,7.5f,6.2f),180,.05f);
            StationLabel(lounge,"08 CUSTOMER GRILLE",new Vector3(5.85f,6.1f,5),90,.025f);
            // Reuse the existing customer definitions and visit/trade behaviour, not decorative stand-ins.
            _glarbJuice=AssetDatabase.LoadAssetAtPath<VentsAndVending.Crafting.ProductDefinition>($"{ProductFolder}/Product_GlarbJuice.asset");
            if(_glarbJuice==null)throw new System.InvalidOperationException("The station lounge requires the existing Glarb-Juice product asset.");
            _blorbDefinition=CreateBlorbDefinition();_gromflomiteDefinition=CreateGromflomiteDefinition();
            var spawn=RingGroup("Lounge Customer Entrance",_stationFrame);spawn.position=RingPoint(70,3,4.65f);
            var meetings=new List<VentsAndVending.Orders.MeetSpot>();
            foreach(var p in new[]{new Vector3(-1.8f,4.65f,.8f),new Vector3(1.8f,4.65f,-3.5f)})
            {
                string id=meetings.Count==0?"A":"B";
                var point=RingGroup("Lounge Delivery "+id,lounge);point.localPosition=p;
                var spot=point.gameObject.AddComponent<VentsAndVending.Orders.MeetSpot>();
                spot.Configure(id,"Concourse Lounge",VentsAndVending.Orders.MeetRisk.Low,
                    "Meet me in the lounge. Use my mugshot and name to find me among passengers, not at a marked spot.",new Renderer[0],null,null);
                spot.ConfigureRoomMeeting();
                meetings.Add(spot);
            }
            var customers=CreateRichCustomers().Cast<CustomerDefinition>().ToArray();
            foreach(var customer in customers)BuildStationPortrait(customer);
            RingGroup("Station VentNet Orders",lounge).gameObject.AddComponent<VentsAndVending.Orders.OrderManager>()
                .Configure(customers,meetings,spawn,spawn,ProceduralAudio.Message);
            BuildStationCrowd(customers,spawn);
            _stationEscort=RingGroup("U01 Escort Exit",lounge);_stationEscort.localPosition=new Vector3(-4.5f,4.65f,0);
        }

        private static void BuildStationBasement()
        {
            var basement=RingGroup("L4_U01_Drone_Shortcut",_stationFrame);_stationBasement=basement;
            var cells=new HashSet<Vector2Int>();
            StationRect(cells,0,11.5f,-3.5f,-.5f);StationRect(cells,8.5f,11.5f,-.5f,13.5f);
            StationRect(cells,-10,8.5f,10.5f,13.5f);
            StationRect(cells,11.5f,16.5f,3,8); // supply closet: guaranteed hiding room
            StationRect(cells,-4,1,13.5f,18); // pump side room
            StationCellSpace(basement,cells,-3.6f,2.8f,null,p=>p.x>=1&&p.x<2.5f&&p.z>=-3&&p.z< -1.5f,
                (p,n)=>n.x<0 && p.x< -9.9f);
            // Fully lined shaft: the only vertical opening in the maintenance hub's deck.
            foreach(float x in new[]{.94f,2.56f})StationLocalBox(basement,"Grate Shaft Liner",new Vector3(x,-.4f,-2.25f),new Vector3(.12f,.8f,1.62f),StationMetal);
            foreach(float z in new[]{-3.06f,-1.44f})StationLocalBox(basement,"Grate Shaft Liner",new Vector3(1.75f,-.4f,z),new Vector3(1.5f,.8f,.12f),StationMetal);
            CreateCrowbar();
            var grate=RingGroup("GATE_GRATE_03 Crowbar Floor Grate",basement);grate.localPosition=new Vector3(1.75f,0,-2.25f);
            var panel=RingGroup("Pryable Grille",grate);
            for(float x=-.7f;x<=.7f;x+=.14f)StationLocalBox(panel,"Grate Bar",new Vector3(x,0,0),new Vector3(.04f,.04f,1.5f),StationMetal);
            foreach(float z in new[]{-.75f,.75f})StationLocalBox(panel,"Grate Frame",new Vector3(0,0,z),new Vector3(1.55f,.06f,.06f),StationMetal);
            var blocker=grate.gameObject.AddComponent<BoxCollider>();blocker.size=new Vector3(1.5f,.08f,1.5f);blocker.center=Vector3.down*.04f;
            grate.gameObject.AddComponent<CrowbarGrate>().Configure(TunnelIds.GrateGate,Crowbar,panel,blocker,ProceduralAudio.PryScreech);
            // A legal pickup on the hub workbench provides the same crowbar prerequisite as main.
            var pickup=AssetDatabase.LoadAssetAtPath<GameObject>($"{ItemPrefabFolder}/Pickup_crowbar.prefab");
            if(pickup!=null){var go=(GameObject)PrefabUtility.InstantiatePrefab(pickup,_stationFrame);go.transform.localPosition=new Vector3(-2,.2f,3);}
            StationLocalBox(basement,"Soft Landing",new Vector3(1.75f,-3.5f,-2.25f),new Vector3(1.5f,.2f,1.5f),ArtLibrary.Get("M_WorkshopOrange"));
            StationTube(basement,"Emergency Stairwell",new[]{new Vector3(-10,-3.6f,12),new Vector3(-18,-.87f,4),new Vector3(-18,1.87f,-8),new Vector3(-10,4.6f,-8),new Vector3(-10,4.6f,0),new Vector3(-6,4.6f,0)},2.4f,2.8f,false);
            var exit=RingGroup("GATE_EXIT_U01 Push Bar Exit",basement);exit.localPosition=new Vector3(-6,4.6f,0);exit.localRotation=Quaternion.Euler(0,90,0);
            var hinge=RingGroup("Door Hinge",exit);hinge.localPosition=new Vector3(-1.2f,0,0);
            StationLocalBox(hinge,"Door Slab",new Vector3(1.2f,1.2f,0),new Vector3(2.4f,2.4f,.1f),StationWall);
            StationLocalBox(hinge,"Push Bar",new Vector3(1.2f,1,-.1f),new Vector3(2,.06f,.08f),ArtLibrary.Get("M_Env_TrimOrange"));
            StationLocalBox(exit,"Exit Frame Header",new Vector3(0,2.6f,0),new Vector3(2.4f,.4f,.14f),StationWall);
            exit.gameObject.AddComponent<LatchDoor>().Configure(TunnelIds.UnderpassExit,hinge,-95,Vector3.back,"EMERGENCY EXIT ONLY. No outside handle.","Push emergency bar",ProceduralAudio.Clunk,4);
            var restricted=RingGroup("U01 Restricted Zone",basement);restricted.localPosition=new Vector3(3,-2.2f,5);
            restricted.gameObject.AddComponent<BoxCollider>().size=new Vector3(30,2.8f,28);restricted.gameObject.AddComponent<SecurityZone>().Configure(SecurityZoneKind.Restricted,TunnelIds.Underpass,_stationEscort);
            var hiding=RingGroup("Supply Closet Hiding Zone",basement);hiding.localPosition=new Vector3(14,-2.4f,5.5f);
            hiding.gameObject.AddComponent<BoxCollider>().size=new Vector3(4,2.4f,4);hiding.gameObject.AddComponent<SecurityZone>().Configure(SecurityZoneKind.Hideout,"closet_u01",null);
            StationLocalBox(basement,"Closet Shelf",new Vector3(16,-2.7f,5.5f),new Vector3(.4f,1.8f,3.5f),StationMetal);
            foreach(var p in new[]{new Vector3(2,-1.3f,-2),new Vector3(10,-1.3f,6),new Vector3(0,-1.3f,12),new Vector3(-8,-1.3f,12)})
                StationLight(basement,"U01 Worklight",p,new Color(.3f,.65f,1),10,9);
            StationLight(basement,"Closet Dim Lamp",new Vector3(14,-1.2f,5),new Color(.25f,.5f,.6f),6,2);
            StationLabel(basement,"U01 / DRONE PATROL\nSUPPLY CLOSET -> / EMERGENCY EXIT <-",new Vector3(10,-1,10.3f),0,.03f);
            BuildStationTunnelState();
            new GameObject("Station Security").AddComponent<SecurityManager>();
            BuildStationDrone("U01 Patrol Drone A",new[]{new Vector3(2,0,-2),new Vector3(9.7f,0,-2),new Vector3(10,0,6),new Vector3(10,0,11.8f)});
            BuildStationDrone("U01 Patrol Drone B",new[]{new Vector3(8,0,12),new Vector3(0,0,12),new Vector3(-8,0,12),new Vector3(0,0,12)});
        }

        private static void BuildStationDrone(string name,Vector3[] localPoints)
        {
            var points=localPoints.Select(p=>_stationFrame.TransformPoint(p)).ToArray();
            var zone=new Bounds(points[0],Vector3.zero);foreach(var p in points)zone.Encapsulate(p);zone.Expand(4);
            BuildDrone(name,1.9f,points.Select(p=>new Stop(p.x,p.z,1.5f,65)).ToArray(),new (int from,Stop[] points,float chance)[0],zone,false,-3.6f);
            GameObject.Find(name).transform.SetParent(_stationFrame,true);GameObject.Find(name+" Route").transform.SetParent(_stationFrame,true);
        }

        private static void BuildStationTunnelState()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects/Tunnels/StationRing");
            var rooms=new List<VentRoomDefinition>();
            var nodes=new[]{(TunnelIds.Nook,"Den",3,new[]{TunnelIds.ServiceJunction}),
                (TunnelIds.ServiceJunction,"Service Junction",3,new[]{TunnelIds.Nook,TunnelIds.CustomerVent,TunnelIds.PowerDistribution,TunnelIds.LongCrawl}),
                (TunnelIds.LongCrawl,"Maintenance Gallery / S01",3,new[]{TunnelIds.ServiceJunction,"MAINT_RING",TunnelIds.Underpass}),
                ("MAINT_RING","Maintenance Ring",3,new[]{TunnelIds.LongCrawl}),
                (TunnelIds.CustomerVent,"Customer Vent",2,new[]{TunnelIds.ServiceJunction,"LOUNGE","AC_RING"}),
                ("LOUNGE","Customer Lounge",1,new[]{TunnelIds.CustomerVent,"CONCOURSE"}),
                ("CONCOURSE","Public Concourse",1,new[]{"LOUNGE"}),
                ("AC_RING","AC Trunk Ring",2,new[]{TunnelIds.CustomerVent}),
                (TunnelIds.PowerDistribution,"Power Distribution",3,new[]{TunnelIds.ServiceJunction,TunnelIds.FanJunction}),
                (TunnelIds.FanJunction,"Fan Junction",3,new[]{TunnelIds.PowerDistribution,TunnelIds.FanRoom}),
                (TunnelIds.FanRoom,"Extraction Service Cache",3,new[]{TunnelIds.FanJunction}),
                (TunnelIds.Underpass,"Maintenance Underpass",4,new[]{"LOUNGE"})};
            foreach(var node in nodes)
            {
                string path=$"Assets/_Project/ScriptableObjects/Tunnels/StationRing/{node.Item1}.asset";
                var room=AssetDatabase.LoadAssetAtPath<VentRoomDefinition>(path);
                if(room==null){room=ScriptableObject.CreateInstance<VentRoomDefinition>();AssetDatabase.CreateAsset(room,path);}
                room.Configure(node.Item1,node.Item2,"station_ring",node.Item3,node.Item4.Select(id=>new VentRoomDefinition.Connection{DestinationRoomId=id,GateId=(node.Item1==TunnelIds.FanJunction && id==TunnelIds.FanRoom)||(node.Item1==TunnelIds.FanRoom && id==TunnelIds.FanJunction)?TunnelIds.FanBreaker:id==TunnelIds.Underpass?TunnelIds.GrateGate:node.Item1==TunnelIds.Underpass?TunnelIds.UnderpassExit:null,GateOpensWhenOff=(node.Item1==TunnelIds.FanJunction && id==TunnelIds.FanRoom)||(node.Item1==TunnelIds.FanRoom && id==TunnelIds.FanJunction),OneWay=id==TunnelIds.Underpass||node.Item1==TunnelIds.Underpass}).ToArray());
                EditorUtility.SetDirty(room);rooms.Add(room);
            }
            RingGroup("Station Tunnel State",_stationFrame).gameObject.AddComponent<TunnelManager>().SetRooms(rooms);
            foreach(var zone in _stationFrame.parent.GetComponentsInChildren<VentsAndVending.World.VentZone>())
                if(zone.name=="AC Crawl Zone")zone.gameObject.AddComponent<RoomVolume>().Configure(rooms.First(r=>r.RoomId=="AC_RING"));
            var ring=GameObject.Find("L3_Enclosed_Maintenance_Ring").transform;
            for(int sector=0;sector<24;sector++)
            {
                float angle=(sector+.5f)*15;
                var zone=RingGroup($"L3 S{sector+1:00} Discovery",ring);
                zone.SetPositionAndRotation(RingPoint(72,angle,1.3f),Quaternion.Euler(0,-angle,0));
                var trigger=zone.gameObject.AddComponent<BoxCollider>();trigger.isTrigger=true;trigger.size=new Vector3(3.8f,2.6f,19);
                zone.gameObject.AddComponent<RoomVolume>().Configure(rooms.First(r=>r.RoomId=="MAINT_RING"));
            }
            foreach(var entry in new[]{
                (TunnelIds.Nook,new Vector3(0,1.3f,-9),new Vector3(5.8f,2.6f,5.7f)),
                (TunnelIds.ServiceJunction,new Vector3(0,1.3f,0),new Vector3(7.8f,2.6f,11.8f)),
                (TunnelIds.LongCrawl,new Vector3(0,1.3f,8),new Vector3(2.8f,2.6f,4)),
                (TunnelIds.PowerDistribution,new Vector3(-7,1.3f,0),new Vector3(5.8f,2.6f,5.8f)),
                (TunnelIds.CustomerVent,new Vector3(8,3.8f,0),new Vector3(1.8f,.9f,1.8f)),
                ("LOUNGE",new Vector3(0,6,-1),new Vector3(11.8f,2.6f,9.8f)),
                (TunnelIds.Underpass,new Vector3(5,-2.2f,5),new Vector3(15,2.6f,20))})
            {
                var volume=RingGroup("Discovery "+entry.Item1,_stationFrame);volume.localPosition=entry.Item2;
                var box=volume.gameObject.AddComponent<BoxCollider>();box.size=entry.Item3;box.isTrigger=true;
                volume.gameObject.AddComponent<RoomVolume>().Configure(rooms.First(r=>r.RoomId==entry.Item1));
            }
        }

        private static void BuildStationNavigation()
        {
            foreach(var parent in new[]{_stationFrame.parent,_stationBasement})
            {
                var surface=parent.gameObject.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
                bool publicLevel=parent==_stationFrame.parent;
                if(publicLevel)
                {
                    int layer=LayerMask.NameToLayer("PublicNavigation");
                    if(layer<0)
                    {
                        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
                        var layers=tags.FindProperty("layers");
                        for(int i=8;i<31;i++)if(string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)){layers.GetArrayElementAtIndex(i).stringValue="PublicNavigation";layer=i;break;}
                        if(layer<0)throw new System.InvalidOperationException("No free layer for public navigation.");
                        tags.ApplyModifiedPropertiesWithoutUndo();
                    }
                    foreach(var name in new[]{"L1_Concourse_Structure","Outer_Wall_Space_Windows","Sector_Joints","District_Bulkheads","L1_08_Customer_Lounge"})
                        foreach(var collider in GameObject.Find(name).GetComponentsInChildren<Collider>())collider.gameObject.layer=layer;
                    surface.layerMask=1<<layer;
                }
                surface.overrideVoxelSize=true;surface.voxelSize=.08f;surface.BuildNavMesh();
                string path=$"Assets/_Project/Scenes/Districts/Station_{(publicLevel?"Lounge":"Underpass")}_NavMesh.asset";
                var old=AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
                if(old!=null){EditorUtility.CopySerialized(surface.navMeshData,old);surface.RemoveData();var so=new SerializedObject(surface);so.FindProperty("m_NavMeshData").objectReferenceValue=old;so.ApplyModifiedPropertiesWithoutUndo();surface.AddData();}
                else AssetDatabase.CreateAsset(surface.navMeshData,path);
            }
        }
    }
}
