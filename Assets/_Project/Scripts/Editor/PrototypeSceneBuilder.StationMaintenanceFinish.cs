using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildStationMaintenanceFinish()
        {
            var finish=RingGroup("Maintenance Architectural Finish",_stationFrame.parent);
            var hub=RingGroup("Den Approach Panelwork",finish);
            hub.SetPositionAndRotation(_stationFrame.position,_stationFrame.rotation);
            var shell=_stationFrame.Find("L3_02_Service_Junction_and_Side_Rooms");
            // Group the original half-metre wall cells into runs. Openings remain openings;
            // the existing sealed collision shell continues to own navigation and enclosure.
            var walls=shell.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Sealed Boundary").ToArray();
            foreach(var group in walls.GroupBy(c=>new {X=c.size.x*c.transform.localScale.x<.2f,Plane=Mathf.RoundToInt((c.size.x*c.transform.localScale.x<.2f?c.transform.localPosition.x:c.transform.localPosition.z)*100)}))
            {
                bool xWall=group.Key.X;
                var ordered=group.OrderBy(c=>xWall?c.transform.localPosition.z:c.transform.localPosition.x).ToArray();
                int start=0;
                for(int end=1;end<=ordered.Length;end++)
                {
                    if(end<ordered.Length && Vector3.Distance(ordered[end].transform.localPosition,ordered[end-1].transform.localPosition)<.51f)continue;
                    var first=ordered[start].transform.localPosition;var last=ordered[end-1].transform.localPosition;
                    var center=(first+last)*.5f;center.y=0;
                    Vector3 room=center.x< -4 && Mathf.Abs(center.z)<3.1f?new Vector3(-7,0,0):center.x>4 && center.z< -1.9f?new Vector3(7,0,-4):center.z>6?new Vector3(0,0,8):Vector3.zero;
                    var normal=xWall?Vector3.right*Mathf.Sign(room.x-center.x):Vector3.forward*Mathf.Sign(room.z-center.z);
                    float length=(end-start)*.5f;int panels=Mathf.CeilToInt(length/2);float width=length/panels;
                    var along=xWall?Vector3.forward:Vector3.right;
                    for(int i=0;i<panels;i++)
                    {
                        var p=center+normal*.065f+along*(-length*.5f+width*(i+.5f));
                        MaintenanceKit(hub,"ENV_Wall_Standard_A",p,Quaternion.LookRotation(normal).eulerAngles.y,new Vector3(width/2,1,1));
                    }
                    start=end;
                }
            }
            for(float z=-5;z<=5;z+=2)
            {
                foreach(float x in new[]{-3f,-1,1,3})
                    MaintenanceKit(hub,"ENV_Ceiling_Standard_A",new Vector3(x,3,z),0,Vector3.one);
                // Lower shoulders around a higher centre make the ceiling read as a service alcove.
                foreach(float x in new[]{-3.3f,3.3f})
                    Part("Recessed Ceiling Shoulder",hub,new Vector3(x,2.78f,z),new Vector3(1.25f,.26f,1.9f),ArtLibrary.Get("M_Env_FrameDark"));
                Part("Overhead Cross Rib",hub,new Vector3(0,2.84f,z-.94f),new Vector3(7.8f,.18f,.13f),ArtLibrary.Get("M_Env_FrameDark"));
            }
            var pipe=MaintenanceMaterial("Station_Oxidised_Pipe","M_BrushedSilver",new Color(.24f,.22f,.19f));
            foreach(float x in new[]{-2.9f,2.9f})
            {
                MaintenancePipe(hub,new Vector3(x,2.55f,-5.7f),new Vector3(x,2.55f,5.7f),.085f,pipe);
                MaintenancePipe(hub,new Vector3(x+.23f,2.68f,-5.7f),new Vector3(x+.23f,2.68f,5.7f),.045f,pipe);
                for(float z=-5;z<=5;z+=2)Part("Pipe Clamp",hub,new Vector3(x,2.55f,z),new Vector3(.24f,.24f,.055f),ArtLibrary.Get("M_Env_TrimOrange"));
            }
            // The same panel family continues around L3, without changing the L2 crawl ring.
            var ring=RingGroup("Maintenance Ring Panelwork",finish);
            const int bays=144;
            for(int i=0;i<bays;i++)
            {
                float angle=(i+.5f)*360/bays;
                foreach(bool inner in new[]{true,false})
                {
                    float radius=inner?70.03f:73.97f;
                    // Split panel runs at real door margins instead of dropping whole bays:
                    // otherwise each entrance leaves several metres of bare collision wall.
                    var spans=new List<Vector2>{new Vector2(i*360f/bays,(i+1)*360f/bays)};
                    if(inner)foreach(var portal in StationAccessibleRoomAngles.Select(a=>new Vector2(a-1,a+1)).Append(new Vector2(ServiceAngle-1.35f,ServiceAngle+1.35f)))
                    {
                        var next=new List<Vector2>();
                        foreach(var span in spans)
                        {
                            if(portal.y<=span.x || portal.x>=span.y){next.Add(span);continue;}
                            if(portal.x>span.x)next.Add(new Vector2(span.x,portal.x));
                            if(portal.y<span.y)next.Add(new Vector2(portal.y,span.y));
                        }
                        spans=next;
                    }
                    foreach(var span in spans)
                    {
                        if(span.y-span.x<.05f)continue;
                        float mid=(span.x+span.y)*.5f;
                        MaintenanceKit(ring,"ENV_Wall_Standard_A",RingPoint(radius,mid,0),(inner?90:270)-mid,new Vector3(radius*(span.y-span.x)*Mathf.Deg2Rad/2,1,1));
                    }
                }
                if(!IsStationObservationAngle(angle))
                    foreach(float radius in new[]{71f,73f})MaintenanceKit(ring,"ENV_Ceiling_Standard_A",RingPoint(radius,angle,3),-angle,new Vector3(1,1,72*(360f/bays)*Mathf.Deg2Rad/2));
                var bay=RingGroup("Pipe Vault",ring);bay.SetPositionAndRotation(RingPoint(72,angle,0),Quaternion.Euler(0,-angle,0));
                foreach(float x in new[]{-1.55f,1.55f})
                {
                    Part("Vault Shoulder",bay,new Vector3(x,2.6f,0),new Vector3(.8f,.38f,3.1f),ArtLibrary.Get("M_Env_FrameDark"));
                    MaintenancePipe(bay,new Vector3(x,2.32f,-1.6f),new Vector3(x,2.32f,1.6f),.1f,pipe);
                    Part("Pipe Collar",bay,new Vector3(x,2.32f,0),new Vector3(.26f,.26f,.07f),ArtLibrary.Get("M_Env_TrimOrange"));
                }
                Part("Vault Rib",bay,new Vector3(0,2.52f,-1.5f),new Vector3(3.8f,.18f,.16f),ArtLibrary.Get("M_Env_FrameDark"));
            }
        }

        private static Material MaintenanceMaterial(string name,string source,Color tint)
        {
            string path=$"{ArtLibrary.Folder}/{name}.mat";
            var material=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(ArtLibrary.Get(source));UnityEditor.AssetDatabase.CreateAsset(material,path);}
            material.SetColor("_BaseColor",tint);Worn(material,.65f,.45f,.5f);
            UnityEditor.EditorUtility.SetDirty(material);return material;
        }

        private static void MaintenanceKit(Transform parent,string prefab,Vector3 position,float yaw,Vector3 scale)
        {
            var go=Kitp(parent,prefab,position.x,position.y,position.z,yaw);go.transform.localScale=scale;
            foreach(var collider in go.GetComponentsInChildren<Collider>())
            {collider.enabled=false;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(collider);}
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        }

        private static void MaintenancePipe(Transform parent,Vector3 a,Vector3 b,float radius,Material material)
        {
            var pipe=Part("Oxidised Utility Pipe",parent,(a+b)*.5f,new Vector3(radius*2,(b-a).magnitude*.5f,radius*2),material,PrimitiveType.Cylinder);
            pipe.transform.localRotation=Quaternion.FromToRotation(Vector3.up,b-a);
        }
    }
}
