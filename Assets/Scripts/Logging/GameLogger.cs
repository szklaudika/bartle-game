/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTutorial.Manager        // <-- ADD THIS LINE
{
    public class GameLogger : MonoBehaviour
    {
        public static GameLogger Instance { get; private set; }

        [Header("Config")]
        [SerializeField] private string serverUrl = "https://your-backend.com/write-zombie-info";
        [SerializeField] private float sendIntervalSec = 60f;

        private readonly Queue<LogEvent> _queue = new();
        private string _userId;
        private string _sessionId;

        private void Awake()
        {
            if (Instance && Instance != this) Destroy(gameObject);
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _userId    = PlayerPrefs.GetString("playerid", "unknown");
                _sessionId = Guid.NewGuid().ToString();
                StartCoroutine(SendLoop());
            }
        }

        public void Log(string eventName, Vector3 pos, string target = "", int value = 0)
        {
            var ev = new LogEvent
            {
                userId    = _userId,
                sessionId = _sessionId,
                eventName = eventName,
                gameTime  = Time.timeSinceLevelLoad,
                posX      = pos.x,
                posY      = pos.y,
                posZ      = pos.z,
                target    = target,
                value     = value
            };
            lock (_queue) _queue.Enqueue(ev);
        }

        private IEnumerator SendLoop()
        {
            while (true)
            {
                yield return new WaitForSeconds(sendIntervalSec);
                Flush();
            }
        }

        private void Flush()
        {
            if (_queue.Count == 0) return;

            List<LogEvent> batch;
            lock (_queue)
            {
                batch = new List<LogEvent>(_queue.Count);
                while (_queue.Count > 0) batch.Add(_queue.Dequeue());
            }

            string json = JsonHelper.ToJsonArray(batch);
            StartCoroutine(Post(json));
        }

        private IEnumerator Post(string json)
        {
            using var www = new UnityWebRequest(serverUrl, "POST");
            byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler   = new UploadHandlerRaw(body);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("[GameLogger] send failed: " + www.error);
        }

        private static class JsonHelper
        {
            public static string ToJsonArray<T>(List<T> list)
            {
                var wrap = new { logs = list };
                return JsonUtility.ToJson(wrap);
            }
        }
    }
}
*/