using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using VentsAndVending.Interaction;
using VentsAndVending.Player;
using VentsAndVending.Tunnels;
using VentsAndVending.World;
using Object=UnityEngine.Object;

namespace VentsAndVending.Editor
{
    /// <summary>Batch-capable rebuild, visual captures and live-frame regression checks for the maintenance finish.</summary>
    [InitializeOnLoad]
    public static class StationMaintenanceReview
    {
        private const string Pending="VV.MaintenanceReview.Pending";
        private const string Report="Logs/MaintenanceFinishReview.txt";
        private static int _phase;
        private static double _since;
        static StationMaintenanceReview(){EditorApplication.update+=Update;}

        public static void BuildAndReview()
        {
            try
            {
                Directory.CreateDirectory("Logs");File.WriteAllText(Report,"Maintenance finish review\n");
                PrototypeSceneBuilder.BuildCompleteConcourseRing();
                var frame=GameObject.Find("Station_Service_Network").transform;
                Capture("Logs/MaintenanceHubFinish.png",frame.TransformPoint(new Vector3(0,1.65f,-4)),frame.TransformPoint(new Vector3(0,1.8f,4)));
                float a=26*Mathf.Deg2Rad;
                Capture("Logs/MaintenanceRingFinish.png",new Vector3(72*Mathf.Cos(a),1.65f,72*Mathf.Sin(a)),new Vector3(72*Mathf.Cos(36*Mathf.Deg2Rad),1.65f,72*Mathf.Sin(36*Mathf.Deg2Rad)));
                float previous=0;
                for(int i=0;i<=100;i++){float travel=SlidingDoorLogic.LeafTravel(i/100f);Check(travel>=previous-.00001f && travel>=0 && travel<=1.00001f,"Door curve monotonic at "+i,false);previous=travel;}
                Check(SlidingDoorLogic.LeafTravel(0)==0 && SlidingDoorLogic.LeafTravel(.14f)==0 && SlidingDoorLogic.LeafTravel(1)==1,"Unseal phase and exact closed/open endpoints");
                SessionState.SetBool(Pending,true);_phase=0;_since=EditorApplication.timeSinceStartup;
                EditorApplication.isPlaying=true;
            }
            catch(Exception exception){Fail(exception);}
        }

        private static void Update()
        {
            if(!SessionState.GetBool(Pending,false) || !EditorApplication.isPlaying || EditorApplication.isCompiling)return;
            try
            {
                var user=Object.FindAnyObjectByType<InteractionController>();
                if(user==null || Time.timeSinceLevelLoad<.5f)return;
                var move=user.GetComponent<PlayerMovement>();var den=GameObject.Find("L3_01_Den").transform;
                var door=den.GetComponentInChildren<SlidingDoor>();var frame=GameObject.Find("Station_Service_Network").transform;
                var fan=Object.FindAnyObjectByType<FanHazard>();var breaker=Object.FindAnyObjectByType<BreakerSwitch>();
                double elapsed=EditorApplication.timeSinceStartup-_since;
                switch(_phase)
                {
                    case 0:
                        Object.FindAnyObjectByType<PlayerCamera>().enabled=false;
                        Check(!GameObject.Find("Maintenance Architectural Finish").GetComponentsInChildren<Collider>().Any(c=>c.enabled),"Decorative panel collisions remain disabled after scene reload");
                        move.Teleport(door.transform.TransformPoint(new Vector3(0,0,1.1f)),door.transform.rotation);Next();break;
                    case 1:
                        if(elapsed<.7)return;Check(door.OpenAmount==0,"Den stays closed on proximity");door.Interact(user);Next();break;
                    case 2:
                        if(elapsed<1.2)return;Check(door.OpenAmount==1,"Den fully opens with interaction");move.Teleport(door.transform.position,door.transform.rotation);Next();break;
                    case 3:
                        if(elapsed<4.8)return;Check(door.OpenAmount==1,"Door holds open for doorway occupancy");move.Teleport(den.TransformPoint(new Vector3(0,0,-3)),den.rotation);Next();break;
                    case 4:
                        if(elapsed<5.4)return;Check(door.OpenAmount==0,"Door closes and latches after clear timer");
                        var circuit=Object.FindAnyObjectByType<LightCircuit>();circuit.SetOn(false);
                        Check(!GameObject.Find("Hub Worklight").GetComponent<Light>().enabled && GameObject.Find("Den Light").GetComponent<Light>().enabled,"Local light isolation preserves Den light");circuit.SetOn(true);
                        Check(fan.State==FanHazard.FanState.Running,"Fan starts running");breaker.Interact(user);Next();break;
                    case 5:
                        if(elapsed<.8)return;Check(!fan.Passable && fan.State==FanHazard.FanState.WindingDown,"Fan blocks during coast-down");Next();break;
                    case 6:
                        if(elapsed<8)return;Check(fan.Passable,"Stopped fan remains passable");
                        for(int i=0;i<144;i++)
                        {
                            float angle=i*2.5f*Mathf.Deg2Rad;var p=new Vector3(72*Mathf.Cos(angle),0,72*Mathf.Sin(angle));
                            Check(!Physics.CheckCapsule(p+Vector3.up*.4f,p+Vector3.up*1.6f,.3f,~0,QueryTriggerInteraction.Ignore),"Maintenance ring clearance sample "+i,false);
                        }
                        Check(true,"All 144 maintenance-ring clearance samples passed");
                        foreach(var local in new[]{new Vector3(-12,0,0),new Vector3(-14,0,-3),new Vector3(-14,0,-7)})
                        {
                            var p=frame.TransformPoint(local);Check(!Physics.CheckCapsule(p+Vector3.up*.3f,p+Vector3.up*.9f,.25f,~0,QueryTriggerInteraction.Ignore),"Fan/cache clearance "+local);
                        }
                        File.AppendAllText(Report,"PASS: live Play Mode checks complete\n");SessionState.SetBool(Pending,false);EditorApplication.Exit(0);break;
                }
            }
            catch(Exception exception){Fail(exception);}
        }
        private static void Next(){_phase++;_since=EditorApplication.timeSinceStartup;}
        private static void Check(bool passed,string message,bool log=true)
        {if(!passed)throw new InvalidOperationException(message);if(log)File.AppendAllText(Report,"PASS: "+message+"\n");}
        private static void Fail(Exception exception)
        {SessionState.SetBool(Pending,false);Debug.LogException(exception);File.AppendAllText(Report,"FAIL: "+exception+"\n");EditorApplication.Exit(1);}
        private static void Capture(string path,Vector3 position,Vector3 lookAt)
        {
            var go=new GameObject("Temporary Finish Review Camera");var camera=go.AddComponent<Camera>();
            var target=new RenderTexture(1280,720,24);Texture2D image=null;var old=RenderTexture.active;
            try
            {
                camera.transform.position=position;camera.transform.LookAt(lookAt);camera.fieldOfView=68;camera.targetTexture=target;camera.Render();
                RenderTexture.active=target;image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally{RenderTexture.active=old;if(image!=null)Object.DestroyImmediate(image);Object.DestroyImmediate(go);target.Release();Object.DestroyImmediate(target);}
        }
    }
}
