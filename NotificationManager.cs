using System.Collections.Generic;
using UnityEngine;

namespace TikTokGiftsToEnemies
{
    public class NotificationManager : MonoBehaviour
    {
        public static NotificationManager Instance { get; private set; }

        private List<(string text, float expiry)> _notifications = new List<(string text, float expiry)>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void Show(string message, float duration = 3f)
        {
            if (!PluginConfig.ShowOnScreenNotifications.Value) return;

            _notifications.Add((message, Time.time + duration));
        }

        void OnGUI()
        {
            if (_notifications.Count == 0) return;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 20;
            style.normal.textColor = Color.yellow;
            style.alignment = TextAnchor.UpperRight;

            float yPos = 10f;
            for (int i = _notifications.Count - 1; i >= 0; i--)
            {
                var notif = _notifications[i];
                if (Time.time > notif.expiry)
                {
                    _notifications.RemoveAt(i);
                    continue;
                }

                GUI.Label(new Rect(Screen.width - 410, yPos, 400, 30), notif.text, style);
                yPos += 30f;
            }
        }
    }
}
