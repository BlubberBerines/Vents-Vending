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
        private const string StationMeshFolder="Assets/_Project/Art/Environment/StationNetworkMeshes";
        private static int _stationMeshSequence;

        private static Mesh StationMesh(string name,Vector3[] vertices,int[] triangles)
        {
            EnsureFolder(StationMeshFolder);
            // Flat, separate face vertices also preserve correct normals on double-sided duct shells.
            var expanded=triangles.Select(i=>vertices[i]).ToArray();
            var mesh=new Mesh { name=name,vertices=expanded,triangles=Enumerable.Range(0,expanded.Length).ToArray() };mesh.RecalculateNormals();mesh.RecalculateBounds();
            string path=$"{StationMeshFolder}/{_stationMeshSequence++:0000}_{name.Replace(' ','_')}.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing!=null){EditorUtility.CopySerialized(mesh,existing);UnityEngine.Object.DestroyImmediate(mesh);mesh=existing;}
            else AssetDatabase.CreateAsset(mesh,path);
            return mesh;
        }

        private static GameObject StationMeshObject(Transform parent,string name,Vector3[] v,int[] triangles,Material mat)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer),typeof(MeshCollider));go.transform.SetParent(parent,false);
            var mesh=StationMesh(name,v,triangles);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshCollider>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=mat;return go;
        }

        private static void StationWedge(Transform parent,string name,float inner,float outer,float low,float high,float a,float b)
        {
            var v=new[]{RingPoint(inner,a,low),RingPoint(outer,a,low),RingPoint(outer,b,low),RingPoint(inner,b,low),RingPoint(inner,a,high),RingPoint(outer,a,high),RingPoint(outer,b,high),RingPoint(inner,b,high)};
            var t=new[]{0,2,1,0,3,2,4,5,6,4,6,7,0,1,5,0,5,4,1,2,6,1,6,5,2,3,7,2,7,6,3,0,4,3,4,7};
            StationMeshObject(parent,name,v,t.Reverse().ToArray(),name.Contains("Wall")?StationWall:name=="Ceiling"?ArtLibrary.Get("M_Env_FrameDark"):StationMetal);
        }

        private static void StationRingGrate(Transform parent,float inner,float outer,float y,float a,float b)
        {
            float mid=(a+b)*.5f,r=(inner+outer)*.5f,length=r*(b-a)*Mathf.Deg2Rad;
            var grate=RingGroup("See Through Inspection Grille",parent);grate.SetPositionAndRotation(RingPoint(r,mid,y),Quaternion.Euler(0,-mid,0));
            float width=outer-inner;
            for(float z=-length*.5f;z<=length*.5f+.001f;z+=.15f)
                StationLocalBox(grate,"Grille Bar",new Vector3(0,0,z),new Vector3(width,.045f,.035f),StationMetal);
            foreach(float x in new[]{-width*.5f,width*.5f})StationLocalBox(grate,"Grille Frame",new Vector3(x,0,0),new Vector3(.06f,.06f,length+.05f),StationMetal);
            var collider=grate.gameObject.AddComponent<BoxCollider>();collider.size=new Vector3(width,.045f,length+.05f);
        }

        private static GameObject StationPublicGrate(Transform parent,Vector3 position,float yaw)
        {
            var grate=RingGroup("L1 See Through Floor Vent",parent);grate.SetPositionAndRotation(position,Quaternion.Euler(0,yaw,0));
            for(float z=-.95f;z<=1;z+=.15f)StationLocalBox(grate,"Inspection Grille Bar",new Vector3(0,-.025f,z),new Vector3(2,.05f,.035f),StationMetal);
            foreach(float x in new[]{-.98f,.98f})StationLocalBox(grate,"Inspection Border",new Vector3(x,-.025f,0),new Vector3(.04f,.05f,2),StationMetal);
            var box=grate.gameObject.AddComponent<BoxCollider>();box.center=Vector3.down*.025f;box.size=new Vector3(2,.05f,2);
            return grate.gameObject;
        }

        private static void StationTube(Transform parent,string name,Vector3[] path,float width,float height,bool crawl)
        {
            var v=new List<Vector3>();var t=new List<int>();
            // Shared mitered cross sections keep bends closed rather than intersecting rectangular boxes.
            for(int i=0;i<path.Length;i++)
            {
                Vector3 previous=i==0?path[1]-path[0]:path[i]-path[i-1];previous.y=0;previous.Normalize();
                Vector3 next=i==path.Length-1?previous:path[i+1]-path[i];next.y=0;next.Normalize();
                Vector3 p=Vector3.Cross(Vector3.up,previous),n=Vector3.Cross(Vector3.up,next);
                var miter=(p+n).normalized;float scale=width*.5f/Mathf.Max(.5f,Vector3.Dot(miter,n));
                var side=miter*scale;
                v.Add(path[i]-side);v.Add(path[i]+side);v.Add(path[i]+side+Vector3.up*height);v.Add(path[i]-side+Vector3.up*height);
            }
            for(int i=0;i<path.Length-1;i++)for(int side=0;side<4;side++)
            {
                int a=i*4+side,b=i*4+(side+1)%4,c=b+4,d=a+4;
                t.AddRange(new[]{a,b,c,a,c,d,c,b,a,d,c,a}); // render both inside and outside the shell
            }
            StationMeshObject(parent,name,v.ToArray(),t.ToArray(),StationMetal);
            for(int i=0;i<path.Length-1;i++)
            {
                var delta=path[i+1]-path[i];int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.35f));
                if(crawl)
                {
                    var zone=RingGroup(name+" Crawl Volume",parent);zone.localPosition=(path[i]+path[i+1])*.5f+Vector3.up*height*.5f;zone.localRotation=Quaternion.LookRotation(delta);
                    var box=zone.gameObject.AddComponent<BoxCollider>();box.size=new Vector3(width+.12f,height+.25f,delta.magnitude+.3f);box.isTrigger=true;zone.gameObject.AddComponent<VentZone>();
                }
                for(int j=1;j<steps;j++)
                {
                    var point=parent.TransformPoint(Vector3.Lerp(path[i],path[i+1],j/(float)steps)+Vector3.up*(crawl?.4f:1));
                    (crawl?StationCrawlAudit:StationWalkAudit).Add(point);
                }
                StationLight(parent,name+" Lamp",(path[i]+path[i+1])*.5f+Vector3.up*(height*.65f),crawl?new Color(1,.7f,.4f):new Color(.5f,.8f,1),crawl?6:10,crawl?1.5f:9);
            }
        }

        private static void StationCrawlVolume(Transform parent,Vector3 center,Vector3 size)
        {var go=new GameObject("Crawl Junction Zone");go.transform.SetParent(parent,false);go.transform.localPosition=center;var b=go.AddComponent<BoxCollider>();b.size=size;b.isTrigger=true;go.AddComponent<VentZone>();}

        private static VentHatch StationHatch(Transform parent,string name,Vector3 foot,Vector3 normal,float width,float height)
        {
            var root=RingGroup(name,parent);root.localPosition=foot;root.localRotation=Quaternion.LookRotation(normal);
            var pivot=RingGroup("Hinge",root);pivot.localPosition=Vector3.up*height;
            for(float x=-width*.5f+.04f;x<width*.5f;x+=.15f)
                StationLocalBox(pivot,"Grille Slat",new Vector3(x,-height*.5f,0),new Vector3(.04f,height,.05f),StationMetal);
            foreach(float y in new[]{0f,-height})StationLocalBox(pivot,"Grille Rail",new Vector3(0,y,0),new Vector3(width,.055f,.065f),ArtLibrary.Get("M_Env_TrimOrange"));
            var blocker=pivot.gameObject.AddComponent<BoxCollider>();blocker.center=new Vector3(0,-height*.5f,0);blocker.size=new Vector3(width,height,.07f);
            var hatch=root.gameObject.AddComponent<VentHatch>();var so=new SerializedObject(hatch);
            so.FindProperty("_pivot").objectReferenceValue=pivot;so.FindProperty("_openRotation").vector3Value=new Vector3(110,0,0);so.FindProperty("_sound").objectReferenceValue=ProceduralAudio.Clunk;so.ApplyModifiedPropertiesWithoutUndo();
            return hatch;
        }

        private static void StationRect(HashSet<Vector2Int> cells,float x0,float x1,float z0,float z1)
        {for(int x=Mathf.RoundToInt(x0*2);x<Mathf.CeilToInt(x1*2);x++)for(int z=Mathf.RoundToInt(z0*2);z<Mathf.CeilToInt(z1*2);z++)cells.Add(new Vector2Int(x,z));}

        private static void StationCellSpace(Transform parent,HashSet<Vector2Int> cells,float floor,float height,Func<Vector3,bool> floorHole,Func<Vector3,bool> roofHole,Func<Vector3,Vector3,bool> opening)
        {
            foreach(bool roof in new[]{false,true})
            {
                var remaining=new HashSet<Vector2Int>(cells.Where(c=>!(roof?roofHole:floorHole)?.Invoke(new Vector3(c.x*.5f+.25f,0,c.y*.5f+.25f))??true));
                while(remaining.Count>0)
                {
                    var start=remaining.OrderBy(c=>c.y).ThenBy(c=>c.x).First();int w=1,h=1;
                    while(remaining.Contains(start+new Vector2Int(w,0)))w++;
                    while(Enumerable.Range(0,w).All(dx=>remaining.Contains(start+new Vector2Int(dx,h))))h++;
                    for(int dx=0;dx<w;dx++)for(int dz=0;dz<h;dz++)remaining.Remove(start+new Vector2Int(dx,dz));
                    StationLocalBox(parent,roof?"Sealed Ceiling":"Walkable Deck",new Vector3(start.x*.5f+w*.25f,floor+(roof?height+.06f:-.06f),start.y*.5f+h*.25f),new Vector3(w*.5f,.12f,h*.5f),roof?StationWall:StationMetal);
                }
            }
            foreach(var c in cells)foreach(var offset in new[]{Vector2Int.right,Vector2Int.left,Vector2Int.up,Vector2Int.down})
            {
                if(cells.Contains(c+offset))continue;
                var n=new Vector3(offset.x,0,offset.y);var p=new Vector3(c.x*.5f+.25f,0,c.y*.5f+.25f)+n*.25f;
                if(opening!=null && opening(p,n))continue;
                StationLocalBox(parent,"Sealed Boundary",p+n*.06f+Vector3.up*(floor+height*.5f),new Vector3(offset.x==0?.5f:.12f,height,offset.x==0?.12f:.5f),StationWall);
            }
        }

        private static void StationPartition(Transform parent,float x,float z0,float z1,string label)
        {
            float center=(z0+z1)*.5f;
            foreach(var range in new[]{new Vector2(z0,center-1),new Vector2(center+1,z1)})
                if(range.y>range.x)StationLocalBox(parent,"Room Partition",new Vector3(x,1.5f,(range.x+range.y)*.5f),new Vector3(.15f,3,range.y-range.x),StationWall);
            StationLocalBox(parent,"Room Door Header",new Vector3(x,2.65f,center),new Vector3(.15f,.7f,2),StationWall);
            StationLabel(parent,label,new Vector3(x+(x<0?.15f:-.15f),2.5f,center),x<0?-90:90,.025f);
        }
    }
}
