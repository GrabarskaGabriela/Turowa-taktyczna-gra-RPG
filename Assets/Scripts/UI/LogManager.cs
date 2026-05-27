using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LogManager : MonoBehaviour
    {
        public GameObject logTextPrefab;
        public Transform container;
        public int maxLines = 18;
        public ScrollRect scrollRect;

        private readonly List<GameObject> _logLines = new List<GameObject>();

        public void AddLog(string message)
        {
            if (_logLines.Count >= maxLines)
            {
                Destroy(_logLines[0]);
                _logLines.RemoveAt(0);
            }

            GameObject newLine = Instantiate(logTextPrefab, container);
            TextMeshProUGUI textComp = newLine.GetComponent<TextMeshProUGUI>();
            if (textComp != null)
            {
                textComp.text = message;
                textComp.ForceMeshUpdate();
            }

            _logLines.Add(newLine);

            if (scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }

    public static class BattleLog
    {
        public static void Add(string message)
        {
            LogManager legacyLog = Object.FindAnyObjectByType<LogManager>();
            if (legacyLog != null && legacyLog.logTextPrefab != null && legacyLog.container != null)
            {
                legacyLog.AddLog(message);
                return;
            }

            CombatLogUI.EnsureInScene().AddLog(message);
        }
    }
}
