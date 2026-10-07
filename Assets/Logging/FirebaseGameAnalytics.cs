using System;
using UnityEngine;
using System.Collections.Generic;

// Firebase
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using System.Threading.Tasks;

public class FirebaseGameAnalytics : MonoBehaviour
{
    public static FirebaseGameAnalytics Instance { get; private set; }

    public bool IsReady => _isInitialized && _user != null && _dbRoot != null;

    private FirebaseAuth _auth;
    private FirebaseUser _user;
    private DatabaseReference _dbRoot;
    private bool _isInitialized = false;

    private SessionData _currentSession;

    void Awake()
    {
        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                var status = task.Result;
                if (status != DependencyStatus.Available)
                {
                    Debug.LogError($"[FirebaseGameAnalytics] Could not resolve all Firebase dependencies: {status}");
                    return;
                }

                Debug.Log("[FirebaseGameAnalytics] Firebase dependencies OK.");

                _auth = FirebaseAuth.DefaultInstance;
                _dbRoot = FirebaseDatabase.DefaultInstance.RootReference;

                SignInAnonymously();
            });
    }

    private void SignInAnonymously()
{
    Debug.Log("[FirebaseGameAnalytics] Signing in anonymously...");

    _auth.SignInAnonymouslyAsync()
        .ContinueWithOnMainThread((Task<AuthResult> task) =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("[FirebaseGameAnalytics] Anonymous sign-in was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError($"[FirebaseGameAnalytics] Anonymous sign-in encountered an error: {task.Exception}");
                return;
            }

            AuthResult result = task.Result;
            _user = result.User;            // <= actual FirebaseUser
            _isInitialized = true;

            Debug.Log($"[FirebaseGameAnalytics] Signed in anonymously as UID: {_user.UserId}");
        });
}


    // =========================
    //  Session API (public)
    // =========================

    public void StartSession()
    {
        if (!IsReady)
        {
            Debug.LogWarning("[FirebaseGameAnalytics] Not ready yet – cannot start session.");
            return;
        }

        if (_currentSession != null)
        {
            Debug.LogWarning("[FirebaseGameAnalytics] A session is already active.");
            return;
        }

        _currentSession = new SessionData
        {
            sessionId = Guid.NewGuid().ToString(),
            userId = _user.UserId,
            sessionStart = DateTime.UtcNow.ToString("o"), // ISO 8601
            buildVersion = Application.version
        };

        Debug.Log($"[FirebaseGameAnalytics] Session started. SessionId = {_currentSession.sessionId}");
    }

    public void LogItemPickup(string itemId)
    {
        if (_currentSession == null)
        {
            Debug.LogWarning("[FirebaseGameAnalytics] No active session – cannot log item pickup.");
            return;
        }

        var pickup = new ItemPickupEvent
        {
            itemId = itemId,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        _currentSession.itemsPicked.Add(pickup);

        Debug.Log($"[FirebaseGameAnalytics] Logged item pickup: {itemId}");
    }


    public void LogGameplayEvent(string eventName, Vector3 pos, string target = "", int value = 0)
    {
        if (_currentSession == null)
        {
            Debug.LogWarning($"[FirebaseGameAnalytics] No active session – cannot log event {eventName}.");
            return;
        }

        var ev = new LogEvent
        {
            userId    = _currentSession.userId,
            sessionId = _currentSession.sessionId,
            eventName = eventName,
            gameTime  = Time.timeSinceLevelLoad,
            posX      = pos.x,
            posY      = pos.y,
            posZ      = pos.z,
            target    = target,
            value     = value
        };

        _currentSession.events.Add(ev);
        Debug.Log($"[FirebaseGameAnalytics] Logged event: {eventName} ({target}, value={value})");
    }

    public void LogGameplayEvent(string eventName, string target = "", int value = 0)
    {
        LogGameplayEvent(eventName, Vector3.zero, target, value);
    }


    /// <summary>
    /// Ends the current session and writes it to the Realtime Database.
    /// gameOver=true means this was a death / game over.
    /// </summary>
    public void EndSession(bool gameOver = true)
    {
        if (_currentSession == null)
        {
            Debug.LogWarning("[FirebaseGameAnalytics] No active session to end.");
            return;
        }

        _currentSession.sessionEnd = DateTime.UtcNow.ToString("o");
        _currentSession.gameOver = gameOver;

        // Make a copy to write (in case we clear _currentSession immediately).
        var finishedSession = _currentSession;
        _currentSession = null;

        WriteSessionToDatabase(finishedSession);
    }

    // =========================
    //  Internal DB write
    // =========================

    private void WriteSessionToDatabase(SessionData session)
    {
        if (!IsReady)
        {
            Debug.LogWarning("[FirebaseGameAnalytics] Not ready – cannot write session to DB.");
            return;
        }

        // sessions/{userId}/{sessionId}
        string path = $"sessions/{session.userId}/{session.sessionId}";
        string json = JsonUtility.ToJson(session);

        Debug.Log($"[FirebaseGameAnalytics] Writing session to DB at path: {path}");

        _dbRoot.Child(path)
            .SetRawJsonValueAsync(json)
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCanceled)
                {
                    Debug.LogError("[FirebaseGameAnalytics] SetRawJsonValueAsync was canceled.");
                    return;
                }
                if (task.IsFaulted)
                {
                    Debug.LogError($"[FirebaseGameAnalytics] SetRawJsonValueAsync encountered an error: {task.Exception}");
                    return;
                }

                Debug.Log("[FirebaseGameAnalytics] Session written successfully.");
            });
    }
}
