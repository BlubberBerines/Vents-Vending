using System;
using System.Collections.Generic;
using UnityEngine;
using VentsAndVending.Crafting;
using VentsAndVending.Inventory;

namespace VentsAndVending.Customers
{
    [Serializable] public sealed class CustomerRecord
    {
        public string Name,DefinitionId;
        public bool Recruited;
        public int Trust;
        public List<string> TriedSamples=new List<string>();
        public List<string> LikedProducts=new List<string>();
        public List<string> DislikedProducts=new List<string>();
    }

    /// <summary>Named passengers become known through conversation and regulars through a liked sample.</summary>
    public sealed class CustomerLedger
    {
        private readonly List<CustomerRecord> _records=new List<CustomerRecord>();
        public IReadOnlyList<CustomerRecord> Records=>_records;
        public event Action Changed;
        public CustomerRecord Find(string name)=>_records.Find(r=>r.Name==name);
        public CustomerRecord Meet(string name,string definitionId)
        {
            var known=Find(name);if(known!=null)return known;
            known=new CustomerRecord{Name=name,DefinitionId=definitionId};_records.Add(known);Changed?.Invoke();return known;
        }
        public static bool LikesProduct(string name,string definitionId,string productId)
        {
            uint hash=2166136261;foreach(char c in name+"/"+definitionId+"/"+productId)hash=unchecked((hash^c)*16777619);
            return hash%4!=0;
        }
        public bool OfferSample(CustomerDefinition definition,string name,ProductDefinition product,ItemQuality quality,InventoryModel pockets,out string reply)
        {
            reply="Carry a finished product to offer a sample.";
            if(definition==null || product==null || pockets==null)return false;
            var record=Meet(name,definition.Id);string key=product.Id+":"+(int)quality;
            if(record.TriedSamples.Contains(key) || record.LikedProducts.Contains(product.Id) || record.DislikedProducts.Contains(product.Id))
            {reply="I've already tried this product. Let's try something different another time.";return false;}
            if(pockets.Count(product,quality)<1)return false;
            pockets.Remove(product,1,quality);record.TriedSamples.Add(key);
            if(quality<definition.MinimumQuality && !definition.LikesPoorQuality)
                reply="Too rough for me. Bring a better-quality batch and I'll give it another chance.";
            else if(LikesProduct(name,definition.Id,product.Id))
            {
                record.LikedProducts.Add(product.Id);record.Recruited=true;record.Trust=Math.Min(100,record.Trust+25);
                reply="That's good. Add me on VentNet — I'll be back for more. You've recruited a regular customer.";
            }
            else
            {record.DislikedProducts.Add(product.Id);reply="Not my taste. I'd try a different product another day. You can still talk to me.";}
            Changed?.Invoke();return true;
        }
        public void AdjustTrust(string name,int amount)
        {var record=Find(name);if(record==null)return;record.Trust=Mathf.Clamp(record.Trust+amount,0,100);Changed?.Invoke();}
        public void Restore(IEnumerable<CustomerRecord> records)
        {
            _records.Clear();
            foreach(var record in records??Array.Empty<CustomerRecord>())
            {
                if(record==null || string.IsNullOrEmpty(record.Name) || string.IsNullOrEmpty(record.DefinitionId) || Find(record.Name)!=null)continue;
                record.TriedSamples??=new List<string>();record.LikedProducts??=new List<string>();record.DislikedProducts??=new List<string>();record.Trust=Mathf.Clamp(record.Trust,0,100);_records.Add(record);
            }
            Changed?.Invoke();
        }
    }
}
