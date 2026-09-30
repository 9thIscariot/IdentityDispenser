using UnityEngine;
using UnityEngine.UI;

/// <summary>마우스와 키보드의 버튼 상태에 맞춰 눌림 이미지와 글자 위치를 전환합니다.</summary>
public sealed class MachineButton : Button
{
    private RawImage face;
    private Texture2D normalTexture, pressedTexture;
    private Text label;

    public void Configure(RawImage image, Texture2D normal, Texture2D pressed, Text title)
    {
        face = image;
        normalTexture = normal;
        pressedTexture = pressed;
        label = title;
        DoStateTransition(currentSelectionState, true);
    }

    protected override void DoStateTransition(SelectionState state, bool instant)
    {
        base.DoStateTransition(state, instant);
        if (face == null) return;
        bool depressed = state == SelectionState.Pressed || state == SelectionState.Disabled;
        face.texture = depressed && pressedTexture != null ? pressedTexture : normalTexture;
        if (label == null) return;
        label.rectTransform.anchoredPosition = new Vector2(0, depressed ? -8 : 9);
        label.color = depressed ? new Color(0.9f, 0.85f, 0.73f) : new Color(0.2f, 0.17f, 0.12f);
        if (state == SelectionState.Disabled) label.color *= new Color(0.65f, 0.65f, 0.65f, 1);
    }
}
