using System;
using System.Collections.Generic;
using System.Linq;
using VentsAndVending.Economy;
using VentsAndVending.Inventory;

namespace VentsAndVending.Orders
{
    public enum DeliveryPhase { Waiting, Outbound, Unloading, Returning }
    [Serializable] public sealed class SupplyOrder
    {
        public string ItemId;
        public int Price;
        public bool InTransit;
    }
    [Serializable] public sealed class DeliverySave
    {
        public DeliveryPhase Phase;
        public float Remaining = DeliveryQueue.DispatchDelay;
        public List<SupplyOrder> Orders = new List<SupplyOrder>();
    }

    /// <summary>One physical courier, with reserved drop-box space and a saved manifest.</summary>
    public sealed class DeliveryQueue
    {
        public const float DispatchDelay=25, TravelSeconds=18, UnloadSeconds=3;
        public const int MaxOrders=12;
        private readonly List<SupplyOrder> _orders=new List<SupplyOrder>();
        public IReadOnlyList<SupplyOrder> Orders=>_orders;
        public DeliveryPhase Phase {get; private set;}
        public float Remaining {get; private set;}=DispatchDelay;
        public bool DropBlocked {get; private set;}
        public int DeliveredBatches {get; private set;}
        public float NextWindowSeconds => Phase switch
        {
            DeliveryPhase.Waiting=>Remaining+TravelSeconds+UnloadSeconds,
            DeliveryPhase.Outbound=>Remaining+UnloadSeconds+TravelSeconds+DispatchDelay+TravelSeconds+UnloadSeconds,
            DeliveryPhase.Unloading=>Remaining+TravelSeconds+DispatchDelay+TravelSeconds+UnloadSeconds,
            _=>Remaining+DispatchDelay+TravelSeconds+UnloadSeconds
        };
        public float ActiveArrivalSeconds=>Phase==DeliveryPhase.Outbound?Remaining+UnloadSeconds:Phase==DeliveryPhase.Unloading?Remaining:NextWindowSeconds;
        public PurchaseResult Order(SupplyListing listing,Wallet wallet,InventoryModel drop)
        {
            if(listing.Item==null || wallet==null || drop==null || listing.Price<0)return PurchaseResult.QueueFull;
            if(_orders.Count>=MaxOrders)return PurchaseResult.QueueFull;
            int reserved=_orders.Count(o=>o.ItemId==listing.Item.Id);
            if(drop.SpaceFor(listing.Item)<=reserved)return PurchaseResult.LockerFull;
            // Space shared by different item types must also be reserved; simulate the complete manifest.
            var capacity=new InventoryModel(drop.Capacity);
            foreach(var stack in drop.Slots)if(!stack.IsEmpty)capacity.Add(stack.Item,stack.Count,stack.Quality);
            foreach(var order in _orders)
            {
                var item=_definitions.TryGetValue(order.ItemId,out var known)?known:null;
                if(item==null || capacity.Add(item,1)>0)return PurchaseResult.LockerFull;
            }
            if(!capacity.CanAdd(listing.Item,1))return PurchaseResult.LockerFull;
            if(!wallet.TrySpend(listing.Price))return PurchaseResult.CannotAfford;
            _definitions[listing.Item.Id]=listing.Item;
            _orders.Add(new SupplyOrder{ItemId=listing.Item.Id,Price=listing.Price});
            return PurchaseResult.OrderQueued;
        }
        private readonly Dictionary<string,ItemDefinition> _definitions=new Dictionary<string,ItemDefinition>();
        public void Tick(float seconds,InventoryModel drop)
        {
            if(seconds<=0 || drop==null)return;
            float dt=seconds;
            for(int guard=0;guard<20;guard++)
            {
                if(Phase==DeliveryPhase.Waiting && _orders.Count==0)return;
                float used=Math.Min(dt,Remaining);Remaining-=used;dt-=used;
                if(Remaining>0)return;
                switch(Phase)
                {
                    case DeliveryPhase.Waiting:
                        foreach(var order in _orders)order.InTransit=true;
                        Phase=DeliveryPhase.Outbound;Remaining=TravelSeconds;break;
                    case DeliveryPhase.Outbound:
                        Phase=DeliveryPhase.Unloading;Remaining=UnloadSeconds;break;
                    case DeliveryPhase.Unloading:
                        if(!TryDrop(drop)){DropBlocked=true;return;}
                        DropBlocked=false;Phase=DeliveryPhase.Returning;Remaining=TravelSeconds;DeliveredBatches++;break;
                    case DeliveryPhase.Returning:
                        Phase=DeliveryPhase.Waiting;Remaining=DispatchDelay;break;
                }
                if(dt<=0)return;
            }
        }
        private bool TryDrop(InventoryModel drop)
        {
            var active=_orders.Where(o=>o.InTransit).ToArray();
            foreach(var group in active.GroupBy(o=>o.ItemId))
                if(!_definitions.TryGetValue(group.Key,out var item) || !drop.CanAdd(item,group.Count()))return false;
            // Check mixed-item capacity atomically as well; the player may have filled the bay since ordering.
            var check=new InventoryModel(drop.Capacity);
            foreach(var stack in drop.Slots)if(!stack.IsEmpty)check.Add(stack.Item,stack.Count,stack.Quality);
            foreach(var order in active)if(check.Add(_definitions[order.ItemId],1)>0)return false;
            foreach(var order in active)drop.Add(_definitions[order.ItemId],1);
            _orders.RemoveAll(o=>o.InTransit);return true;
        }
        public DeliverySave Capture()=>new DeliverySave{Phase=Phase,Remaining=Remaining,Orders=_orders.Select(o=>new SupplyOrder{ItemId=o.ItemId,Price=o.Price,InTransit=o.InTransit}).ToList()};
        public int Restore(DeliverySave save,Func<string,ItemDefinition> lookup)
        {
            _orders.Clear();_definitions.Clear();DropBlocked=false;DeliveredBatches=0;
            Phase=save!=null && Enum.IsDefined(typeof(DeliveryPhase),save.Phase)?save.Phase:DeliveryPhase.Waiting;
            float limit=Phase==DeliveryPhase.Waiting?DispatchDelay:Phase==DeliveryPhase.Unloading?UnloadSeconds:TravelSeconds;
            float remaining=save?.Remaining??DispatchDelay;
            Remaining=float.IsNaN(remaining)||float.IsInfinity(remaining)?limit:Math.Max(0,Math.Min(limit,remaining));
            int refund=0;
            foreach(var saved in save?.Orders??new List<SupplyOrder>())
            {
                var item=saved?.ItemId!=null?lookup(saved.ItemId):null;
                if(item==null || _orders.Count>=MaxOrders){refund+=Math.Max(0,saved?.Price??0);continue;}
                _definitions[item.Id]=item;
                _orders.Add(new SupplyOrder{ItemId=item.Id,Price=Math.Max(0,saved.Price),InTransit=Phase!=DeliveryPhase.Waiting && Phase!=DeliveryPhase.Returning && saved.InTransit});
            }
            // Never strand a corrupted/stale in-transit flag on a parked courier.
            if(Phase==DeliveryPhase.Outbound || Phase==DeliveryPhase.Unloading)
                if(!_orders.Any(o=>o.InTransit)){Phase=DeliveryPhase.Returning;Remaining=TravelSeconds;}
            return refund;
        }
    }
}
