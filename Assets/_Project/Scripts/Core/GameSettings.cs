using System;
using UnityEngine;

namespace VentsAndVending.Core
{
    /// <summary>Player preferences (GDD §17.5 accessibility subset), persisted in PlayerPrefs.</summary>
    public static class GameSettings
    {
        private const string SensitivityKey = "vv.mouseSensitivity";
        private const string FovKey = "vv.fieldOfView";
        private const string InvertYKey = "vv.invertY";
        private const string HoldCrouchKey = "vv.holdToCrouch";

        public const float DefaultSensitivity = 0.12f;
        public const float DefaultFov = 75f;

        public static event Action Changed;

        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
            set => Set(() => PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.02f, 0.6f)));
        }

        public static float FieldOfView
        {
            get => PlayerPrefs.GetFloat(FovKey, DefaultFov);
            set => Set(() => PlayerPrefs.SetFloat(FovKey, Mathf.Clamp(value, 60f, 100f)));
        }

        public static bool InvertY
        {
            get => PlayerPrefs.GetInt(InvertYKey, 0) == 1;
            set => Set(() => PlayerPrefs.SetInt(InvertYKey, value ? 1 : 0));
        }

        public static bool HoldToCrouch
        {
            get => PlayerPrefs.GetInt(HoldCrouchKey, 0) == 1;
            set => Set(() => PlayerPrefs.SetInt(HoldCrouchKey, value ? 1 : 0));
        }

        public static void ResetToDefaults() => Set(() =>
        {
            PlayerPrefs.DeleteKey(SensitivityKey);
            PlayerPrefs.DeleteKey(FovKey);
            PlayerPrefs.DeleteKey(InvertYKey);
            PlayerPrefs.DeleteKey(HoldCrouchKey);
        });

        private static void Set(Action write)
        {
            write();
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
