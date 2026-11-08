using System;
using TMPro;
using UnityEngine;
using VentsAndVending.Interaction;

namespace VentsAndVending.Orders
{
    /// <summary>
    /// The den PC: an old maintenance terminal patched into the station's message relays. Opens the VentNet inbox,
    /// and its screen shows whether anything is waiting so you notice it walking past.
    /// </summary>
    public sealed class VentNetTerminal : MonoBehaviour, IInteractable
    {
        public static event Action<VentNetTerminal, InteractionController> PanelRequested;

        [SerializeField] private TextMeshPro _screen;

        private float _refresh;

        internal void Configure(TextMeshPro screen) => _screen = screen;

        public string GetPrompt(InteractionController interactor)
        {
            int n = OrderManager.Instance != null ? OrderManager.Instance.Board.OfferCount : 0;
            return n > 0 ? $"Check VentNet <color=#FFD24D>({n} new)</color>" : "Check VentNet";
        }

        public void Interact(InteractionController interactor) => PanelRequested?.Invoke(this, interactor);

        private void Update()
        {
            _refresh -= Time.deltaTime;
            if (_screen == null || _refresh > 0f)
            {
                return;
            }
            _refresh = 0.5f;

            OrderManager m = OrderManager.Instance;
            int offers = m != null ? m.Board.OfferCount : 0;
            int active = m != null ? m.Board.ActiveCount : 0;
            bool blink = Mathf.Repeat(Time.time, 1f) > 0.5f;
            _screen.text = "<b>VENTNET</b> v0.9\n" +
                           (offers > 0 ? $"<color=#FFD24D>{(blink ? ">" : " ")} {offers} NEW MSG</color>" : "<color=#5C8A6A>no new msgs</color>") +
                           $"\n<size=80%>deals: {active}</size>";
        }
    }
}
