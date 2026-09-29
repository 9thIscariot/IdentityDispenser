using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>자판기 화면에는 이미지만 회전하고, 결과 이름은 하단 명판에 표시합니다.</summary>
public sealed class RoulletteUI : MonoBehaviour
{
    [SerializeField] private Font font;
    [SerializeField] private Texture2D machineBackground;
    private const float DesignWidth = 1500, DesignHeight = 829;
    private const float ScreenWidth = 914, CellHeight = 394;
    private RectTransform currentRow, nextRow;
    private RawImage currentPortrait, nextPortrait;
    private Text resultName, counter, status;
    private Button spinButton, resetButton;
    private RouletteManager manager;
    private Font generatedFont;

    public void Initialize(RouletteManager owner)
    {
        if (resultName != null) return;
        manager = owner;
        if (font == null)
        {
            generatedFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Malgun Gothic", "Apple SD Gothic Neo", "Noto Sans CJK KR", "Arial" }, 36);
            font = generatedFont;
        }
        var canvasObject = new GameObject("RouletteCanvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(DesignWidth, DesignHeight);
        // 화면 비율이 달라도 자판기 전체와 버튼이 화면 안에 들어옵니다.
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        RectTransform root = canvasObject.GetComponent<RectTransform>();
        RectTransform letterbox = Panel("Letterbox", root, Vector2.zero, Vector2.zero, Color.black);
        letterbox.anchorMin = Vector2.zero;
        letterbox.anchorMax = Vector2.one;
        RectTransform machine = Rect("Machine", root, Vector2.zero, new Vector2(DesignWidth, DesignHeight));
        RawImage background = Rect("MachineBackground", machine, Vector2.zero,
            new Vector2(DesignWidth, DesignHeight)).gameObject.AddComponent<RawImage>();
        background.texture = machineBackground;
        background.raycastTarget = false;
        background.color = machineBackground != null ? Color.white : new Color(0.12f, 0.12f, 0.12f);

        // 원본의 붉은 외곽 프레임을 남기고 화면 내부 문구를 가립니다.
        RectTransform viewport = Rect("Screen", machine, At(750, 279), new Vector2(ScreenWidth, CellHeight));
        var screenShape = viewport.gameObject.AddComponent<MachineShapeGraphic>();
        screenShape.color = new Color(0.015f, 0.018f, 0.02f);
        screenShape.Configure(45);
        screenShape.raycastTarget = false;
        viewport.gameObject.AddComponent<Mask>().showMaskGraphic = true;
        currentRow = Rect("CurrentIdentity", viewport, Vector2.zero, new Vector2(ScreenWidth, CellHeight));
        nextRow = Rect("NextIdentity", viewport, new Vector2(0, -CellHeight), new Vector2(ScreenWidth, CellHeight));
        currentPortrait = MakePortrait(currentRow);
        nextPortrait = MakePortrait(nextRow);

        // 좌우 사각 패널은 클릭 기능이 없는 상태 표시창으로 사용합니다.
        Panel("CounterPlate", machine, At(337, 599), new Vector2(143, 123), new Color(0.055f, 0.065f, 0.065f));
        counter = Label("Counter", machine, At(337, 599), new Vector2(137, 111), "", 23, new Color(0.6f, 0.86f, 0.72f));
        Panel("StatusPlate", machine, At(1160, 599), new Vector2(143, 123), new Color(0.055f, 0.065f, 0.065f));
        status = Label("Status", machine, At(1160, 599), new Vector2(137, 111), "대기", 22, new Color(0.85f, 0.75f, 0.5f));

        spinButton = MakeButton("Spin", machine, At(550, 591), "인격\n뽑기", new Color(0.09f, 0.5f, 0.23f));
        resetButton = MakeButton("Reset", machine, At(749, 591), "전체\n초기화", new Color(0.74f, 0.56f, 0.08f));
        Button quitButton = MakeButton("Quit", machine, At(949, 585), "룰렛\n종료", new Color(0.62f, 0.1f, 0.08f));
        spinButton.onClick.AddListener(manager.Spin);
        resetButton.onClick.AddListener(manager.ResetDraws);
        quitButton.onClick.AddListener(manager.QuitRoulette);

        Panel("Nameplate", machine, At(750, 712), new Vector2(574, 43), new Color(0.045f, 0.045f, 0.045f));
        resultName = Label("ResultName", machine, At(750, 712), new Vector2(554, 37), "", 24,
            new Color(0.9f, 0.84f, 0.66f));
        resultName.resizeTextForBestFit = true;
        resultName.resizeTextMinSize = 12;
        resultName.resizeTextMaxSize = 24;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var events = new GameObject("RouletteEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(transform, false);
        }
        ShowIdle();
    }

    public void RenderReel(IdentityData current, IdentityData next, float fraction)
    {
        SetPortrait(currentPortrait, current);
        SetPortrait(nextPortrait, next);
        currentRow.anchoredPosition = new Vector2(0, fraction * CellHeight);
        nextRow.anchoredPosition = new Vector2(0, (fraction - 1) * CellHeight);
    }

    public void ShowResult(IdentityData result)
    {
        RenderReel(result, result, 0);
        resultName.text = result.DisplayName;
    }

    public void ShowIdle(IdentityData preview = null)
    {
        resultName.text = "";
        if (preview != null) RenderReel(preview, preview, 0);
        else
        {
            currentPortrait.enabled = nextPortrait.enabled = false;
            currentRow.anchoredPosition = Vector2.zero;
            nextRow.anchoredPosition = new Vector2(0, -CellHeight);
        }
    }

    public void Refresh(int remaining, int total, bool spinning, string message)
    {
        counter.text = $"남은 인격\n{remaining} / {total}";
        status.text = spinning ? "추첨 중" : remaining == 0 ? "추첨 완료\n초기화 필요" :
            manager.LastResult != null ? "추첨 완료" : manager.YiSangOnly ? "대기\n이상 테스트" : "대기";
        spinButton.interactable = !spinning && remaining > 0;
        resetButton.interactable = !spinning;
        if (spinning) resultName.text = "";
    }

    private void SetPortrait(RawImage portrait, IdentityData identity)
    {
        Texture2D texture = manager.GetPortrait(identity.Id);
        portrait.enabled = texture != null;
        if (portrait.texture == texture) return;
        portrait.texture = texture;
        if (texture == null) return;
        float scale = Mathf.Min(ScreenWidth / texture.width, CellHeight / texture.height);
        portrait.rectTransform.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
    }

    private static RawImage MakePortrait(Transform parent)
    {
        RawImage image = Rect("Portrait", parent, Vector2.zero, new Vector2(ScreenWidth, CellHeight))
            .gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private Button MakeButton(string name, Transform parent, Vector2 position, string title, Color color)
    {
        RectTransform shadow = Rect(name + "Bezel", parent, position + new Vector2(0, -5), new Vector2(132, 112));
        var bevel = shadow.gameObject.AddComponent<MachineShapeGraphic>();
        bevel.Configure(0, true);
        bevel.color = new Color(0.025f, 0.025f, 0.022f);
        bevel.raycastTarget = false;
        RectTransform rect = Rect(name, parent, position, new Vector2(122, 102));
        var face = rect.gameObject.AddComponent<MachineShapeGraphic>();
        face.color = color;
        face.Configure(0, true, 1.65f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f);
        colors.disabledColor = new Color(0.38f, 0.38f, 0.38f);
        button.colors = colors;
        Text label = Label("Label", rect, Vector2.zero, new Vector2(108, 78), title, 21, new Color(0.97f, 0.96f, 0.87f));
        label.fontStyle = FontStyle.Bold;
        var textShadow = label.gameObject.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0, 0, 0, 0.7f);
        textShadow.effectDistance = new Vector2(1, -1);
        return button;
    }

    private static RectTransform Panel(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        RectTransform rect = Rect(name, parent, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private Text Label(string name, Transform parent, Vector2 position, Vector2 size, string value, int fontSize, Color color)
    {
        Text text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.supportRichText = false;
        text.raycastTarget = false;
        return text;
    }

    private static Vector2 At(float x, float y) => new Vector2(x - DesignWidth / 2, DesignHeight / 2 - y);

    private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        return rect;
    }

    private void OnDestroy()
    {
        if (generatedFont != null) Destroy(generatedFont);
    }
}
