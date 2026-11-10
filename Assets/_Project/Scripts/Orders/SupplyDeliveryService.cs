using UnityEngine;
using VentsAndVending.Economy;
using VentsAndVending.Inventory;
using VentsAndVending.Interaction;

namespace VentsAndVending.Orders
{
    public sealed class SupplyDeliveryService : MonoBehaviour
    {
        [SerializeField] private InventoryComponent _dropBox;
        [SerializeField] private Transform _drone,_cargo,_parcel;
        [SerializeField] private Transform[] _wheels;
        [SerializeField] private Vector3[] _route;
        private readonly Collider[] _obstacles=new Collider[24];
        private float[] _lengths;
        private float _totalLength;
        private bool _trafficBlocked;
        private Vector3 _cargoHome;
        public static SupplyDeliveryService Instance {get;private set;}
        public DeliveryQueue Queue {get;}=new DeliveryQueue();
        public InventoryComponent DropBox=>_dropBox;
        public string WindowText=>Window(Queue.NextWindowSeconds);
        private static string Window(float seconds)=>$"{Mathf.Max(1,Mathf.CeilToInt(seconds-3))}-{Mathf.CeilToInt(seconds+9)}s";
        public string StatusText=>Queue.DropBlocked?"Courier waiting at bay: clear space in the delivery crate. Paid goods are safe."
            :_trafficBlocked?"Courier waiting for a clear lane. Your delivery is delayed, not lost."
            :Queue.Phase==DeliveryPhase.Outbound||Queue.Phase==DeliveryPhase.Unloading?$"Courier arriving in {Window(Queue.ActiveArrivalSeconds)}. Collect parcels at the marked ring bay outside the Den approach."
            :Queue.Orders.Count>0?$"Next courier window: {WindowText}. Collect at the marked ring delivery bay."
            :$"Next order window: {WindowText}. Wheeled courier -> ring delivery bay. Not the Den cupboard.";
        internal void Configure(InventoryComponent box,Transform drone,Transform cargo,Transform parcel,Transform[] wheels,Vector3[] route)
        {_dropBox=box;_drone=drone;_cargo=cargo;_parcel=parcel;_wheels=wheels;_route=route;}
        private void Awake()
        {
            Instance=this;_lengths=new float[_route.Length];if(_cargo!=null)_cargoHome=_cargo.localPosition;
            for(int i=1;i<_route.Length;i++){_totalLength+=Vector3.Distance(_route[i-1],_route[i]);_lengths[i]=_totalLength;}
        }
        private void OnDestroy(){if(Instance==this)Instance=null;}
        public PurchaseResult Order(SupplyListing listing,Wallet wallet)=>Queue.Order(listing,wallet,_dropBox.Model);
        public PurchaseResult Checkout(System.Collections.Generic.IReadOnlyList<SupplyListing> cart,Wallet wallet)=>Queue.OrderCart(cart,wallet,_dropBox.Model);
        private Vector3 RoutePoint(float progress,out Vector3 forward)
        {
            float distance=Mathf.Clamp01(progress)*_totalLength;
            for(int i=1;i<_route.Length;i++)if(distance<=_lengths[i])
            {forward=(_route[i]-_route[i-1]).normalized;return Vector3.Lerp(_route[i-1],_route[i],Mathf.InverseLerp(_lengths[i-1],_lengths[i],distance));}
            forward=(_route[_route.Length-1]-_route[_route.Length-2]).normalized;return _route[_route.Length-1];
        }
        private float Progress=>Queue.Phase==DeliveryPhase.Outbound?1-Queue.Remaining/DeliveryQueue.TravelSeconds:
            Queue.Phase==DeliveryPhase.Returning?Queue.Remaining/DeliveryQueue.TravelSeconds:Queue.Phase==DeliveryPhase.Unloading?1:0;
        private void Update()
        {
            if(_drone==null || _dropBox==null || _route.Length<2)return;
            bool moving=Queue.Phase==DeliveryPhase.Outbound||Queue.Phase==DeliveryPhase.Returning;
            _trafficBlocked=false;
            if(moving)
            {
                float step=Time.deltaTime/DeliveryQueue.TravelSeconds*(Queue.Phase==DeliveryPhase.Returning?-1:1);
                var next=RoutePoint(Progress+step,out var direction);
                int count=Physics.OverlapBoxNonAlloc(next+Vector3.up*.38f,new Vector3(.48f,.3f,.56f),_obstacles,Quaternion.LookRotation(direction),~0,QueryTriggerInteraction.Ignore);
                for(int i=0;i<count;i++)if(!_obstacles[i].transform.IsChildOf(_drone)){_trafficBlocked=true;break;}
            }
            int delivered=Queue.DeliveredBatches;
            Queue.Tick(_trafficBlocked?0:Time.deltaTime,_dropBox.Model);
            var position=RoutePoint(Progress,out var tangent);
            float travelled=Vector3.Distance(_drone.position,position);
            _drone.SetPositionAndRotation(position,Quaternion.LookRotation(Queue.Phase==DeliveryPhase.Returning?-tangent:tangent));
            if(_wheels!=null)foreach(var wheel in _wheels)wheel.Rotate(Vector3.right,travelled*360/(Mathf.PI*.34f),Space.Self);
            if(_cargo!=null)
            {
                _cargo.gameObject.SetActive(Queue.Phase==DeliveryPhase.Outbound||Queue.Phase==DeliveryPhase.Unloading);
                if(Queue.Phase==DeliveryPhase.Unloading && _parcel!=null)
                    _cargo.position=Vector3.Lerp(_drone.TransformPoint(_cargoHome),_parcel.position,1-Queue.Remaining/DeliveryQueue.UnloadSeconds);
                else _cargo.localPosition=_cargoHome;
            }
            if(_parcel!=null)_parcel.gameObject.SetActive(!_dropBox.Model.IsEmpty);
            if(delivered!=Queue.DeliveredBatches)FindAnyObjectByType<InteractionController>()?.Notify("Courier drop complete. Collect your order at the ring delivery bay.");
        }
    }
}
