using System.Collections.Generic;
using System.Linq;
using VentsAndVending.Inventory;

namespace VentsAndVending.Orders
{
    /// <summary>Unpaid draft. Kept when the PC closes; only checkout books a courier manifest.</summary>
    public sealed class SupplyCart
    {
        private readonly List<SupplyListing> _items=new List<SupplyListing>();
        public IReadOnlyList<SupplyListing> Items=>_items;
        public int Count=>_items.Count;
        public long Total=>_items.Sum(l=>(long)l.Price);
        public bool Add(SupplyListing listing)
        {
            if(listing.Item==null || listing.Price<0 || Count>=DeliveryQueue.MaxOrders)return false;
            _items.Add(listing);return true;
        }
        public void Remove(ItemDefinition item)
        {int index=_items.FindLastIndex(l=>l.Item==item);if(index>=0)_items.RemoveAt(index);}
        public void Clear()=>_items.Clear();
    }
}
