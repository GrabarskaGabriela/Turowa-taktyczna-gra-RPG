using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class VictoryUI : MonoBehaviour
    {
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