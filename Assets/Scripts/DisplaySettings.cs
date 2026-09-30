using UnityEngine;

public class DisplaySettings : MonoBehaviour
{
    private const int WIDTH = 1920;
    private const int HEIGHT = 1080;
    private const int WINDOW_WIDTH = 1920;
    private const int WINDOW_HEIGHT = 1080;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void UseWindowedOnStartup()
    {
        Screen.SetResolution(WINDOW_WIDTH, WINDOW_HEIGHT, FullScreenMode.Windowed);
    }

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
            WINDOW_WIDTH,
            WINDOW_HEIGHT,
            FullScreenMode.Windowed
        );
    }
}
