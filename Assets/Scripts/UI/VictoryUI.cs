using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class VictoryUI : MonoBehaviour
    {
        public void PlayAgain()
        {
            SceneManager.LoadScene(1);
        }

        public void GoToMenu()
        {
            SceneManager.LoadScene(0);
        }
    }
}