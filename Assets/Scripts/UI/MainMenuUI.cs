using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public static void StartGame()
        {
            SceneManager.LoadScene(1);
        }
        public static void QuitGame()
        {
            Application.Quit();
        }
    }
}
