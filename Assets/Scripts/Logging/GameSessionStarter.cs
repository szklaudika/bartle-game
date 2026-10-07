using System.Collections;
using UnityEngine;

public class GameSessionStarter : MonoBehaviour
{
    private IEnumerator Start()
    {
        while (FirebaseGameAnalytics.Instance == null || !FirebaseGameAnalytics.Instance.IsReady)
        {
            yield return null;
        }

        FirebaseGameAnalytics.Instance.StartSession();
        Debug.Log("[GameSessionStarter] Firebase session started in gameplay scene.");
    }
}
