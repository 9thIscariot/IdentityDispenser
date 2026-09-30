using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>골목의 자판기에서 룰렛으로 진입하는 타이틀 화면입니다.</summary>
public sealed class TitleScreen : MonoBehaviour
{
    [SerializeField] private Texture2D alleyBackground;

    private const float Width = 1920f;
    private const float Height = 1080f;
    private Font titleFont;

    private void Awake()
    {
        titleFont = Font.CreateDynamicFontFromOSFont(new[] { "Georgia", "Times New Roman", "Arial" }, 56);

        var canvasObject = new GameObject("TitleCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(Width, Height);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        RectTransform root = canvasObject.GetComponent<RectTransform>();

        Image matte = Rect("BlackLetterbox", root, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
        matte.rectTransform.anchorMin = Vector2.zero;
        matte.rectTransform.anchorMax = Vector2.one;
        matte.color = Color.black;
        matte.raycastTarget = false;

        RawImage alley = Rect("AlleyAndVendingMachine", root, Vector2.zero,
            new Vector2(Width, Height)).gameObject.AddComponent<RawImage>();
        alley.texture = alleyBackground;
        alley.color = alleyBackground != null ? Color.white : new Color(0.1f, 0.1f, 0.1f);
        alley.raycastTarget = false;

        Text machineName = Label("MachineName", root, At(1553, 838), new Vector2(300, 48),
            "IDENTITY DISPENSER", titleFont, 25, new Color(0.84f, 0.79f, 0.69f));
        machineName.fontStyle = FontStyle.BoldAndItalic;
        machineName.resizeTextForBestFit = true;
        machineName.resizeTextMinSize = 20;
        machineName.resizeTextMaxSize = 25;
        Outline nameShadow = machineName.gameObject.AddComponent<Outline>();
        nameShadow.effectColor = new Color(0.08f, 0.065f, 0.05f, 0.9f);
        nameShadow.effectDistance = new Vector2(1, -1);

        RectTransform hotspot = Rect("MachineHotspot", root, At(1550, 648), new Vector2(470, 525));
        Image hitArea = hotspot.gameObject.AddComponent<Image>();
        hitArea.color = Color.clear;
        Button machine = hotspot.gameObject.AddComponent<Button>();
        machine.targetGraphic = hitArea;
        machine.onClick.AddListener(StartRoulette);

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("TitleEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
    }

    private void StartRoulette() => SceneManager.LoadScene("Roullet");

    private static Text Label(string name, Transform parent, Vector2 position, Vector2 size,
        string value, Font font, int fontSize, Color color)
    {
        Text label = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        label.font = font;
        label.text = value;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;
        return label;
    }

    private static Vector2 At(float x, float y) => new Vector2(x - Width / 2, Height / 2 - y);

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        RectTransform rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private void OnDestroy()
    {
        if (titleFont != null) Destroy(titleFont);
    }
}
