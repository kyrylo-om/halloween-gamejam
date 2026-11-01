using UnityEngine;

/// <summary>
/// Scales OnGUI drawing the same way the scene CanvasScalers do
/// (1280x720 reference, match width/height 0.5), so IMGUI looks identical on any screen size.
/// Call Apply() at the start of OnGUI and use Width/Height instead of Screen.width/height.
/// </summary>
public static class GuiScale
{
    public static readonly Vector2 ReferenceResolution = new Vector2(1280f, 720f);

    public static float Factor => Mathf.Sqrt(Screen.width / ReferenceResolution.x * (Screen.height / ReferenceResolution.y));
    public static float Width => Screen.width / Factor;
    public static float Height => Screen.height / Factor;

    public static void Apply()
    {
        float f = Factor;
        GUI.matrix = Matrix4x4.Scale(new Vector3(f, f, 1f));
    }
}
