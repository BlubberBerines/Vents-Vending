using System.Collections;
using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.World
{
    /// <summary>
    /// A crate the player can shove half a metre at a time to stand under a vent mouth (station layers spec §6.2).
    /// Pushes along the cardinal axis nearest the player's view; refused into walls, other crates or off edges.
    /// Positions are not saved (crates reset on load); crates the routes depend on are fixed, not pushable.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PushableCrate : MonoBehaviour, IInteractable
    {
        [SerializeField] private float _step = 0.5f;
        [SerializeField] private float _moveSeconds = 0.25f;
        [SerializeField] private AudioClip _scrape;
        [SerializeField] private string _blockedMessage = "It won't budge that way.";

        private readonly Collider[] _overlaps = new Collider[16];
        private BoxCollider _box;
        private bool _moving;

        private void Awake() => _box = GetComponent<BoxCollider>();

        public string GetPrompt(InteractionController interactor) => _moving ? null : "Push crate";

        public void Interact(InteractionController interactor)
        {
            if (_moving)
            {
                return;
            }

            Vector3 axis = CratePushLogic.PushAxis(interactor.View.forward);
            Vector3 delta = axis * _step;
            if (!CratePushLogic.CanPush(Overlaps(delta), HasGround(delta)))
            {
                interactor.Notify(_blockedMessage);
                return;
            }

            StartCoroutine(Slide(delta));
        }

        private bool Overlaps(Vector3 delta)
        {
            Bounds b = _box.bounds;
            Vector3 half = b.extents - Vector3.one * 0.02f;
            int n = Physics.OverlapBoxNonAlloc(b.center + delta + Vector3.up * 0.02f, half, _overlaps, Quaternion.identity, ~0,
                                               QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                Collider c = _overlaps[i];
                if (c != _box && !c.transform.IsChildOf(transform))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasGround(Vector3 delta)
        {
            Bounds b = _box.bounds;
            Vector3 origin = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z) + delta;
            return Physics.Raycast(origin, Vector3.down, 0.2f, ~0, QueryTriggerInteraction.Ignore);
        }

        private IEnumerator Slide(Vector3 delta)
        {
            _moving = true;
            Sfx.PlayAt(_scrape, transform.position, 0.9f);
            Vector3 from = transform.position, to = from + delta;
            for (float t = 0f; t < _moveSeconds; t += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / _moveSeconds));
                yield return null;
            }

            transform.position = to;
            Physics.SyncTransforms();
            _moving = false;
        }
    }
}
