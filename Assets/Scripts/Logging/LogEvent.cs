using System;

[Serializable]
public class LogEvent
{
    public string userId;        // filled once at start
    public string sessionId;     // System.Guid.NewGuid() at start
    public string eventName;     // jump | hitenemy | takedamage | shoot | collect | finishlevel | lose
    public float  gameTime;      // Time.timeSinceLevelLoad
    public float  posX, posY, posZ;
    public string target;        // enemy name, collectable name, etc. (optional)
    public int    value;         // damage dealt, item count, etc. (optional)

    public string ToJson() => UnityEngine.JsonUtility.ToJson(this);
}