using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class GameOverUI : MonoBehaviour
    {
        public void RestartGame()
        {
            SceneManager.LoadScene(1);
        }

        public void GoToMenu()
        {
            SceneManager.LoadScene(0);
        }

        public void QuitGame()
        {
            ApplicationQuitter.Quit();
        }
    }
}
