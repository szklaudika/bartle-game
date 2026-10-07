using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemPickupEvent
{
    public string itemId;
    public string timestamp; // ISO 8601 (UTC)
}

[Serializable]
public class SessionData
{
    public string sessionId;
    public string userId;

    public string sessionStart;
    public string sessionEnd;

    public bool gameOver;

    public List<ItemPickupEvent> itemsPicked = new List<ItemPickupEvent>();

    public List<LogEvent> events = new List<LogEvent>();

    public string buildVersion;
}
