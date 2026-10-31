using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A smooth Catmull-Rom spline path that the Teacher (or other NPCs) can follow.
/// Developers can edit waypoints directly in the Scene view using position handles.
/// </summary>
[ExecuteAlways]
public class SplinePath : MonoBehaviour
{
    [Tooltip("List of path waypoints in local coordinates relative to this transform.")]
    public List<Vector3> points = new List<Vector3>()
    {
        new Vector3(-2f, 0f, -1.8f),
        new Vector3(-2f, 0f, 1.2f),
        new Vector3(0f, 0f, 2.0f),
        new Vector3(2f, 0f, 1.2f),
        new Vector3(2f, 0f, -1.8f),
        new Vector3(0f, 0f, -2.5f)
    };

    [Tooltip("Whether the spline connects back to the start forming a continuous loop.")]
    public bool isClosedLoop = true;

    [Header("Gizmo Display")]
    public Color pathColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    public Color pointColor = new Color(1f, 0.9f, 0.2f, 0.9f);
    public float handleSize = 0.2f;
    [Range(5, 40)]
    public int subdivisionsPerSegment = 20;

    /// <summary>
    /// Total number of control points.
    /// </summary>
    public int PointCount => points != null ? points.Count : 0;

    /// <summary>
    /// Total number of segments along the path.
    /// </summary>
    public int SegmentCount => points == null || points.Count < 2 ? 0 : (isClosedLoop ? points.Count : points.Count - 1);

    /// <summary>
    /// Returns world position of a waypoint at the given index.
    /// </summary>
    public Vector3 GetWorldPoint(int index)
    {
        if (points == null || points.Count == 0) return transform.position;
        index = Mathf.Clamp(index, 0, points.Count - 1);
        return transform.TransformPoint(points[index]);
    }

    /// <summary>
    /// Sets world position of a waypoint at the given index.
    /// </summary>
    public void SetWorldPoint(int index, Vector3 worldPos)
    {
        if (points == null || index < 0 || index >= points.Count) return;
        points[index] = transform.InverseTransformPoint(worldPos);
    }

    /// <summary>
    /// Evaluates point on a specific segment (from waypoint segmentIndex to next) with localT in [0, 1].
    /// </summary>
    public Vector3 EvaluateSegment(int segmentIndex, float localT)
    {
        if (points == null || points.Count < 2) return transform.position;
        int numSegments = SegmentCount;
        if (numSegments <= 0) return transform.position;

        segmentIndex = isClosedLoop ? ((segmentIndex % numSegments + numSegments) % numSegments) : Mathf.Clamp(segmentIndex, 0, numSegments - 1);
        localT = Mathf.Clamp01(localT);

        int p0 = GetIndex(segmentIndex - 1);
        int p1 = GetIndex(segmentIndex);
        int p2 = GetIndex(segmentIndex + 1);
        int p3 = GetIndex(segmentIndex + 2);

        Vector3 wp0 = transform.TransformPoint(points[p0]);
        Vector3 wp1 = transform.TransformPoint(points[p1]);
        Vector3 wp2 = transform.TransformPoint(points[p2]);
        Vector3 wp3 = transform.TransformPoint(points[p3]);

        return CatmullRom(wp0, wp1, wp2, wp3, localT);
    }

    /// <summary>
    /// Evaluates forward tangent / direction on a specific segment with localT in [0, 1].
    /// </summary>
    public Vector3 EvaluateSegmentTangent(int segmentIndex, float localT)
    {
        float delta = 0.01f;
        Vector3 pA = EvaluateSegment(segmentIndex, Mathf.Max(0f, localT - delta));
        Vector3 pB = EvaluateSegment(segmentIndex, Mathf.Min(1f, localT + delta));
        Vector3 dir = (pB - pA).normalized;
        return dir != Vector3.zero ? dir : transform.forward;
    }

    /// <summary>
    /// Calculates length in meters of a specific segment.
    /// </summary>
    public float GetSegmentLength(int segmentIndex, int samples = 15)
    {
        float len = 0f;
        Vector3 prev = EvaluateSegment(segmentIndex, 0f);
        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 curr = EvaluateSegment(segmentIndex, t);
            len += Vector3.Distance(prev, curr);
            prev = curr;
        }
        return Mathf.Max(0.01f, len);
    }

    /// <summary>
    /// Evaluates smooth Catmull-Rom spline point at global parameter t [0, 1].
    /// </summary>
    public Vector3 EvaluateSpline(float t)
    {
        if (points == null || points.Count < 2)
            return transform.position;

        t = isClosedLoop ? Mathf.Repeat(t, 1f) : Mathf.Clamp01(t);

        int numSegments = SegmentCount;
        float progress = t * numSegments;
        int p1 = Mathf.FloorToInt(progress);
        float localT = progress - p1;

        if (!isClosedLoop && p1 >= numSegments)
        {
            p1 = numSegments - 1;
            localT = 1f;
        }

        return EvaluateSegment(p1, localT);
    }

    /// <summary>
    /// Evaluates forward tangent / direction vector at global parameter t [0, 1].
    /// </summary>
    public Vector3 EvaluateTangent(float t)
    {
        float delta = 0.005f;
        Vector3 pA = EvaluateSpline(t - delta);
        Vector3 pB = EvaluateSpline(t + delta);
        Vector3 dir = (pB - pA).normalized;
        return dir != Vector3.zero ? dir : transform.forward;
    }

    /// <summary>
    /// Calculates approximate total length of the spline.
    /// </summary>
    public float CalculateTotalLength(int samples = 100)
    {
        float length = 0f;
        Vector3 prev = EvaluateSpline(0f);
        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 curr = EvaluateSpline(t);
            length += Vector3.Distance(prev, curr);
            prev = curr;
        }
        return length;
    }

    private int GetIndex(int i)
    {
        int count = points.Count;
        if (isClosedLoop)
        {
            return (i % count + count) % count;
        }
        return Mathf.Clamp(i, 0, count - 1);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    void OnDrawGizmos()
    {
        if (points == null || points.Count < 2) return;

        // Draw Spline Curve
        Gizmos.color = pathColor;
        int totalSegments = SegmentCount;
        int totalSteps = totalSegments * subdivisionsPerSegment;

        Vector3 prevPoint = EvaluateSpline(0f);
        for (int i = 1; i <= totalSteps; i++)
        {
            float t = (float)i / totalSteps;
            Vector3 nextPoint = EvaluateSpline(t);
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }

        // Draw Waypoint Spheres
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 worldPos = transform.TransformPoint(points[i]);
            Gizmos.color = (i == 0) ? Color.green : pointColor;
            Gizmos.DrawSphere(worldPos, handleSize * 0.7f);
        }
    }
}
