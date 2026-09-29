using UnityEngine;
using UnityEngine.UI;

/// <summary>자판기의 둥근 화면 마스크와 원형 버튼을 위한 UI 도형입니다.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MachineShapeGraphic : MaskableGraphic, ICanvasRaycastFilter
{
    private float radius;
    private bool ellipse;
    private float centerBrightness = 1;

    public void Configure(float cornerRadius, bool isEllipse = false, float brightness = 1)
    {
        radius = cornerRadius;
        ellipse = isEllipse;
        centerBrightness = brightness;
        SetVerticesDirty();
    }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        Rect rect = rectTransform.rect;
        Color centerColor = color * centerBrightness;
        centerColor.a = color.a;
        mesh.AddVert(rect.center, centerColor, Vector2.one * 0.5f);
        const int segments = 64;
        float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) * 0.5f);
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2 / segments;
            Vector2 position;
            if (ellipse)
                position = rect.center + new Vector2(Mathf.Cos(angle) * rect.width / 2, Mathf.Sin(angle) * rect.height / 2);
            else
            {
                int quadrant = i / 16;
                Vector2 corner = new Vector2(quadrant == 0 || quadrant == 3 ? rect.xMax - r : rect.xMin + r,
                    quadrant < 2 ? rect.yMax - r : rect.yMin + r);
                angle = (quadrant * 90 + (i % 16) * 90f / 15) * Mathf.Deg2Rad;
                position = corner + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            }
            mesh.AddVert(position, color, Vector2.zero);
        }
        for (int i = 0; i < segments; i++)
            mesh.AddTriangle(0, i + 1, (i + 1) % segments + 1);
    }

    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 point))
            return false;
        Rect rect = rectTransform.rect;
        if (rect.width <= 0 || rect.height <= 0) return false;
        Vector2 offset = point - rect.center;
        if (ellipse)
            return Mathf.Pow(offset.x * 2 / rect.width, 2) + Mathf.Pow(offset.y * 2 / rect.height, 2) <= 1;
        float r = Mathf.Min(radius, Mathf.Min(rect.width, rect.height) / 2);
        Vector2 cornerOffset = new Vector2(Mathf.Max(0, Mathf.Abs(offset.x) - rect.width / 2 + r),
            Mathf.Max(0, Mathf.Abs(offset.y) - rect.height / 2 + r));
        return rect.Contains(point) && cornerOffset.sqrMagnitude <= r * r;
    }
}
