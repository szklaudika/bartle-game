using UnityEngine;
using System.Collections.Generic;

public class SplinePath : MonoBehaviour
{
    [Header("Spline Control Points")]
    [Tooltip("Drag child transforms or scene objects here to define the path")]
    [SerializeField] private List<Transform> controlPoints = new List<Transform>();
    
    public List<Transform> ControlPoints => controlPoints; // Public getter

    [Header("Settings")]
    [Tooltip("Should the path loop back to the start?")]
    public bool isLooping = false;

    [Tooltip("Number of interpolated points for gizmo visualization")]
    [Range(10, 100)]
    public int gizmoSegments = 20;

    // Get a point on the spline at time 't' (0 to 1)
    public Vector3 GetPoint(float t)
    {
        if (controlPoints.Count < 2) return Vector3.zero;

        // Ensure 't' is clamped and handle looping
        float clampedT = Mathf.Clamp01(t);
        int numSegments = isLooping ? controlPoints.Count : controlPoints.Count - 1;
        if (numSegments == 0) return controlPoints[0].position;

        float segmentT = clampedT * numSegments;
        int segmentIndex = Mathf.FloorToInt(segmentT);
        segmentIndex = isLooping ? segmentIndex % controlPoints.Count : Mathf.Min(segmentIndex, controlPoints.Count - 2);
        float localT = segmentT - segmentIndex;

        // Get the four points needed for the Catmull-Rom calculation
        Vector3 p0 = GetControlPoint(segmentIndex - 1);
        Vector3 p1 = GetControlPoint(segmentIndex);
        Vector3 p2 = GetControlPoint(segmentIndex + 1);
        Vector3 p3 = GetControlPoint(segmentIndex + 2);

        return CalculateCatmullRomPoint(localT, p0, p1, p2, p3);
    }

    // Helper to safely get a control point index, handling looping and edge cases
    private Vector3 GetControlPoint(int index)
    {
        if (controlPoints.Count == 0) return Vector3.zero;
        if (isLooping)
        {
            index = (index + controlPoints.Count) % controlPoints.Count;
            return controlPoints[index].position;
        }
        else
        {
            index = Mathf.Clamp(index, 0, controlPoints.Count - 1);
            return controlPoints[index].position;
        }
    }

    // Catmull-Rom interpolation
    private Vector3 CalculateCatmullRomPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        float b0 = -0.5f * t3 + t2 - 0.5f * t;
        float b1 = 1.5f * t3 - 2.5f * t2 + 1.0f;
        float b2 = -1.5f * t3 + 2.0f * t2 + 0.5f * t;
        float b3 = 0.5f * t3 - 0.5f * t2;

        return b0 * p0 + b1 * p1 + b2 * p2 + b3 * p3;
    }

    // Optional: Get the direction/forward vector at a point on the spline
    public Vector3 GetDirection(float t)
    {
        float delta = 0.01f;
        Vector3 point = GetPoint(t);
        Vector3 nextPoint = GetPoint(Mathf.Clamp01(t + delta));
        return (nextPoint - point).normalized;
    }

    // Visualize the path in the editor
    private void OnDrawGizmos()
    {
        if (controlPoints == null || controlPoints.Count < 2) return;

        Gizmos.color = Color.cyan;
        int numSegments = isLooping ? controlPoints.Count : controlPoints.Count - 1;

        for (int i = 0; i < numSegments; i++)
        {
            int startIndex = i;
            for (int j = 0; j < gizmoSegments; j++)
            {
                float t1 = (float)j / gizmoSegments;
                float t2 = (float)(j + 1) / gizmoSegments;
                Vector3 p1 = GetPoint((startIndex + t1) / controlPoints.Count);
                Vector3 p2 = GetPoint((startIndex + t2) / controlPoints.Count);
                Gizmos.DrawLine(p1, p2);
            }
        }

        // Draw control points
        Gizmos.color = Color.yellow;
        foreach (var point in controlPoints)
        {
            if (point != null) Gizmos.DrawSphere(point.position, 0.1f);
        }
    }
}