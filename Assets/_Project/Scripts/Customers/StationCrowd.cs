using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace VentsAndVending.Customers
{
    /// <summary>Bounded ambient foot traffic. Uses the same bodies as customers without sales or order markers.</summary>
    public sealed class StationCrowd : MonoBehaviour
    {
        [SerializeField] private CustomerDefinition[] _definitions;
        [SerializeField] private Transform[] _entrances;
        [SerializeField] private Transform[] _destinations;
        [SerializeField] private int _population = 12;
        private readonly List<Customer> _visitors = new();
        private float _nextSpawn = 1;
        private static readonly string[] PassengerNames={"Nera Voss","Tavi Noll","Ollo Kes","Miri Venn","Zavi Tel","Rook Fen","Pella Wex","Daro Nim","Vela Pox","Orri Vale","Kova Ress","Lumi Tor","Tess Ulo","Wren Xal","Sela Mor","Deni Vos"};
        public int Population => _visitors.Count;
        public void ResetPassengers()
        {
            foreach(var visitor in _visitors)if(visitor!=null && visitor.IsAmbient){visitor.gameObject.SetActive(false);Destroy(visitor.gameObject);}
            _visitors.Clear();_nextSpawn=1;
        }
        internal void Configure(CustomerDefinition[] definitions,Transform[] entrances,Transform[] destinations)
        { _definitions=definitions;_entrances=entrances;_destinations=destinations; }
        private void Update()
        {
            _visitors.RemoveAll(c=>c==null || !c.IsAmbient);
            _nextSpawn-=Time.deltaTime;
            if(_nextSpawn>0 || _visitors.Count>=_population) return;
            _nextSpawn=Random.Range(3f,6f);
            if(_definitions==null || _definitions.Length==0 || _entrances.Length==0 || _destinations.Length==0)return;
            var entry=_entrances[Random.Range(0,_entrances.Length)];
            var destination=_destinations[Random.Range(0,_destinations.Length)];
            var exit=_entrances[Random.Range(0,_entrances.Length)];
            var path=new NavMeshPath();
            if(!NavMesh.CalculatePath(entry.position,destination.position,NavMesh.AllAreas,path) || path.status!=NavMeshPathStatus.PathComplete)return;
            var definition=_definitions[Random.Range(0,_definitions.Length)];
            string passengerName=null;
            foreach(var name in PassengerNames)if(!_visitors.Exists(c=>c!=null && c.DisplayName==name) && !(Orders.OrderManager.Instance?.HasPendingCustomer(name)??false)){passengerName=name;break;}
            if(passengerName==null)return;
            var profile=Orders.OrderManager.Instance?.Customers.Find(passengerName);
            if(profile!=null)definition=System.Array.Find(_definitions,d=>d.Id==profile.DefinitionId)??definition;
            var visitor=Instantiate(definition.Prefab,entry.position,entry.rotation).GetComponent<Customer>();
            visitor.name="Passenger "+definition.SpeciesName;
            visitor.BeginAmbient(definition,destination,exit,passengerName??"Traveller");
            _visitors.Add(visitor);
        }
        private void OnDestroy()
        {
            foreach(var visitor in _visitors)if(visitor!=null && visitor.IsAmbient)Destroy(visitor.gameObject);
        }
    }
}
