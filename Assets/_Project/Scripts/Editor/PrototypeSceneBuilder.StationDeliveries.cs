using System.Linq;
using UnityEngine;
using VentsAndVending.Inventory;
using VentsAndVending.Interaction;
using VentsAndVending.Orders;

namespace VentsAndVending.Editor
{
    public static partial class PrototypeSceneBuilder
    {
        private static void BuildStationDeliveries()
        {
            var parent=RingGroup("Station Courier Delivery",_stationFrame.parent);
            const float bayAngle=15;
            var bay=RingGroup("Ring Delivery Collection Crate",parent);
            bay.SetPositionAndRotation(RingPoint(73.55f,bayAngle,0),Quaternion.Euler(0,90-bayAngle,0));
            Part("Delivery Crate",bay,new Vector3(0,.35f,0),new Vector3(.95f,.7f,.55f),StationMetal);
            var box=bay.gameObject.AddComponent<BoxCollider>();box.center=new Vector3(0,.35f,0);box.size=new Vector3(.95f,.7f,.55f);
            var inventory=bay.gameObject.AddComponent<InventoryComponent>();inventory.Configure("Ring delivery parcels",24,saveId:"station.delivery_bay");
            bay.gameObject.AddComponent<StorageContainer>();
            Part("Delivery Sign Back",bay,new Vector3(0,1.05f,0),new Vector3(1.4f,.4f,.08f),ArtLibrary.Get("M_Env_FrameDark"));
            StationLabel(bay,"COURIER DELIVERY BAY\nE: COLLECT PARCELS",new Vector3(0,1.05f,-.048f),0,.012f);
            var parcel=RingGroup("Delivered Parcel Visual",bay);parcel.localPosition=new Vector3(0,.86f,0);
            Part("Delivered Supply Package",parcel,Vector3.zero,new Vector3(.50f,.32f,.38f),ArtLibrary.Get("M_WorkshopOrange"));
            Part("Package Strap",parcel,new Vector3(0,.17f,0),new Vector3(.1f,.02f,.40f),StationMetal);
            var pad=RingGroup("Marked Ring Delivery Zone",parent);pad.SetPositionAndRotation(RingPoint(73.12f,bayAngle,.015f),bay.rotation);
            foreach(float x in new[]{-.85f,.85f})Part("Bay Floor Marking",pad,new Vector3(x,0,0),new Vector3(.055f,.018f,1.35f),ArtLibrary.Get("M_Env_TrimOrange"));
            foreach(float z in new[]{-.67f,.67f})Part("Bay Floor Marking",pad,new Vector3(0,0,z),new Vector3(1.75f,.018f,.055f),ArtLibrary.Get("M_Env_TrimOrange"));
            StationLight(bay,"Delivery Bay Beacon",new Vector3(0,1.1f,-.3f),new Color(1,.65f,.2f),4,2);
            var drone=RingGroup("Wheeled Supply Courier",parent);
            Part("Courier Chassis",drone,new Vector3(0,.39f,0),new Vector3(.70f,.42f,1.0f),StationMetal);
            Part("Courier Orange Fairing",drone,new Vector3(0,.63f,.18f),new Vector3(.64f,.12f,.55f),ArtLibrary.Get("M_WorkshopOrange"));
            foreach(float x in new[]{-.2f,.2f})Part("Courier Headlight",drone,new Vector3(x,.48f,.52f),new Vector3(.13f,.12f,.04f),ArtLibrary.Get("M_Env_TrimOrange"));
            var wheels=new Transform[4];int index=0;
            foreach(float x in new[]{-.4f,.4f})foreach(float z in new[]{-.34f,.34f})
            {
                var wheel=RingGroup("Courier Wheel",drone);wheel.localPosition=new Vector3(x,.18f,z);wheels[index++]=wheel;
                Part("Rubber Tyre",wheel,Vector3.zero,new Vector3(.34f,.07f,.34f),ArtLibrary.Get("M_Env_FrameDark"),PrimitiveType.Cylinder).transform.localRotation=Quaternion.Euler(0,0,90);
            }
            var cargo=RingGroup("Courier Cargo",drone);cargo.localPosition=new Vector3(0,.82f,-.15f);
            Part("Courier Parcel",cargo,Vector3.zero,new Vector3(.52f,.35f,.48f),ArtLibrary.Get("M_WorkshopOrange"));
            var body=drone.gameObject.AddComponent<BoxCollider>();body.center=new Vector3(0,.38f,0);body.size=new Vector3(.94f,.56f,1.05f);
            var rigidbody=drone.gameObject.AddComponent<Rigidbody>();rigidbody.isKinematic=true;rigidbody.useGravity=false;
            var route=Enumerable.Range(0,31).Select(i=>RingPoint(72.4f,45-i,0)).ToArray();
            drone.position=route[0];
            parent.gameObject.AddComponent<SupplyDeliveryService>().Configure(inventory,drone,cargo,parcel,wheels,route);
            StationLabel(_stationFrame,"RING DELIVERY BAY ->\nPC ORDERS / WAIT FOR WHEELED COURIER",new Vector3(0,2.35f,9.65f),0,.016f);
        }
    }
}
