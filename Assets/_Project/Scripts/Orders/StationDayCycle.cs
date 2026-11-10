using UnityEngine;

namespace VentsAndVending.Orders
{
    /// <summary>Station lighting shifts, not an outdoor sun. The Den and safety lights remain independent.</summary>
    public sealed class StationDayCycle : MonoBehaviour
    {
        private Light[] _lights;
        private float[] _intensities;
        private void Start()
        {
            _lights=GetComponentsInChildren<Light>();_intensities=new float[_lights.Length];
            for(int i=0;i<_lights.Length;i++)_intensities[i]=_lights[i].intensity;
        }
        private void Update()
        {
            if(OrderManager.Instance==null || _lights==null)return;
            float level=StationTime.IsNight(OrderManager.Instance.Board.Clock)?.4f:1;
            for(int i=0;i<_lights.Length;i++)if(_lights[i]!=null)_lights[i].intensity=Mathf.MoveTowards(_lights[i].intensity,_intensities[i]*level,Time.deltaTime*_intensities[i]*.3f);
        }
    }
}
