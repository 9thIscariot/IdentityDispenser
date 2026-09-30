using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>자판기 화면에는 이미지만 회전하고, 결과 이름은 하단 명판에 표시합니다.</summary>
public sealed class RoulletteUI : MonoBehaviour
{
    [SerializeField] private Font font;
    [SerializeField] private Texture2D machineBackground;
    [SerializeField] private Texture2D buttonNormal;
    [SerializeField] private Texture2D buttonPressed;
    // 현재 배경 PNG(1920×1080)의 픽셀 좌표를 기준으로 배치합니다.
    private const float DesignWidth = 1920, DesignHeight = 1080;
    // 투명 스크린보다 크게 깔아 프레임 가장자리까지 빈틈없이 채웁니다.
    private const float ScreenWidth = 1040, CellHeight = 520;
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

        // 이미지를 뒤에서 회전시키고, 앞쪽 PNG의 알파가 실제 화면 윤곽을 만듭니다.
        RectTransform viewport = Rect("Screen", machine, At(960, 400), new Vector2(ScreenWidth, CellHeight));
        viewport.gameObject.AddComponent<RectMask2D>();
        currentRow = Rect("CurrentIdentity", viewport, Vector2.zero, new Vector2(ScreenWidth, CellHeight));
        nextRow = Rect("NextIdentity", viewport, new Vector2(0, -CellHeight), new Vector2(ScreenWidth, CellHeight));
        currentPortrait = MakePortrait(currentRow);
        nextPortrait = MakePortrait(nextRow);
        background.transform.SetAsLastSibling();

        // 좌우 사각 패널은 클릭 기능이 없는 상태 표시창으로 사용합니다.
        Panel("CounterPlate", machine, At(514, 754), new Vector2(154, 134), new Color(0.055f, 0.065f, 0.065f));
        counter = Label("Counter", machine, At(514, 754), new Vector2(148, 122), "", 23, new Color(0.6f, 0.86f, 0.72f));
        Panel("StatusPlate", machine, At(1410, 757), new Vector2(154, 134), new Color(0.055f, 0.065f, 0.065f));
        status = Label("Status", machine, At(1410, 757), new Vector2(148, 122), "대기", 22, new Color(0.85f, 0.75f, 0.5f));

        spinButton = MakeButton("Spin", machine, At(744, 751), "인격\n뽑기");
        resetButton = MakeButton("Reset", machine, At(959, 751), "전체\n초기화");
        Button quitButton = MakeButton("Quit", machine, At(1177, 751), "룰렛\n종료");
        spinButton.onClick.AddListener(manager.Spin);
        resetButton.onClick.AddListener(manager.ResetDraws);
        quitButton.onClick.AddListener(manager.QuitRoulette);

        Panel("Nameplate", machine, At(960, 878), new Vector2(620, 44), new Color(0.045f, 0.045f, 0.045f));
        resultName = Label("ResultName", machine, At(960, 878), new Vector2(600, 40), "", 24,
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
        // 비율을 유지하며 화면을 채우고, 넘치는 부분은 중앙 기준으로 잘라냅니다.
        // UV로 자르면 회전 중에도 다음 인격의 칸을 침범하지 않습니다.
        float scale = Mathf.Max(ScreenWidth / texture.width, CellHeight / texture.height);
        float visibleWidth = ScreenWidth / (texture.width * scale);
        float visibleHeight = CellHeight / (texture.height * scale);
        portrait.rectTransform.sizeDelta = new Vector2(ScreenWidth, CellHeight);
        portrait.uvRect = new UnityEngine.Rect((1 - visibleWidth) / 2, (1 - visibleHeight) / 2,
            visibleWidth, visibleHeight);
    }

    private static RawImage MakePortrait(Transform parent)
    {
        RawImage image = Rect("Portrait", parent, Vector2.zero, new Vector2(ScreenWidth, CellHeight))
            .gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        image.enabled = false;
        return image;
    }

    private Button MakeButton(string name, Transform parent, Vector2 position, string title)
    {
        // PNG의 투명 여백을 포함한 크기입니다. 실제 버튼은 약 134×116입니다.
        RectTransform rect = Rect(name, parent, position, new Vector2(190, 190));
        var face = rect.gameObject.AddComponent<RawImage>();
        face.texture = buttonNormal;
        MachineButton button = rect.gameObject.AddComponent<MachineButton>();
        button.targetGraphic = face;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.08f, 1.08f, 1.08f);
        colors.pressedColor = Color.white;
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f);
        button.colors = colors;
        Text label = Label("Label", rect, new Vector2(0, 9), new Vector2(100, 68), title, 21,
            new Color(0.2f, 0.17f, 0.12f));
        label.fontStyle = FontStyle.Bold;
        button.Configure(face, buttonNormal, buttonPressed, label);
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
