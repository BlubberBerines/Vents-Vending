using UnityEngine;
using UnityEngine.EventSystems;
using VentsAndVending.Interaction;
using VentsAndVending.Player;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Base for full-screen panels: frees the cursor and freezes the player while open, closes on
    /// Interact / Inventory / Cancel, and only one can be open at a time.
    /// </summary>
    public abstract class ModalScreen : MonoBehaviour
    {
        [SerializeField] protected InteractionController _interactor;

        private GameObject _root;
        private int _openedFrame = -1;

        public bool IsOpen => _root != null && _root.activeSelf;
        protected InteractionController Interactor => _interactor;

        protected virtual void Awake()
        {
            if (_interactor == null)
            {
                _interactor = FindAnyObjectByType<InteractionController>();
            }

            _root = BuildUi();
            _root.SetActive(false);
        }

        /// <summary>Create the UI hierarchy (under this transform) and return its root.</summary>
        protected abstract GameObject BuildUi();

        /// <summary>Shows the screen. Returns false if another screen/interaction already owns the player.</summary>
        protected bool BeginOpen()
        {
            if (IsOpen || _interactor == null || _interactor.State.IsInteracting)
            {
                return false;
            }

            _root.SetActive(true);
            _openedFrame = Time.frameCount;
            _interactor.State.IsInteracting = true;
            PlayerCamera.SetCursorLocked(false);
            return true;
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            OnClosing();
            _root.SetActive(false);
            _interactor.State.IsInteracting = false;
            PlayerCamera.SetCursorLocked(true);
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        protected virtual void OnClosing()
        {
        }

        protected virtual void UpdateWhileClosed()
        {
        }

        protected virtual void UpdateWhileOpen()
        {
        }

        protected virtual void Update()
        {
            if (_interactor == null)
            {
                return;
            }

            if (!IsOpen)
            {
                UpdateWhileClosed();
                return;
            }

            if (Time.frameCount != _openedFrame)
            {
                PlayerInputHandler input = _interactor.Input;
                if (input.InventoryPressed || input.InteractPressed || input.CancelPressed)
                {
                    Close();
                    return;
                }
            }

            UpdateWhileOpen();
        }

        protected static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
        }
    }
}
