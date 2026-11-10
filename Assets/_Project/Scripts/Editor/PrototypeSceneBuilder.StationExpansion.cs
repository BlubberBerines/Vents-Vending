using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VentsAndVending.World;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private const float ServiceAngle = 12.5f;
        private static readonly List<Vector3> StationCrawlAudit = new();
        private static readonly List<Vector3> StationWalkAudit = new();
        private static Transform _stationFrame;
        private static Material StationMetal => ArtLibrary.Get("M_Env_FloorMetal");
        private static Material StationWall => ArtLibrary.Get("M_Env_StationPurple");

        private static void BuildStationLowerLayers(Transform root)
        {
            StationCrawlAudit.Clear(); StationWalkAudit.Clear();
            _stationMeshSequence=0;
            _stationFrame=RingGroup("Station_Service_Network",root);
            _stationFrame.SetPositionAndRotation(RingPoint(60,ServiceAngle,0),Quaternion.Euler(0,90-ServiceAngle,0));
            BuildContinuousRing(root,true); BuildContinuousRing(root,false);
            BuildStationHub(); BuildStationDucts(); BuildStationLounge();
            BuildStationDenGameplay(); BuildStationBasement(); BuildStationAtmosphere(); BuildStationMaintenanceFinish(); BuildStationLocalControls(); BuildStationFanPuzzle(); BuildStationRingRooms(); BuildStationResourceLoop(); BuildStationDeliveries(); BuildStationNavigation();
            BuildStationDenPipes();
        }

        private static void BuildStationDenPipes()
        {
            var den=_stationFrame.Find("L3_01_Den");var pipes=RingGroup("Den Ceiling Pipework",den);
            MaintenancePipe(pipes,new Vector3(2.65f,2.65f,-.6f),new Vector3(2.65f,2.65f,-5.9f),.085f,StationMetal);
            MaintenancePipe(pipes,new Vector3(-2.65f,2.66f,-5.8f),new Vector3(2.65f,2.66f,-5.8f),.07f,StationMetal);
            MaintenancePipe(pipes,new Vector3(2.65f,2.65f,-5.8f),new Vector3(2.65f,2.25f,-5.8f),.085f,StationMetal);
            StationSteam(pipes,new Vector3(2.65f,2.32f,-5.8f),null,false);
            foreach(var collider in pipes.GetComponentsInChildren<Collider>())collider.enabled=false;
        }

        private static void BuildContinuousRing(Transform root,bool crawl)
        {
            float inner=crawl?71.2f:70, outer=crawl?72.8f:74, floor=crawl?3.3f:0, height=crawl?1:3;
            var parent=RingGroup(crawl?"L2_Enclosed_AC_Ring":"L3_Enclosed_Maintenance_Ring",root);
            float portalAngle=crawl?ServiceAngle-Mathf.Asin(12/71.2f)*Mathf.Rad2Deg:ServiceAngle;
            // Openings are slightly narrower than the overlapping connector shells, sealing their jambs.
            float halfAngle=crawl?.55f:1.2f;
            var angles=new SortedSet<float>(Enumerable.Range(0,145).Select(i=>i*2.5f));
            if(!crawl)foreach(float roomAngle in StationAccessibleRoomAngles){angles.Add(roomAngle-.82f);angles.Add(roomAngle+.82f);}
            if(crawl)foreach(float roomAngle in StationRoomVentAngles){angles.Add(roomAngle-.55f);angles.Add(roomAngle+.55f);}
            angles.Add(portalAngle-halfAngle);angles.Add(portalAngle+halfAngle);
            var edge=angles.ToArray();
            for(int i=0;i<edge.Length-1;i++)
            {
                float a=edge[i],b=edge[i+1],mid=(a+b)*.5f;
                bool portal=Mathf.Abs(Mathf.DeltaAngle(mid,portalAngle))<halfAngle-.001f || (!crawl && StationAccessibleRoomAngles.Any(angle=>Mathf.Abs(Mathf.DeltaAngle(mid,angle))<.819f)) || (crawl && StationRoomVentAngles.Any(angle=>Mathf.Abs(Mathf.DeltaAngle(mid,angle))<.549f));
                bool observation=crawl && IsStationObservationAngle(mid);
                if(!observation) StationWedge(parent,"Deck",inner,outer,floor-.12f,floor,a,b);
                else StationRingGrate(parent,inner,outer,floor,a,b);
                bool roofGrate=crawl && IsStationObservationAngle(mid);
                bool ceilingHole=!crawl && IsStationObservationAngle(mid);
                if(!roofGrate && !ceilingHole)StationWedge(parent,"Ceiling",inner,outer,floor+height,floor+height+.12f,a,b);
                if(ceilingHole)
                {
                    StationWedge(parent,"Inspection Ceiling Inner Border",inner,71.2f,3,3.12f,a,b);
                    StationWedge(parent,"Inspection Ceiling Outer Border",72.8f,outer,3,3.12f,a,b);
                    StationWedge(parent,"Inspection Shaft Inner",71.08f,71.2f,3.12f,3.3f,a,b);
                    StationWedge(parent,"Inspection Shaft Outer",72.8f,72.92f,3.12f,3.3f,a,b);
                }
                if(roofGrate)StationRingGrate(parent,inner,outer,floor+height,a,b);
                if(!portal)StationWedge(parent,"Inner Wall",inner-.12f,inner,floor,floor+height,a,b);
                StationWedge(parent,"Outer Wall",outer,outer+.12f,floor,floor+height,a,b);
                if(crawl)
                {
                    var z=new GameObject("AC Crawl Zone");z.transform.SetParent(parent,false);
                    z.transform.SetPositionAndRotation(RingPoint(72,mid,floor+.5f),Quaternion.Euler(0,-mid,0));
                    var box=z.AddComponent<BoxCollider>();box.size=new Vector3(1.7f,1.05f,72*(b-a)*Mathf.Deg2Rad+.1f);box.isTrigger=true;z.AddComponent<VentZone>();
                    StationCrawlAudit.Add(RingPoint(72,mid,floor+.4f));
                }
                else StationWalkAudit.Add(RingPoint(72,mid,1));
                if(i%4==0)CreatePointLight(crawl?"AC Amber":"Maintenance Cyan",RingPoint(72,mid,floor+(crawl?.65f:2.4f)),crawl?new Color(1,.65f,.3f):new Color(.6f,.8f,1),crawl?7:14,crawl?2:12,parent);
            }
        }

        private static bool IsStationObservationAngle(float angle)
        {
            int sector=Mathf.FloorToInt(angle/15);
            return sector%4==2 && Mathf.Abs(angle-(sector*15+3.75f))<1.26f;
        }

        private static void BuildStationHub()
        {
            var hub=RingGroup("L3_02_Service_Junction_and_Side_Rooms",_stationFrame);
            var cells=new HashSet<Vector2Int>();
            StationRect(cells,-4,4,-6,6);StationRect(cells,-1.5f,1.5f,6,10.15f);
            StationRect(cells,-10,-4,-3,3);StationRect(cells,4,10,-6,-2);
            StationCellSpace(hub,cells,0,3,p=>p.x>=1 && p.x<2.5f && p.z>=-3 && p.z< -1.5f,null,
                (p,n)=> (p.z>9.9f && n.z>0) || (p.z< -5.9f && Mathf.Abs(p.x)<1.05f && n.z<0) || (p.x>3.9f && p.x<4.1f && Mathf.Abs(p.z)<.65f && n.x>0) || (p.x< -9.9f && n.x<0 && Mathf.Abs(p.z)<1.5f));
            StationLocalBox(hub,"Crate Mouth Sill",new Vector3(4,.9f,0),new Vector3(.16f,1.8f,1.3f),StationWall);
            StationLocalBox(hub,"Crate Mouth Header",new Vector3(4,2.9f,0),new Vector3(.16f,.2f,1.3f),StationWall);
            Kitp(hub,"ENV_Crate_Fixed_A",3.45f,0,0,0);
            StationPartition(hub,-4,-3,3,"16 POWER DISTRIBUTION");
            StationPartition(hub,4,-6,-2,"09 SUPPLY STORE");
            Prop(hub,Props+"/PROP_server_rack.prefab",-9.2f,0,1.8f,90);
            Prop(hub,Props+"/PROP_Generator.prefab",-8.7f,0,-1.8f,90);
            Prop(hub,Props+"/PROP_Locker_Row.prefab",7,0,-5.4f,0);
            StationLight(hub,"Hub Worklight",new Vector3(0,2.5f,1),new Color(1,.8f,.55f),11,12);
            StationLight(hub,"Power Room Cyan",new Vector3(-7,2.4f,0),new Color(.35f,.8f,1),7,8);
            StationLight(hub,"Stores Amber",new Vector3(7,2.4f,-4),new Color(1,.65f,.35f),7,8);
            BuildStationDen();
            StationLabel(hub,"02 SERVICE JUNCTION\nDEN / AC ACCESS / U01 SHORTCUT",new Vector3(0,2.3f,5.7f),180,.045f);
            // Put the instruction beside the hatch, not across its bars/hinge sweep.
            StationLabel(hub,"CRAWL TO LOUNGE\nCRATE -> HATCH\nL2 AC ACCESS",new Vector3(3.72f,1.25f,1.85f),90,.018f);
        }

        private static void BuildStationDen()
        {
            var den=RingGroup("L3_01_Den",_stationFrame);
            foreach(float x in new[]{-2f,0,2})foreach(float z in new[]{-1.3f,-3.3f,-5.3f})
            {Kitp(den,"ENV_Floor_Solid_A",x,0,z,0);Kitp(den,"ENV_Ceiling_Standard_A",x,3,z,0);}
            foreach(float x in new[]{-2f,0,2})
            {Kitp(den,"ENV_Wall_Standard_A",x,0,-6.3f,0);Kitp(den,x==0?"ENV_Wall_DoorSliding_A":"ENV_Wall_Standard_A",x,0,-.3f,180);}
            foreach(float z in new[]{-1.3f,-3.3f,-5.3f})
            {Kitp(den,"ENV_Wall_Standard_A",-3,0,z,90);Kitp(den,"ENV_Wall_Standard_A",3,0,z,-90);}
            // Existing dressing computes prop heights in world space: dress at origin before placement.
            var pos=den.position;var rot=den.rotation;den.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
            DressTestBayDen(den,true);den.SetPositionAndRotation(pos,rot);
            StationLight(den,"Den Light",new Vector3(0,2.5f,-3.3f),new Color(1,.8f,.55f),8,10);
            den.localPosition=new Vector3(0,0,-5.7f);
        }

        private static void BuildStationDucts()
        {
            var ducts=RingGroup("L2_Hatched_Connectors",_stationFrame);
            StationTube(ducts,"Crate Riser",new[]{new Vector3(4,1.8f,0),new Vector3(4.6f,1.8f,0),new Vector3(7,3.3f,0)},1.2f,1,true);
            var junction=new HashSet<Vector2Int>();StationRect(junction,7,9,-1,1);
            StationCellSpace(ducts,junction,3.3f,1,null,null,(p,n)=>(n.x<0 && Mathf.Abs(p.z)<.65f) || (Mathf.Abs(p.x-8)<.65f && Mathf.Abs(n.z)>.5f));
            StationCrawlVolume(ducts,new Vector3(8,3.8f,0),new Vector3(2,1.05f,2));
            StationTube(ducts,"Customer Riser 08",new[]{new Vector3(8,3.3f,1),new Vector3(8,4.8f,5),new Vector3(6,4.8f,5)},1.2f,1,true);
            float endZ=Mathf.Sqrt(71.2f*71.2f-144)-60+.35f;
            StationTube(ducts,"AC Loop Branch",new[]{new Vector3(8,3.3f,-1),new Vector3(8,3.3f,-4),new Vector3(12,3.3f,-4),new Vector3(12,3.3f,endZ)},1.4f,1,true);
            StationHatch(ducts,"Maintenance Mouth Hatch",new Vector3(4,1.8f,0),Vector3.right,1.2f,1);
            StationHatch(ducts,"Riser Junction Hatch",new Vector3(7,3.3f,0),Vector3.right,1.2f,1);
            StationHatch(ducts,"Customer Route Hatch",new Vector3(8,3.3f,1),Vector3.forward,1.2f,1);
            StationHatch(ducts,"Lounge Customer Grille",new Vector3(6,4.8f,5),Vector3.left,1.2f,1);
            StationHatch(ducts,"AC Branch Hatch",new Vector3(8,3.3f,-1),Vector3.back,1.4f,1);
            StationHatch(ducts,"AC Trunk Hatch",new Vector3(12,3.3f,endZ),new Vector3(12,0,endZ+60).normalized,1.4f,1);
        }

        private static GameObject StationLocalBox(Transform parent,string name,Vector3 pos,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=pos;go.transform.localScale=size;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }
        private static void StationLight(Transform parent,string name,Vector3 pos,Color color,float range,float intensity)
        {var light=new GameObject(name).AddComponent<Light>();light.transform.SetParent(parent,false);light.transform.localPosition=pos;light.type=LightType.Point;light.color=color;light.range=range;light.intensity=intensity;}
        private static void StationLabel(Transform parent,string value,Vector3 pos,float yaw,float size)
        {
            var go=new GameObject(value.Split('\n')[0]);go.transform.SetParent(parent,false);go.transform.SetLocalPositionAndRotation(pos,Quaternion.Euler(0,yaw,0));
            var t=go.AddComponent<TextMesh>();t.text=value;t.fontSize=48;t.characterSize=size;t.anchor=TextAnchor.MiddleCenter;t.alignment=TextAlignment.Center;t.color=new Color(.45f,.9f,1);
            go.GetComponent<MeshRenderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Settings/ConcourseWorldText.mat");
        }
    }
}
