using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VentsAndVending.Interaction;
using VentsAndVending.Orders;
using VentsAndVending.Player;

namespace VentsAndVending.UI
{
    /// <summary>
    /// In-game HUD (GDD §17.2) in the Ink &amp; Hazard style: VentNet deals card (top-left), credits (top-right), drone
    /// alert (top-centre), crosshair + keycap prompt, toasts, stance and key hints. Everything hides while a screen is open.
    /// </summary>
    public sealed class HudController : MonoBehaviour
    {
        [SerializeField] private InteractionController _interactor;
        [SerializeField] private float _toastSeconds = 2.6f;

        private sealed class DealRow
        {
            public GameObject Root;
            public Image Badge;
            public TextMeshProUGUI Letter;
            public TextMeshProUGUI Title;
            public TextMeshProUGUI Detail;
            public TextMeshProUGUI Timer;
        }

        private Economy.PlayerWallet _wallet;
        private GameObject _hudRoot;

        private TextMeshProUGUI _credits;
        private TextMeshProUGUI _creditsDelta;
        private float _deltaTimer;

        private GameObject _alertRoot;
        private TextMeshProUGUI _alertTitle;
        private TextMeshProUGUI _alertText;
        private Image _alertFill;

        private Image _crosshair;
        private GameObject _promptRoot;
        private TextMeshProUGUI _promptKey;
        private TextMeshProUGUI _prompt;

        private GameObject _toastRoot;
        private CanvasGroup _toastGroup;
        private Image _toastDot;
        private TextMeshProUGUI _toast;
        private float _toastTimer;

        private GameObject _dealsRoot;
        private readonly DealRow[] _deals = new DealRow[OrderRules.MaxActiveOrders];
        private TextMeshProUGUI _dealsFooter;
        private float _dealsRefresh;

        private GameObject _stanceRoot;
        private TextMeshProUGUI _stance;
        private GameObject _hints;

        private void Awake()
        {
            if (_interactor == null)
            {
                _interactor = FindAnyObjectByType<InteractionController>();
            }
            if (_interactor != null)
            {
                _wallet = _interactor.GetComponent<Economy.PlayerWallet>();
            }

            Canvas canvas = UiFactory.CreateCanvas(transform, "HUD Canvas", 0);
            _hudRoot = UiFactory.Rect("HUD", canvas.transform).gameObject;
            UiFactory.Stretch((RectTransform)_hudRoot.transform);
            Transform root = _hudRoot.transform;

            BuildCredits(root);
            BuildAlert(root);
            BuildCrosshairAndPrompt(root);
            BuildToast(root);
            BuildDeals(root);
            BuildStanceAndHints(root);
        }

        // ------------------------------------------------------------------ Build

        private void BuildCredits(Transform root)
        {
            Image panel = UiFactory.Panel(root, "Credits", null, 5f, 14f);
            UiFactory.Place(panel.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-32f, -32f), new Vector2(210f, 62f));
            panel.raycastTarget = false;
            _credits = UiFactory.Mono(panel.transform, "Amount", "0", 34f, TextAlignmentOptions.Right, UiFactory.Paper);
            UiFactory.Stretch(_credits.rectTransform, 12f);
            _credits.rectTransform.offsetMax = new Vector2(-18f, -6f);
            _creditsDelta = UiFactory.Mono(root, "Delta", string.Empty, 22f, TextAlignmentOptions.Right, UiFactory.Credit);
            UiFactory.Place(_creditsDelta.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -102f), new Vector2(220f, 30f));
        }

        private void BuildAlert(Transform root)
        {
            Image panel = UiFactory.Panel(root, "SecurityAlert", null, 5f, 14f);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(520f, 70f));
            panel.raycastTarget = false;
            _alertRoot = panel.gameObject;
            _alertTitle = UiFactory.Heading(panel.transform, "Title", string.Empty, 21f, TextAlignmentOptions.Left);
            UiFactory.Place(_alertTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -10f), new Vector2(200f, 28f));
            _alertText = UiFactory.Text(panel.transform, "Text", string.Empty, 18f, TextAlignmentOptions.Right, new Color(0.79f, 0.82f, 0.85f));
            UiFactory.Place(_alertText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-20f, -11f), new Vector2(300f, 28f));
            _alertFill = UiFactory.Bar(panel.transform, "Bar", 0f, UiFactory.Hazard);
            UiFactory.Place((RectTransform)_alertFill.transform.parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(480f, 14f));
            _alertRoot.SetActive(false);
        }

        private void BuildCrosshairAndPrompt(Transform root)
        {
            _crosshair = UiFactory.Image(root, "Crosshair", UiFactory.Paper, false);
            if (UiTheme.Current != null && UiTheme.Current.Circle != null)
            {
                _crosshair.sprite = UiTheme.Current.Circle;
            }
            UiFactory.Place(_crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f));
            _crosshair.raycastTarget = false;

            Image panel = UiFactory.Panel(root, "Prompt", null, 4f, 12f);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(10f, 48f));
            panel.raycastTarget = false;
            var row = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(12, 18, 8, 8);
            row.spacing = 12f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            RectTransform key = UiFactory.Keycap(panel.transform, "E", UiFactory.Hazard);
            _promptKey = key.GetComponentInChildren<TextMeshProUGUI>();
            _prompt = UiFactory.Heading(panel.transform, "Text", string.Empty, 22f);
            _prompt.textWrappingMode = TextWrappingModes.NoWrap;
            _promptRoot = panel.gameObject;
            _promptRoot.SetActive(false);
        }

        private void BuildToast(Transform root)
        {
            Image panel = UiFactory.Panel(root, "Toast", null, 5f, 14f);
            UiFactory.Place(panel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(10f, 54f));
            panel.raycastTarget = false;
            var row = panel.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(18, 22, 10, 10);
            row.spacing = 14f;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            _toastDot = UiFactory.Image(panel.transform, "Dot", UiFactory.Go, false);
            if (UiTheme.Current != null && UiTheme.Current.Circle != null)
            {
                _toastDot.sprite = UiTheme.Current.Circle;
            }
            UiFactory.Layout(_toastDot.gameObject, preferredHeight: 14f, preferredWidth: 14f);
            _toast = UiFactory.Text(panel.transform, "Text", string.Empty, 21f);
            _toast.textWrappingMode = TextWrappingModes.NoWrap;
            _toastGroup = panel.gameObject.AddComponent<CanvasGroup>();
            _toastRoot = panel.gameObject;
            _toastRoot.SetActive(false);
        }

        private void BuildDeals(Transform root)
        {
            Image panel = UiFactory.Panel(root, "Deals", null, 5f, 14f);
            UiFactory.Place(panel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(32f, -32f), new Vector2(470f, 10f));
            panel.raycastTarget = false;
            var list = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(18, 18, 14, 14);
            list.spacing = 10f;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandHeight = false;
            var fit = panel.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RectTransform header = UiFactory.Rect("Header", panel.transform);
            UiFactory.HRow(header, 10f);
            Image dot = UiFactory.Image(header, "Dot", UiFactory.Go, false);
            if (UiTheme.Current != null && UiTheme.Current.Circle != null)
            {
                dot.sprite = UiTheme.Current.Circle;
            }
            UiFactory.Layout(dot.gameObject, preferredHeight: 10f, preferredWidth: 10f);
            UiFactory.Label(header, "Title", "VentNet · deals", UiFactory.Go);

            for (int i = 0; i < _deals.Length; i++)
            {
                RectTransform r = UiFactory.Rect($"Deal {i}", panel.transform);
                UiFactory.HRow(r, 14f);
                UiFactory.Layout(r.gameObject, preferredHeight: 46f);
                var d = new DealRow { Root = r.gameObject };
                d.Badge = UiFactory.Panel(r, "Badge", UiFactory.Row, 0f, 9f);
                UiFactory.Layout(d.Badge.gameObject, preferredHeight: 42f, preferredWidth: 42f);
                d.Letter = UiFactory.Heading(d.Badge.transform, "Letter", "A", 22f, TextAlignmentOptions.Center);
                UiFactory.Stretch(d.Letter.rectTransform);
                RectTransform texts = UiFactory.Rect("Texts", r);
                var v = texts.gameObject.AddComponent<VerticalLayoutGroup>();
                v.childControlWidth = true;
                v.childControlHeight = true;
                v.childForceExpandHeight = false;
                UiFactory.Layout(texts.gameObject, flexibleWidth: 1f);
                d.Title = UiFactory.Heading(texts, "Title", string.Empty, 19f);
                d.Title.textWrappingMode = TextWrappingModes.NoWrap;
                d.Title.overflowMode = TextOverflowModes.Ellipsis;
                d.Detail = UiFactory.Text(texts, "Detail", string.Empty, 16f, TextAlignmentOptions.Left, UiFactory.MutedText);
                d.Detail.textWrappingMode = TextWrappingModes.NoWrap;
                d.Timer = UiFactory.Mono(r, "Timer", string.Empty, 20f, TextAlignmentOptions.Right);
                UiFactory.Layout(d.Timer.gameObject, preferredWidth: 70f);
                _deals[i] = d;
            }

            _dealsFooter = UiFactory.Heading(panel.transform, "Footer", string.Empty, 18f, TextAlignmentOptions.Left, UiFactory.Hazard);
            _dealsRoot = panel.gameObject;
            _dealsRoot.SetActive(false);
        }

        private void BuildStanceAndHints(Transform root)
        {
            Image panel = UiFactory.Panel(root, "Stance", null, 4f, 12f);
            UiFactory.Place(panel.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(32f, 32f), new Vector2(180f, 44f));
            panel.raycastTarget = false;
            _stance = UiFactory.Label(panel.transform, "Text", "Crouching", UiFactory.Paper, TextAlignmentOptions.Center);
            UiFactory.Stretch(_stance.rectTransform, 4f);
            _stanceRoot = panel.gameObject;
            _stanceRoot.SetActive(false);

            RectTransform hints = UiFactory.Rect("Hints", root);
            UiFactory.Place(hints, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 34f), new Vector2(500f, 34f));
            UiFactory.HRow(hints, 24f, TextAnchor.MiddleRight);
            UiFactory.KeyHint(hints, "Tab", "Inventory");
            UiFactory.KeyHint(hints, "Esc", "Pause");
            _hints = hints.gameObject;
        }

        // ------------------------------------------------------------------ Runtime

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

        private void OnCreditsChanged(int balance, int delta)
        {
            _credits.text = $"{balance} <size=60%><color=#3FE0FF>CC</color></size>";
            if (delta != 0)
            {
                _creditsDelta.text = delta > 0 ? $"+{delta}" : delta.ToString();
                _creditsDelta.color = delta > 0 ? UiFactory.Credit : UiFactory.Alert;
                _deltaTimer = 2.5f;
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
            _hints.SetActive(!busy);

            string prompt = busy ? null : _interactor.CurrentPrompt;
            bool showPrompt = !string.IsNullOrEmpty(prompt);
            if (_promptRoot.activeSelf != showPrompt)
            {
                _promptRoot.SetActive(showPrompt);
            }
            if (showPrompt)
            {
                _prompt.text = prompt;
                _promptKey.text = _interactor.InteractKeyLabel;
            }

            UpdateStance(busy);
            UpdateSecurityAlert();
            UpdateDeals(busy);

            if (_deltaTimer > 0f)
            {
                _deltaTimer -= Time.unscaledDeltaTime;
                _creditsDelta.alpha = Mathf.Clamp01(_deltaTimer / 0.5f);
            }

            if (_toastTimer > 0f)
            {
                _toastTimer -= Time.unscaledDeltaTime;
                _toastGroup.alpha = Mathf.Clamp01(_toastTimer / 0.4f);
                if (_toastTimer <= 0f)
                {
                    _toastRoot.SetActive(false);
                }
            }
        }

        private void UpdateStance(bool busy)
        {
            PlayerStateController state = _interactor.State;
            PlayerMovementState posture = state != null ? state.Current : PlayerMovementState.Normal;
            bool show = !busy && (posture == PlayerMovementState.Crouching || posture == PlayerMovementState.Crawling);
            if (_stanceRoot.activeSelf != show)
            {
                _stanceRoot.SetActive(show);
            }
            if (show)
            {
                _stance.text = posture == PlayerMovementState.Crawling ? "CRAWLING" : "CROUCHING";
            }
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
                    color = Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f ? UiFactory.Alert : new Color(1f, 0.7f, 0.65f);
                    _alertTitle.text = "PURSUIT!";
                    _alertText.text = "run · break line of sight · hit a vent";
                    break;
                case Security.AlertState.Searching:
                    color = new Color(1f, 0.6f, 0.2f);
                    _alertTitle.text = "SEARCHING";
                    _alertText.text = "stay out of sight";
                    break;
                case Security.AlertState.Suspicious:
                    color = UiFactory.Hazard;
                    _alertTitle.text = "SUSPICIOUS";
                    _alertText.text = "a drone is watching you";
                    break;
                default:
                    color = new Color(0.8f, 0.8f, 0.5f);
                    _alertTitle.text = "COOLING OFF";
                    _alertText.text = "drone attention fading";
                    break;
            }

            _alertTitle.color = color;
            _alertFill.color = color;
            UiFactory.SetBar(_alertFill, suspicion);
        }

        private void UpdateDeals(bool busy)
        {
            _dealsRefresh -= Time.unscaledDeltaTime;
            if (_dealsRefresh > 0f)
            {
                return;
            }
            _dealsRefresh = 0.2f;

            OrderManager manager = OrderManager.Instance;
            if (manager == null || busy)
            {
                _dealsRoot.SetActive(false);
                return;
            }

            OrderBoard board = manager.Board;
            int shown = 0;
            foreach (CustomerOrder o in board.Orders)
            {
                if (!o.IsActive || shown >= _deals.Length)
                {
                    continue;
                }
                DealRow row = _deals[shown++];
                row.Root.SetActive(true);
                MeetSpot spot = manager.FindSpot(o.SpotId);
                var (bg, fg) = UiFactory.RiskColours(spot != null ? (int)spot.Risk : 1);
                row.Badge.color = bg;
                row.Letter.color = fg;
                row.Letter.text = o.SpotId;
                var product = manager.FindProduct(o.ProductId);
                row.Title.text = $"{o.CustomerName} · {o.Quantity}× {(product != null ? product.DisplayName : o.ProductId)}";
                bool waiting = o.Status == OrderStatus.Waiting;
                row.Detail.text = waiting
                    ? $"<color=#3FE0FF>waiting at the spot</color>"
                    : $"{o.MinimumQuality}+ · {(spot != null ? spot.DisplayName : "")}";
                row.Timer.text = OrderRules.Clock(waiting ? o.WindowClosesAt - board.Clock : o.WindowOpensAt - board.Clock);
                row.Timer.color = waiting ? UiFactory.Credit : UiFactory.Paper;
            }
            for (int i = shown; i < _deals.Length; i++)
            {
                _deals[i].Root.SetActive(false);
            }

            int offers = board.OfferCount;
            _dealsFooter.gameObject.SetActive(offers > 0);
            _dealsFooter.text = $"{offers} new message{(offers == 1 ? "" : "s")} <color=#9AA3AD>· PC in the den</color>";
            _dealsRoot.SetActive(shown > 0 || offers > 0);
        }

        private void ShowToast(string message)
        {
            _toast.text = message;
            _toastDot.color = message.Contains("#FF6") || message.Contains("#FF7") || message.Contains("#FF8") ? UiFactory.Alert
                : message.Contains("#FFD") || message.Contains("#FFB") ? UiFactory.Hazard
                : message.Contains("+") ? UiFactory.Go : UiFactory.Credit;
            _toastRoot.SetActive(true);
            _toastGroup.alpha = 1f;
            _toastTimer = _toastSeconds;
        }
    }
}
