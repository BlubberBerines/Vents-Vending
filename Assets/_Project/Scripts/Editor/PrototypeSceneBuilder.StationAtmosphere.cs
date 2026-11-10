using System.Linq;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Customers;
using VentsAndVending.Security;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildStationCrowd(CustomerDefinition[] customers,Transform firstEntry)
        {
            var second=RingGroup("East Passenger Arrival",_stationFrame);second.position=RingPoint(71,26,4.65f);
            var destinations=new System.Collections.Generic.List<Transform>();
            foreach(var p in new[]{new Vector3(-2.3f,4.6f,-3.6f),new Vector3(2.3f,4.6f,-3.8f),new Vector3(-2.4f,4.6f,1),new Vector3(2.4f,4.6f,2),new Vector3(0,4.6f,3.8f)})
            {var t=RingGroup("Passenger Lounge Pause",_stationLounge);t.localPosition=p;destinations.Add(t);}
            foreach(float angle in new[]{6f,10,18,23})
            {var t=RingGroup("Passenger Concourse Pause",_stationFrame);t.position=RingPoint(73,angle,4.65f);destinations.Add(t);}
            var definitions=customers.Concat(new[]{_blorbDefinition,_gromflomiteDefinition}).Distinct().ToArray();
            RingGroup("Station Passenger Traffic",_stationFrame).gameObject.AddComponent<StationCrowd>()
                .Configure(definitions,new[]{firstEntry,second},destinations.ToArray());
        }

        private static void BuildStationAtmosphere()
        {
            var dressing=RingGroup("Maintenance Atmosphere",_stationFrame.parent);
            string[] props={"PROP_server_rack","PROP_Tool_Cart","PROP_Crate_Stack","PROP_Locker","PROP_Generator","PROP_Coolant_Tank"};
            for(int sector=0;sector<24;sector++)
            {
                float angle=sector*15+7.5f;
                var bay=RingGroup($"Service Bay {sector+1:00}",dressing);
                var prop=Prop(bay,Props+"/"+props[sector%props.Length]+".prefab",0,0,0,180);
                if(prop!=null)prop.transform.localScale*=.65f;
                foreach(float z in new[]{-5f,5f})
                {
                    var equipment=Prop(bay,Props+"/"+props[(sector+(z<0?2:4))%props.Length]+".prefab",0,0,z,180);
                    if(equipment!=null)equipment.transform.localScale*=.65f;
                    Part("Equipment Hazard Pad",bay,new Vector3(.1f,.008f,z),new Vector3(1.1f,.015f,2),ArtLibrary.Get("M_Env_TrimOrange"));
                }
                // Wall-side bays preserve a continuous central walking lane and every existing portal.
                StationLocalBox(bay,"Utility Pipe",new Vector3(0,2.65f,0),new Vector3(.12f,.12f,11),StationMetal);
                StationLocalBox(bay,"Service Wall Panel",new Vector3(-.26f,1.3f,0),new Vector3(.08f,1.1f,1.6f),StationWall);
                StationLight(bay,"Equipment Worklight",new Vector3(.4f,2.4f,2),new Color(1,.64f,.3f),6,4);
                StationLabel(bay,$"MAINT {sector+1:00}\n"+(sector%3==0?"COOLANT / PRESSURE":"SERVICE ACCESS"),new Vector3(.2f,1.9f,0),90,.018f);
                bay.SetPositionAndRotation(RingPoint(70.45f,angle,0),Quaternion.Euler(0,-angle,0));
            }
            var hub=_stationFrame.Find("L3_02_Service_Junction_and_Side_Rooms");
            Prop(hub,Props+"/PROP_Tool_Cart.prefab",-3.4f,0,4.6f,90);
            Prop(hub,Props+"/PROP_Crate_Stack.prefab",8.5f,0,-5.1f,0);
            Prop(hub,Props+"/PROP_Fire_Extinguisher.prefab",-3.5f,0,-4.8f,0);
            var points=new[]{new Vector3(-2,4.6f,3),new Vector3(-2,4.6f,-3.8f),new Vector3(2,4.6f,-3.8f),new Vector3(2,4.6f,3),new Vector3(0,4.6f,5)};
            BuildStationPublicDrone("Lounge Security Patrol",points.Select(p=>_stationFrame.TransformPoint(p)).ToArray());
            BuildStationPublicDrone("Concourse Security Patrol",new[]{5f,10,15,20,25,20,15,10}.Select(a=>RingPoint(72,a,4.6f)).ToArray());
        }

        private static void BuildStationPublicDrone(string name,Vector3[] points)
        {
            var bounds=new Bounds(points[0],Vector3.zero);foreach(var point in points)bounds.Encapsulate(point);bounds.Expand(3);
            BuildDrone(name,2.25f,points.Select(p=>new Stop(p.x,p.z,2,65)).ToArray(),new (int from,Stop[] points,float chance)[0],bounds,false,4.6f);
            GameObject.Find(name).transform.SetParent(_stationFrame,true);GameObject.Find(name+" Route").transform.SetParent(_stationFrame,true);
        }

        private static void BuildStationPortrait(CustomerDefinition definition)
        {
            int count=Mathf.Max(1,definition.BodyColors.Length);
            var portraits=new Sprite[count];
            for(int i=0;i<count;i++)portraits[i]=BuildStationPortraitVariant(definition,i);
            var so=new SerializedObject(definition);
            so.FindProperty("_portrait").objectReferenceValue=portraits[0];
            var variants=so.FindProperty("_portraits");variants.arraySize=count;
            for(int i=0;i<count;i++)variants.GetArrayElementAtIndex(i).objectReferenceValue=portraits[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite BuildStationPortraitVariant(CustomerDefinition definition,int variant)
        {
            const string folder="Assets/_Project/Art/UI/CustomerPortraits";EnsureFolder(folder);
            string path=folder+"/"+definition.Id+(variant==0?"":"_"+variant.ToString("00"))+".png";
            var model=Object.Instantiate(definition.Prefab);
            var cameraObject=new GameObject("Temporary Mugshot Camera");
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var target=new RenderTexture(384,384,24);
            Texture2D copy=null;
            var materials=new System.Collections.Generic.List<Material>();
            try
            {
                model.transform.position=new Vector3(1000,1000,1000);
                foreach(var child in model.GetComponentsInChildren<Transform>())child.gameObject.layer=31;
                var visual=model.GetComponentInChildren<CustomerVisual>();
                if(visual!=null && definition.BodyColors.Length>0)visual.ApplyTint(definition.BodyColors[variant],definition.BodyGlow);
                foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>())
                {
                    if(renderer.GetComponent<TMPro.TMP_Text>()!=null)continue;
                    var block=new MaterialPropertyBlock();renderer.GetPropertyBlock(block);
                    var source=renderer.sharedMaterial;
                    Color color=block.HasColor("_BaseColor")?block.GetColor("_BaseColor"):source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):source.color;
                    var material=new Material(Shader.Find("Universal Render Pipeline/Unlit"));material.SetColor("_BaseColor",color);materials.Add(material);renderer.sharedMaterial=material;
                }
                var bounds=new Bounds(model.transform.position+Vector3.up,.1f*Vector3.one);
                foreach(var r in model.GetComponentsInChildren<MeshRenderer>())if(r.GetComponent<TMPro.TMP_Text>()==null)bounds.Encapsulate(r.bounds);
                var focus=bounds.center+Vector3.up*bounds.extents.y*.32f;
                camera.orthographic=true;camera.orthographicSize=bounds.size.y*.38f;
                camera.transform.position=focus+new Vector3(0,.05f,Mathf.Max(1.4f,bounds.size.y*.8f));
                camera.transform.LookAt(focus);camera.fieldOfView=35;camera.cullingMask=1<<31;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.1f,.14f);
                camera.targetTexture=target;camera.Render();var old=RenderTexture.active;
                try{RenderTexture.active=target;copy=new Texture2D(384,384,TextureFormat.RGB24,false);copy.ReadPixels(new Rect(0,0,384,384),0,0);copy.Apply();}
                finally{RenderTexture.active=old;}
                System.IO.File.WriteAllBytes(path,copy.EncodeToPNG());
            }
            finally{if(copy!=null)Object.DestroyImmediate(copy);Object.DestroyImmediate(model);Object.DestroyImmediate(cameraObject);target.Release();Object.DestroyImmediate(target);foreach(var material in materials)Object.DestroyImmediate(material);}
            AssetDatabase.ImportAsset(path);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
