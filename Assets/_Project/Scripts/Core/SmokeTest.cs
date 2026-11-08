using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VentsAndVending.Core
{
    /// <summary>
    /// Launch the player with <c>-smoketest</c> to boot the main menu, start a new game, run for a few seconds and quit,
    /// logging "SMOKETEST PASS" or "SMOKETEST FAIL" (any exception or error logged counts as a failure).
    /// </summary>
    public sealed class SmokeTest : MonoBehaviour
    {
        private const float RunSeconds = 8f;

        private int _errors;
        private float _gameTime;
        private bool _gameStarted;

        public static bool Requested => Environment.GetCommandLineArgs().Any(a => a.Equals("-smoketest", StringComparison.OrdinalIgnoreCase));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Requested || Application.isEditor)
            {
                return;
            }

            var go = new GameObject("SmokeTest");
            DontDestroyOnLoad(go);
            go.AddComponent<SmokeTest>();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;

        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert)
            {
                _errors++;
            }
        }

        private void Start()
        {
            Debug.Log($"[SmokeTest] Booted into '{SceneManager.GetActiveScene().name}'. Starting a new game…");
            SceneFlow.NewGame();
        }

        private void Update()
        {
            if (SceneManager.GetActiveScene().name != SceneFlow.GameScene)
            {
                return;
            }

            if (!_gameStarted)
            {
                _gameStarted = true;
                Debug.Log("[SmokeTest] Game scene loaded.");
            }

            _gameTime += Time.unscaledDeltaTime;
            if (_gameTime < RunSeconds)
            {
                return;
            }

            int customers = FindObjectsByType<Customers.Customer>(FindObjectsSortMode.None).Length;
            int drones = FindObjectsByType<Security.SecurityDrone>(FindObjectsSortMode.None).Length;
            bool ok = _errors == 0 && drones > 0;
            Debug.Log($"[SmokeTest] {(ok ? "SMOKETEST PASS" : "SMOKETEST FAIL")} — errors={_errors}, drones={drones}, customers={customers}, frames={Time.frameCount}");
            Application.Quit(ok ? 0 : 1);
        }
    }
}
