using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public void StartGame()
        {
            SceneManager.LoadScene(1);
        }
        public void QuitGame()
        {
            ApplicationQuitter.Quit();
        }
    }
}
