using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class GameOverUI : MonoBehaviour
    {
        public static void RestartGame()
        {
            SceneManager.LoadScene(1);
        }

        public static void GoToMenu()
        {
            SceneManager.LoadScene(0);
        }
    }
}
