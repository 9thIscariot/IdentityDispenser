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
    private Text resultName, status;
    private Button spinButton, stopButton;
    private RectTransform settingsPanel, drawPage, identityPage, displayPage;
    private Button settingsButton;
    private SelectionVisual[] categories, sinnerChoices, identityChoices, displayChoices;
    private SelectionVisual[] sinnerSelections;
    private int selectedCategory;
    private static readonly Color SelectedColor = Color.white;
    private static readonly Color UnselectedColor = new Color(0.84f, 0.12f, 0.12f);

    private sealed class SelectionVisual
    {
        public Text Label;
        public Image[] Edges;

        public void SetSelected(bool selected)
        {
            Color color = selected ? SelectedColor : UnselectedColor;
            Label.color = color;
            foreach (Image edge in Edges) edge.color = color;
        }
    }
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

        // 자판기 이미지 자체의 패널 질감을 살리고, 왼쪽 패널은 설정 버튼으로 사용합니다.
        Text settingsLabel = Label("Settings", machine, At(514, 754), new Vector2(148, 122),
            "설정", 24, Color.black);
        settingsLabel.fontStyle = FontStyle.Bold;
        settingsLabel.raycastTarget = true;
        settingsButton = settingsLabel.gameObject.AddComponent<Button>();
        settingsButton.targetGraphic = settingsLabel;
        settingsButton.onClick.AddListener(ToggleSettings);
        status = Label("Status", machine, At(1410, 757), new Vector2(148, 122), "대기", 24, Color.black);
        status.fontStyle = FontStyle.Bold;

        spinButton = MakeButton("Spin", machine, At(744, 751), "뽑기");
        stopButton = MakeButton("Stop", machine, At(959, 751), "정지");
        Button quitButton = MakeButton("Quit", machine, At(1177, 751), "종료");
        spinButton.onClick.AddListener(manager.Spin);
        stopButton.onClick.AddListener(manager.StopSpin);
        quitButton.onClick.AddListener(manager.QuitRoulette);

        Panel("Nameplate", machine, At(960, 878), new Vector2(620, 44), new Color(0.045f, 0.045f, 0.045f));
        resultName = Label("ResultName", machine, At(960, 878), new Vector2(600, 40), "", 24,
            new Color(0.9f, 0.84f, 0.66f));
        resultName.resizeTextForBestFit = true;
        resultName.resizeTextMinSize = 12;
        resultName.resizeTextMaxSize = 24;

        BuildSettingsScreen(viewport);

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

    public void ShowIdle()
    {
        resultName.text = "";
        // 이미지를 숨기면 뒤쪽의 검은 배경이 보여 꺼진 화면처럼 보입니다.
        currentPortrait.enabled = nextPortrait.enabled = false;
        currentRow.anchoredPosition = Vector2.zero;
        nextRow.anchoredPosition = new Vector2(0, -CellHeight);
    }

    public void Refresh(int remaining, int total, bool spinning, string message)
    {
        status.text = spinning ? "뽑기 중" :
            manager.LastResult != null ? "뽑기 완료" : "대기";
        spinButton.interactable = !spinning && remaining > 0 &&
            (settingsPanel == null || !settingsPanel.gameObject.activeSelf);
        stopButton.interactable = spinning && !manager.IsStopping;
        if (settingsButton != null) settingsButton.interactable = !spinning;
        if (spinning) resultName.text = "";
    }

    private void SetPortrait(RawImage portrait, IdentityData identity)
    {
        Texture2D texture = manager.GetPortrait(identity.Id);
        portrait.enabled = texture != null;
        if (portrait.texture == texture) return;
        portrait.texture = texture;
        if (texture == null) return;
        // 카드 형태의 세로 이미지는 얼굴과 카드 전체가 보이도록 축소해 표시합니다.
        if (texture.width < texture.height)
        {
            // 회전 영역은 프레임 뒤까지 확장되어 있으므로, 카드는 실제 창 안쪽 높이에 맞춥니다.
            float fit = Mathf.Min(ScreenWidth / texture.width, 450f / texture.height);
            portrait.rectTransform.sizeDelta = new Vector2(texture.width * fit, texture.height * fit);
            portrait.uvRect = new UnityEngine.Rect(0, 0, 1, 1);
            return;
        }
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

    private void ToggleSettings()
    {
        if (manager.IsSpinning) return;
        settingsPanel.gameObject.SetActive(!settingsPanel.gameObject.activeSelf);
        if (settingsPanel.gameObject.activeSelf) RenderSettings();
        Refresh(manager.RemainingCount, 0, manager.IsSpinning, "");
    }

    private void BuildSettingsScreen(RectTransform viewport)
    {
        // 화면 위에 불투명한 검은 면을 두되 자판기 PNG 프레임의 뒤쪽에 배치합니다.
        settingsPanel = Panel("SettingsScreen", viewport, Vector2.zero,
            new Vector2(ScreenWidth, CellHeight), Color.black);
        categories = new[]
        {
            OutlinedButton("DrawCategory", settingsPanel, new Vector2(-363, 144),
                new Vector2(218, 72), "뽑기 설정", 25, () => SelectSettingsCategory(0)),
            OutlinedButton("IdentityCategory", settingsPanel, new Vector2(-363, 46),
                new Vector2(218, 72), "인격 설정", 25, () => SelectSettingsCategory(1)),
            OutlinedButton("DisplayCategory", settingsPanel, new Vector2(-363, -52),
                new Vector2(218, 72), "화면 설정", 25, () => SelectSettingsCategory(2))
        };
        OutlinedButton("CloseSettings", settingsPanel, new Vector2(-363, -160),
            new Vector2(218, 72), "닫기", 25, ToggleSettings).SetSelected(false);
        Panel("Divider", settingsPanel, new Vector2(-229, 0),
            new Vector2(2, 426), UnselectedColor);

        drawPage = Rect("DrawSettings", settingsPanel, new Vector2(131, 0), new Vector2(690, 432));
        Frame(drawPage, new Vector2(690, 432));
        Label("DrawTitle", drawPage, new Vector2(0, 164), new Vector2(610, 54),
            "뽑기 설정", 30, SelectedColor).fontStyle = FontStyle.Bold;
        SettingsRow(drawPage, "SinnerDuplicates", "수감자 중복 설정", 67,
            out sinnerChoices, value => { manager.SetSinnerDuplicates(value); RenderSettings(); });
        SettingsRow(drawPage, "IdentityDuplicates", "인격 중복 설정", -61,
            out identityChoices, value => { manager.SetIdentityDuplicates(value); RenderSettings(); });

        identityPage = Rect("IdentitySettings", settingsPanel, new Vector2(131, 0), new Vector2(690, 432));
        Frame(identityPage, new Vector2(690, 432));
        Label("IdentityTitle", identityPage, new Vector2(0, 174), new Vector2(610, 50),
            "인격 설정", 30, SelectedColor).fontStyle = FontStyle.Bold;
        sinnerSelections = new SelectionVisual[RouletteManager.SinnerOrder.Length];
        for (int i = 0; i < sinnerSelections.Length; i++)
        {
            string sinner = RouletteManager.SinnerOrder[i];
            int column = i % 3, row = i / 3;
            sinnerSelections[i] = OutlinedButton("Sinner" + (i + 1), identityPage,
                new Vector2(-220 + column * 220, 93 - row * 77),
                new Vector2(184, 60), sinner, 23, () => ToggleSinner(sinner));
        }

        displayPage = Rect("DisplaySettings", settingsPanel, new Vector2(131, 0), new Vector2(690, 432));
        Frame(displayPage, new Vector2(690, 432));
        Label("DisplayTitle", displayPage, new Vector2(0, 164), new Vector2(610, 54),
            "화면 설정", 30, SelectedColor).fontStyle = FontStyle.Bold;
        Label("DisplayModeLabel", displayPage, new Vector2(-154, 54), new Vector2(280, 58),
            "화면 모드", 25, SelectedColor).fontStyle = FontStyle.Bold;
        displayChoices = new[]
        {
            OutlinedButton("Fullscreen", displayPage, new Vector2(86, 54),
                new Vector2(144, 66), "전체 화면", 22, () => SetDisplayMode(true)),
            OutlinedButton("Windowed", displayPage, new Vector2(250, 54),
                new Vector2(144, 66), "창 모드", 22, () => SetDisplayMode(false))
        };
        settingsPanel.gameObject.SetActive(false);
    }

    private void SettingsRow(RectTransform parent, string name, string caption, float y,
        out SelectionVisual[] choices, System.Action<bool> onChange)
    {
        Text heading = Label(name + "Label", parent, new Vector2(-154, y),
            new Vector2(280, 62), caption, 23, SelectedColor);
        heading.fontStyle = FontStyle.Bold;
        choices = new[]
        {
            OutlinedButton(name + "On", parent, new Vector2(91, y),
                new Vector2(98, 66), "ON", 25, () => onChange(true)),
            OutlinedButton(name + "Off", parent, new Vector2(246, y),
                new Vector2(98, 66), "OFF", 25, () => onChange(false))
        };
    }

    private void SelectSettingsCategory(int index)
    {
        selectedCategory = index;
        RenderSettings();
    }

    private void ToggleSinner(string sinner)
    {
        manager.SetSinnerSelected(sinner, !manager.IsSinnerSelected(sinner));
        RenderSettings();
    }

    private void RenderSettings()
    {
        for (int i = 0; i < categories.Length; i++) categories[i].SetSelected(i == selectedCategory);
        drawPage.gameObject.SetActive(selectedCategory == 0);
        identityPage.gameObject.SetActive(selectedCategory == 1);
        displayPage.gameObject.SetActive(selectedCategory == 2);
        for (int i = 0; i < sinnerSelections.Length; i++)
            sinnerSelections[i].SetSelected(manager.IsSinnerSelected(RouletteManager.SinnerOrder[i]));
        sinnerChoices[0].SetSelected(manager.AllowSinnerDuplicates);
        sinnerChoices[1].SetSelected(!manager.AllowSinnerDuplicates);
        identityChoices[0].SetSelected(manager.AllowIdentityDuplicates);
        identityChoices[1].SetSelected(!manager.AllowIdentityDuplicates);
        bool fullscreen = Screen.fullScreenMode != FullScreenMode.Windowed;
        displayChoices[0].SetSelected(fullscreen);
        displayChoices[1].SetSelected(!fullscreen);
    }

    private void SetDisplayMode(bool fullscreen)
    {
        DisplaySettings display = FindFirstObjectByType<DisplaySettings>();
        if (display != null)
        {
            if (fullscreen) display.SetFullscreen();
            else display.SetWindowed();
        }
        else
        {
            Screen.SetResolution(1920, 1080,
                fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }
        displayChoices[0].SetSelected(fullscreen);
        displayChoices[1].SetSelected(!fullscreen);
    }

    private SelectionVisual OutlinedButton(string name, Transform parent, Vector2 position,
        Vector2 size, string caption, int fontSize, UnityEngine.Events.UnityAction action)
    {
        RectTransform rect = Rect(name, parent, position, size);
        Image face = rect.gameObject.AddComponent<Image>();
        face.color = new Color(0, 0, 0, 0.01f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = face;
        button.onClick.AddListener(action);
        Text label = Label("Label", rect, Vector2.zero, size - new Vector2(8, 8),
            caption, fontSize, UnselectedColor);
        label.fontStyle = FontStyle.Bold;
        return new SelectionVisual { Label = label, Edges = Frame(rect, size) };
    }

    private static Image[] Frame(Transform parent, Vector2 size)
    {
        const float border = 2f;
        return new[]
        {
            Panel("Top", parent, new Vector2(0, size.y / 2 - border / 2),
                new Vector2(size.x, border), UnselectedColor).GetComponent<Image>(),
            Panel("Bottom", parent, new Vector2(0, -size.y / 2 + border / 2),
                new Vector2(size.x, border), UnselectedColor).GetComponent<Image>(),
            Panel("Left", parent, new Vector2(-size.x / 2 + border / 2, 0),
                new Vector2(border, size.y), UnselectedColor).GetComponent<Image>(),
            Panel("Right", parent, new Vector2(size.x / 2 - border / 2, 0),
                new Vector2(border, size.y), UnselectedColor).GetComponent<Image>()
        };
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
