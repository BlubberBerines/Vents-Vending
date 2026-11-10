using UnityEngine;
using UnityEngine.SceneManagement;

namespace VentsAndVending.Core
{
    /// <summary>Scene transitions (GDD §18.3 GameManager responsibilities, kept deliberately small).</summary>
    public static class SceneFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string GameScene = "Station_Concourse_Ring";

        /// <summary>Set by the main menu; the game scene's SaveManager loads the save when true.</summary>
        public static bool ContinueRequested { get; set; }

        public static void NewGame()
        {
            ContinueRequested = false;
            Load(GameScene);
        }

        public static void ContinueGame()
        {
            ContinueRequested = true;
            Load(Saving.SaveManager.SavedSceneName());
        }

        public static void ToMainMenu() => Load(MainMenuScene);

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static void Load(string scene)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
        }
    }
}
