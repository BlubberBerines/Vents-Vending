using System;

namespace VentsAndVending.Security
{
    /// <summary>GDD §11.3 security alert states.</summary>
    public enum AlertState
    {
        Unaware,
        Suspicious,
        Searching,
        Pursuing
    }

    /// <summary>
    /// Engine-independent alert state machine for one security unit. Suspicion (0–1) builds while the player is
    /// seen doing something suspicious; full suspicion means the player is identified and pursued.
    /// </summary>
    public sealed class DroneAlert
    {
        public const float SuspiciousAt = 0.25f;
        public const float ClearBelow = 0.05f;
        public const float DecayPerSecond = 0.12f;
        public const float LoseSightSeconds = 2.5f;
        public const float SearchSeconds = 7f;
        public const float CatchSeconds = 1.2f;
        /// <summary>Suspicion kept when the trail goes cold — re-spotting you during the search resumes pursuit fast.</summary>
        public const float SearchSuspicion = 0.7f;

        private float _lostTimer;
        private float _searchTimer;
        private float _catchTimer;

        public AlertState State { get; private set; } = AlertState.Unaware;
        public float Suspicion { get; private set; }
        /// <summary>0–1 progress of the current catch attempt.</summary>
        public float CatchProgress => _catchTimer / CatchSeconds;

        public event Action<AlertState, AlertState> StateChanged;
        public event Action Caught;

        /// <param name="gainRate">Suspicion per second from <see cref="SecurityRules.SuspicionRate"/> (0 when not suspicious).</param>
        /// <param name="canSeePlayer">Line of sight + view cone + visible, regardless of suspiciousness.</param>
        /// <param name="inCatchRange">Close enough to scan/apprehend.</param>
        public void Tick(float deltaTime, float gainRate, bool canSeePlayer, bool inCatchRange)
        {
            if (gainRate > 0f)
            {
                Suspicion = Math.Min(1f, Suspicion + gainRate * deltaTime);
            }
            else if (State != AlertState.Pursuing)
            {
                Suspicion = Math.Max(0f, Suspicion - DecayPerSecond * deltaTime);
            }

            switch (State)
            {
                case AlertState.Unaware:
                    if (Suspicion >= SuspiciousAt)
                    {
                        SetState(AlertState.Suspicious);
                        if (Suspicion >= 1f)
                        {
                            BeginPursuit();
                        }
                    }
                    break;

                case AlertState.Suspicious:
                    if (Suspicion >= 1f)
                    {
                        BeginPursuit();
                    }
                    else if (Suspicion <= ClearBelow)
                    {
                        SetState(AlertState.Unaware);
                    }
                    break;

                case AlertState.Pursuing:
                    Suspicion = 1f;
                    if (canSeePlayer)
                    {
                        _lostTimer = 0f;
                        if (inCatchRange)
                        {
                            _catchTimer += deltaTime;
                            if (_catchTimer >= CatchSeconds)
                            {
                                Reset();
                                Caught?.Invoke();
                                return;
                            }
                        }
                        else
                        {
                            _catchTimer = Math.Max(0f, _catchTimer - deltaTime);
                        }
                    }
                    else
                    {
                        _catchTimer = Math.Max(0f, _catchTimer - deltaTime);
                        _lostTimer += deltaTime;
                        if (_lostTimer >= LoseSightSeconds)
                        {
                            Suspicion = SearchSuspicion;
                            _searchTimer = SearchSeconds;
                            SetState(AlertState.Searching);
                        }
                    }
                    break;

                case AlertState.Searching:
                    if (Suspicion >= 1f)
                    {
                        BeginPursuit();
                        break;
                    }

                    _searchTimer -= deltaTime;
                    if (_searchTimer <= 0f)
                    {
                        Suspicion = Math.Min(Suspicion, SuspiciousAt - 0.01f);
                        SetState(AlertState.Unaware);
                    }
                    break;
            }
        }

        public void Reset()
        {
            Suspicion = 0f;
            _lostTimer = 0f;
            _searchTimer = 0f;
            _catchTimer = 0f;
            SetState(AlertState.Unaware);
        }

        private void BeginPursuit()
        {
            Suspicion = 1f;
            _lostTimer = 0f;
            _catchTimer = 0f;
            SetState(AlertState.Pursuing);
        }

        private void SetState(AlertState next)
        {
            if (next == State)
            {
                return;
            }

            AlertState previous = State;
            State = next;
            StateChanged?.Invoke(previous, next);
        }
    }
}
