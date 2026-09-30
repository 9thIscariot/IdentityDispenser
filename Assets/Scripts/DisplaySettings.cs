using UnityEngine;

public class DisplaySettings : MonoBehaviour
{
    private const int WIDTH = 1920;
    private const int HEIGHT = 1080;

    private void Start()
    {
        SetResolution();
    }

    private void SetResolution()
    {
        Screen.SetResolution(
            WIDTH,
            HEIGHT,
            FullScreenMode.FullScreenWindow
        );
    }

    // 나중에 UI 버튼이나 Toggle에서 사용할 수 있음
    public void SetFullscreen()
    {
        Screen.SetResolution(
            WIDTH,
            HEIGHT,
            FullScreenMode.FullScreenWindow
        );
    }

    public void SetWindowed()
    {
        Screen.SetResolution(
            WIDTH,
            HEIGHT,
            FullScreenMode.Windowed
        );
    }
}