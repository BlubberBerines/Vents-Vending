using UnityEngine;
using VentsAndVending.Interaction;
using VentsAndVending.Tunnels;

namespace VentsAndVending.World
{
    /// <summary>Saved isolation valve plus optional non-damaging room-entry obstruction.</summary>
    public sealed class SteamLeak : MonoBehaviour,IInteractable
    {
        [SerializeField] private string _switchId;
        [SerializeField] private ParticleSystem _jet;
        [SerializeField] private AudioSource _hiss;
        [SerializeField] private Collider _blocker;
        [SerializeField] private Transform _wheel;
        private bool? _last;
        private TunnelManager _manager;
        private void Start(){_manager=TunnelManager.Instance;if(_manager!=null)_manager.Restored+=Apply;Apply();}
        private void OnDestroy(){if(_manager!=null)_manager.Restored-=Apply;}
        public bool Active => string.IsNullOrEmpty(_switchId) || TunnelManager.Instance==null || TunnelManager.Instance.GetSwitch(_switchId,true);
        public bool Passable => _blocker==null || !Active;
        internal void Configure(string id,ParticleSystem jet,AudioSource hiss,Collider blocker,Transform wheel)
        {_switchId=id;_jet=jet;_hiss=hiss;_blocker=blocker;_wheel=wheel;}
        private void Apply()
        {
            bool active=Active;
            if(_last!=active)
            {
                _last=active;
                if(_blocker!=null)_blocker.enabled=active;
                if(_jet!=null){if(active)_jet.Play();else _jet.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
                if(_hiss!=null){if(active)_hiss.Play();else _hiss.Stop();}
            }
        }
        private void Update(){Apply();if(_wheel!=null)_wheel.localRotation=Quaternion.RotateTowards(_wheel.localRotation,Quaternion.Euler(0,0,Active?0:160),180*Time.deltaTime);}
        public string GetPrompt(InteractionController user)=>string.IsNullOrEmpty(_switchId)?null:Active?"Close steam isolation valve":"Steam isolated";
        public void Interact(InteractionController user)
        {
            // Deliberately one-way isolation: no restarting a jet on a player in its doorway.
            if(Active && !string.IsNullOrEmpty(_switchId) && TunnelManager.Instance!=null)
            {TunnelManager.Instance.SetSwitch(_switchId,false,true);user.Notify("Steam isolated. Service room access clear.");}
        }
    }
}
