using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Interaction;

namespace VentsAndVending.UI
{
    /// <summary>
    /// Minimal HUD (GDD §17.2): crosshair, interaction prompt, feedback toasts and the inventory shortcut hint.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private InteractionController _interactor;
        [SerializeField] private float _toastSeconds = 2.2f;

        private Economy.PlayerWallet _wallet;
        private TextMeshProUGUI _credits;
        private TextMeshProUGUI _creditsDelta;
        private float _deltaTimer;
        private GameObject _alertRoot;
        private TextMeshProUGUI _alertText;
        private Image _alertFill;
        private Image _crosshair;
        private TextMeshProUGUI _prompt;
        private TextMeshProUGUI _toast;
        private TextMeshProUGUI _hint;
        private float _toastTimer;
        private TextMeshProUGUI _orders;
        private float _ordersRefresh;

        private void Awake()
        {
            if (_interactor == null)
            {
                _interactor = FindAnyObjectByType<InteractionController>();
            }

            Canvas canvas = UiFactory.CreateCanvas(transform, "HUD Canvas", 0);
            Transform root = canvas.transform;

            _crosshair = UiFactory.Image(root, "Crosshair", new Color(1f, 1f, 1f, 0.75f));
            UiFactory.Place(_crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5f, 5f));
            _crosshair.raycastTarget = false;

            _prompt = UiFactory.Text(root, "Prompt", string.Empty, 30f, TextAlignmentOptions.Center);
            UiFactory.Place(_prompt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 50f));
            _prompt.fontStyle = FontStyles.Bold;

            _toast = UiFactory.Text(root, "Toast", string.Empty, 26f, TextAlignmentOptions.Center, UiFactory.Accent);
            UiFactory.Place(_toast.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 140f), new Vector2(900f, 44f));

            _hint = UiFactory.Text(root, "Hint", "[ Tab ] Inventory", 20f, TextAlignmentOptions.Right, UiFactory.MutedText);
            UiFactory.Place(_hint.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 24f), new Vector2(400f, 30f));

            // VentNet deals tracker (top-left): booked meetings and unread messages
            _orders = UiFactory.Text(root, "Orders", string.Empty, 21f, TextAlignmentOptions.TopLeft);
            UiFactory.Place(_orders.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -24f), new Vector2(640f, 200f));

            // Cosmic Credits (GDD §17.2: clear money indicator)
            var creditsBg = UiFactory.Image(root, "CreditsPanel", UiFactory.PanelColor);
            UiFactory.Place(creditsBg.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -28f), new Vector2(220f, 56f));
            creditsBg.raycastTarget = false;
            _credits = UiFactory.Text(creditsBg.transform, "Credits", string.Empty, 30f, TextAlignmentOptions.Right);
            UiFactory.Stretch(_credits.rectTransform, 12f);
            _credits.fontStyle = FontStyles.Bold;
            _creditsDelta = UiFactory.Text(root, "CreditsDelta", string.Empty, 24f, TextAlignmentOptions.Right, UiFactory.Accent);
            UiFactory.Place(_creditsDelta.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -90f), new Vector2(220f, 32f));

            if (_interactor != null)
            {
                _wallet = _interactor.GetComponent<Economy.PlayerWallet>();
            }

            // Security alert indicator (GDD §17.2)
            var alertBg = UiFactory.Image(root, "SecurityAlert", UiFactory.PanelColor);
            UiFactory.Place(alertBg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(520f, 64f));
            alertBg.raycastTarget = false;
            _alertRoot = alertBg.gameObject;
            _alertText = UiFactory.Text(alertBg.transform, "Label", string.Empty, 24f, TextAlignmentOptions.Center);
            UiFactory.Place(_alertText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(500f, 34f));
            _alertText.fontStyle = FontStyles.Bold;
            var track = UiFactory.Image(alertBg.transform, "Track", new Color(1f, 1f, 1f, 0.12f));
            UiFactory.Place(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(480f, 8f));
            _alertFill = UiFactory.Image(track.transform, "Fill", Color.yellow);
            _alertFill.rectTransform.anchorMin = Vector2.zero;
            _alertFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            _alertFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _alertFill.rectTransform.offsetMin = Vector2.zero;
            _alertFill.rectTransform.offsetMax = Vector2.zero;
            _alertRoot.SetActive(false);
        }

        private void UpdateSecurityAlert()
        {
            var security = Security.SecurityManager.Instance;
            if (security == null)
            {
                _alertRoot.SetActive(false);
                return;
            }

            Security.AlertState state = security.HighestAlert;
            float suspicion = security.HighestSuspicion;
            bool show = state != Security.AlertState.Unaware || suspicion > 0.02f;
            _alertRoot.SetActive(show);
            if (!show)
            {
                return;
            }

            Color color;
            switch (state)
            {
                case Security.AlertState.Pursuing:
                    color = Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f ? new Color(1f, 0.2f, 0.2f) : new Color(1f, 0.6f, 0.6f);
                    _alertText.text = "PURSUIT!  <size=70%>Run, break line of sight, or get into a vent</size>";
                    break;
                case Security.AlertState.Searching:
                    color = new Color(1f, 0.55f, 0.1f);
                    _alertText.text = "SEARCHING…  <size=70%>Stay out of sight</size>";
                    break;
                case Security.AlertState.Suspicious:
                    color = new Color(1f, 0.85f, 0.2f);
                    _alertText.text = "SUSPICIOUS  <size=70%>A drone is watching you</size>";
                    break;
                default:
                    color = new Color(0.8f, 0.8f, 0.4f);
                    _alertText.text = "<size=80%>Drone attention fading…</size>";
                    break;
            }

            _alertText.color = color;
            _alertFill.color = color;
            _alertFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(suspicion), 1f);
        }

        private void Start()
        {
            if (_wallet != null)
            {
                _wallet.Wallet.Changed += OnCreditsChanged;
                OnCreditsChanged(_wallet.Wallet.Balance, 0);
            }
            else
            {
                _credits.transform.parent.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_wallet != null)
            {
                _wallet.Wallet.Changed -= OnCreditsChanged;
            }
        }

        private void OnCreditsChanged(int balance, int delta)
        {
            _credits.text = $"{balance} <color=#19E5FF><size=70%>CC</size></color>";
            if (delta != 0)
            {
                _creditsDelta.text = delta > 0 ? $"+{delta} CC" : $"{delta} CC";
                _creditsDelta.color = delta > 0 ? UiFactory.Accent : new Color(1f, 0.45f, 0.4f);
                _deltaTimer = 2.5f;
            }
        }

        private void OnEnable()
        {
            if (_interactor != null)
            {
                _interactor.Notified += ShowToast;
            }
        }

        private void OnDisable()
        {
            if (_interactor != null)
            {
                _interactor.Notified -= ShowToast;
            }
        }

        private void Update()
        {
            if (_interactor == null)
            {
                return;
            }

            bool busy = _interactor.State != null && _interactor.State.IsInteracting;
            _crosshair.enabled = !busy;
            _hint.enabled = !busy;

            string prompt = busy ? null : _interactor.CurrentPrompt;
            _prompt.text = string.IsNullOrEmpty(prompt) ? string.Empty : $"<color=#19E5FF>[ {_interactor.InteractKeyLabel} ]</color>  {prompt}";

            UpdateSecurityAlert();
            UpdateOrders(busy);

            if (_deltaTimer > 0f)
            {
                _deltaTimer -= Time.unscaledDeltaTime;
                _creditsDelta.alpha = Mathf.Clamp01(_deltaTimer / 0.5f);
            }

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                _toast.alpha = Mathf.Clamp01(_toastTimer / 0.4f);
            }
        }

        private void UpdateOrders(bool busy)
        {
            _ordersRefresh -= Time.unscaledDeltaTime;
            if (_ordersRefresh > 0f)
            {
                return;
            }
            _ordersRefresh = 0.25f;

            var manager = Orders.OrderManager.Instance;
            if (manager == null || busy)
            {
                _orders.text = string.Empty;
                return;
            }

            var board = manager.Board;
            var sb = new System.Text.StringBuilder();
            foreach (Orders.CustomerOrder o in board.Orders)
            {
                if (!o.IsActive)
                {
                    continue;
                }
                var product = manager.FindProduct(o.ProductId);
                string when = o.Status == Orders.OrderStatus.Waiting
                    ? $"<color=#19E5FF>waiting · {Orders.OrderRules.Clock(o.WindowClosesAt - board.Clock)}</color>"
                    : $"<color=#9AA3AD>arrives in {Orders.OrderRules.Clock(o.WindowOpensAt - board.Clock)}</color>";
                sb.Append($"<b>Spot {o.SpotId}</b>  {o.CustomerName} · {o.Quantity}× {(product != null ? product.DisplayName : o.ProductId)} {o.MinimumQuality}+  {when}").Append('\n');
            }
            if (board.OfferCount > 0)
            {
                sb.Append($"<color=#FFD24D>[!] {board.OfferCount} new VentNet message{(board.OfferCount == 1 ? "" : "s")}</color> <color=#9AA3AD><size=85%>(PC in the den)</size></color>");
            }
            _orders.text = sb.ToString();
        }

        private void ShowToast(string message)
        {
            _toast.text = message;
            _toast.alpha = 1f;
            _toastTimer = _toastSeconds;
        }
    }
}
