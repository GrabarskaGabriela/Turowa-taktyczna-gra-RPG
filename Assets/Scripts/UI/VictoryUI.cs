using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class VictoryUI : MonoBehaviour
    {
        public static void GoToMenu()
        {
            SceneManager.LoadScene(0);
        }

        public static void QuitGame()
        {
            Application.Quit();
        }
    }
}
