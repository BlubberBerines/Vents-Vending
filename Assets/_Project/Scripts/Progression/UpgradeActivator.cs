using UnityEngine;

namespace VentsAndVending.Progression
{
    /// <summary>Shows objects (lamps, props, new equipment) once an upgrade with the given effect key is owned.</summary>
    public sealed class UpgradeActivator : MonoBehaviour
    {
        [SerializeField] private string _key = UpgradeKeys.HideoutLighting;
        [SerializeField] private GameObject[] _showWhenOwned = new GameObject[0];
        [SerializeField] private GameObject[] _hideWhenOwned = new GameObject[0];

        private void Start() => ProgressionManager.Subscribe(Apply);

        private void OnDestroy() => ProgressionManager.Unsubscribe(Apply);

        private void Apply()
        {
            bool owned = ProgressionManager.Instance != null && ProgressionManager.Instance.Has(_key);
            foreach (GameObject go in _showWhenOwned)
            {
                if (go != null)
                {
                    go.SetActive(owned);
                }
            }
            foreach (GameObject go in _hideWhenOwned)
            {
                if (go != null)
                {
                    go.SetActive(!owned);
                }
            }
        }
    }
}
