using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private static Material StationFineFinish(string name,string source,Color tint,float tile,float rust)
        {
            var mat=MaintenanceMaterial(name,source,tint);
            mat.SetFloat("_TileSize",tile);
            mat.SetFloat("_GrimeAmount",.4f);
            mat.SetFloat("_GrimeHeight",.55f);
            mat.SetFloat("_GrimePatches",.24f);
            mat.SetFloat("_GrimeScale",3.5f);
            mat.SetFloat("_RustAmount",rust);
            mat.SetFloat("_RustScale",6);
            mat.SetFloat("_WearCrispness",.35f);
            mat.SetFloat("_EdgeWear",.18f);
            mat.SetFloat("_EdgeWidth",.018f);
            mat.SetFloat("_BumpScale",.65f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void BuildStationRoomFinish(Transform room,HashSet<Vector2Int> cells)
        {
            // Per-station variants leave the Den and imported kit materials untouched.
            var wall=StationFineFinish("Station_Service_Painted_Panels","M_PaintedMetal_Wall",new Color(.28f,.245f,.30f),1.2f,.08f);
            var deck=StationFineFinish("Station_Service_Deck_Plates","M_FloorPlate",new Color(.22f,.23f,.24f),1.25f,.12f);
            var ceiling=StationFineFinish("Station_Service_Ceiling","M_BrushedSilver",new Color(.23f,.24f,.25f),1,.06f);
            wall.SetFloat("_PanelSize",1.5f);wall.SetFloat("_SeamDarkness",.22f);
            foreach(var renderer in room.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.name=="Sealed Boundary" || renderer.name.Contains("Partition") || renderer.name=="Crawl Mouth Header")renderer.sharedMaterial=wall;
                else if(renderer.name=="Walkable Deck")renderer.sharedMaterial=deck;
                else if(renderer.name=="Sealed Ceiling")renderer.sharedMaterial=ceiling;
            }
            // Cover the whole actual footprint, including annexes, without projecting
            // a ceiling tile out through a concave room corner.
            int minX=cells.Min(c=>c.x),maxX=cells.Max(c=>c.x),minZ=cells.Min(c=>c.y),maxZ=cells.Max(c=>c.y);
            for(int x=minX;x<=maxX-3;x+=4)for(int z=minZ;z<=maxZ-3;z+=4)
                if(Enumerable.Range(0,4).All(dx=>Enumerable.Range(0,4).All(dz=>cells.Contains(new Vector2Int(x+dx,z+dz)))))
                    MaintenanceKit(room,"ENV_Ceiling_Standard_A",new Vector3(x*.5f+1,2.7f,z*.5f+1),0,Vector3.one);
            // Group collinear boundary runs so the skirting seals joints without
            // hundreds of independent half-metre decorative strips.
            var boundaries=room.GetComponentsInChildren<BoxCollider>().Where(c=>c.name=="Sealed Boundary").ToArray();
            foreach(var group in boundaries.GroupBy(c=>new {X=c.size.x*c.transform.localScale.x<.2f,Plane=Mathf.RoundToInt((c.size.x*c.transform.localScale.x<.2f?c.transform.localPosition.x:c.transform.localPosition.z)*100)}))
            {
                bool xWall=group.Key.X;
                var ordered=group.OrderBy(c=>xWall?c.transform.localPosition.z:c.transform.localPosition.x).ToArray();int start=0;
                for(int end=1;end<=ordered.Length;end++)
                {
                    if(end<ordered.Length && Vector3.Distance(ordered[end].transform.localPosition,ordered[end-1].transform.localPosition)<.51f)continue;
                    var center=(ordered[start].transform.localPosition+ordered[end-1].transform.localPosition)*.5f;center.y=.11f;
                    float length=(end-start)*.5f;
                    Part("Wall Foot Seal",room,center,new Vector3(xWall?.17f:length,.22f,xWall?length:.17f),ArtLibrary.Get("M_Env_FrameDark"));
                    start=end;
                }
            }
        }
    }
}
