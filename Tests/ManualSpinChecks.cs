// Unity 없이 실제 Manager/Animator의 상태 전이를 검증하는 최소 런타임 대역입니다.
using System;
using System.Reflection;

namespace UnityEngine
{
    public class MonoBehaviour
    {
        public bool isActiveAndEnabled = true;
        public bool enabled = true;
        public GameObject gameObject = new GameObject();
        public T GetComponent<T>() where T : class => null;
    }
    public class GameObject { public T AddComponent<T>() where T : new() => new T(); }
    public class Texture2D { }
    public static class Time { public static float unscaledDeltaTime; }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
    public static class Random
    {
        private static readonly System.Random Generator = new System.Random(17);
        public static int Range(int min, int max) => Generator.Next(min, max);
    }
    public class SerializeField : Attribute { }
    public class DisallowMultipleComponent : Attribute { }
    public class HeaderAttribute : Attribute { public HeaderAttribute(string value) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string value) { } }
    public class MinAttribute : Attribute { public MinAttribute(float value) { } }
}
namespace UnityEngine.SceneManagement
{
    public static class SceneManager
    {
        public static string Loaded;
        public static void LoadScene(string name) { Loaded = name; }
    }
}
public sealed class RoulletteUI
{
    public IdentityData Shown;
    public int Frames;
    public void Initialize(RouletteManager owner) { }
    public void ShowIdle() { Shown = null; }
    public void ShowResult(IdentityData value) { Shown = value; }
    public void RenderReel(IdentityData a, IdentityData b, float fraction) { Frames++; }
    public void Refresh(int count, int total, bool spinning, string message) { }
}
public static class ManualSpinChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static void Tick(RouletteAnimatior reel, int frames, float delta)
    {
        UnityEngine.Time.unscaledDeltaTime = delta;
        for (int i = 0; i < frames; i++)
            typeof(RouletteAnimatior).GetMethod("Update", Private).Invoke(reel, null);
    }
    public static string Run()
    {
        int paths = 0;
        foreach (int count in new[] { 1, 2, 16, 188 })
        for (int target = 0; target < count; target++)
        foreach (double fraction in new[] { 0.0, 0.13, 0.999 })
        foreach (double dt in new[] { 1.0 / 30, 1.0 / 144, 0.25 })
        {
            double start = fraction * count;
            var plan = ReelSpinPlan.BrakeFrom(count, target, 1, 4, start);
            Check(Math.Abs(plan.PositionAt(0) - start) < 1e-8, "No jump at stop request");
            Check(plan.Duration >= 4 - 1e-8 && plan.Duration < 8 + 1e-8, "Brake duration bounds");
            double previous = start, stepBefore = double.PositiveInfinity;
            for (double t = dt; t < plan.Duration + dt; t += dt)
            {
                double position = plan.PositionAt(t), step = position - previous;
                Check(step >= -1e-8 && step <= stepBefore + 1e-8, "Continuous deceleration");
                previous = position;
                stepBefore = step;
            }
            Check(plan.EndPosition % count == target, "Exact selected identity");
            paths++;
        }
        var manager = new RouletteManager();
        typeof(RouletteManager).GetField("portraitsOnly", Private).SetValue(manager, false);
        typeof(RouletteManager).GetMethod("Awake", Private).Invoke(manager, null);
        var reel = (RouletteAnimatior)typeof(RouletteManager).GetField("reel", Private).GetValue(manager);
        var view = (RoulletteUI)typeof(RouletteManager).GetField("view", Private).GetValue(manager);
        int total = manager.RemainingCount, results = 0;
        foreach (string sinner in RouletteManager.SinnerOrder)
            manager.SetSinnerSelected(sinner, sinner == "이상");
        Check(manager.RemainingCount == 16, "Only selected sinner enters draw pool");
        manager.SetSinnerSelected("이상", false);
        Check(manager.RemainingCount == 0, "No selected sinners leaves an empty draw pool");
        manager.Spin();
        Check(!manager.IsSpinning, "Empty draw pool cannot spin");
        foreach (string sinner in RouletteManager.SinnerOrder)
            manager.SetSinnerSelected(sinner, true);
        Check(manager.RemainingCount == total, "Selecting all sinners restores draw pool");
        manager.ResultRevealed += result => results++;
        manager.Spin();
        manager.Spin();
        Tick(reel, 900, 1f / 60);
        Check(manager.IsSpinning && results == 0 && view.Frames > 900, "Wait indefinitely for Stop");
        manager.StopSpin();
        manager.StopSpin();
        Tick(reel, 60, 1f / 60);
        Check(manager.IsStopping && results == 0, "Stop starts gradual braking");
        Tick(reel, 600, 1f / 60);
        Check(!manager.IsSpinning && !manager.IsStopping && results == 1 &&
            manager.RemainingCount == total - 1 && view.Shown == manager.LastResult, "One completed draw");
        manager.SetIdentityDuplicates(true);
        Check(manager.RemainingCount == total, "Identity duplicate ON restores drawn identity to pool");
        manager.SetSinnerDuplicates(false);
        Check(manager.RemainingCount < total - 1, "Sinner duplicate OFF excludes drawn sinner");
        manager.SetSinnerDuplicates(true);
        Check(manager.RemainingCount == total, "Sinner duplicate ON restores drawn sinner");
        manager.SetIdentityDuplicates(false);
        Check(manager.RemainingCount == total - 1, "Identity duplicate OFF excludes drawn identity");
        manager.QuitRoulette();
        Check(manager.RemainingCount == total && manager.LastResult == null && view.Shown == null &&
            UnityEngine.SceneManagement.SceneManager.Loaded == null,
            "First Exit after completed draw resets instead of leaving");
        manager.Spin();
        Tick(reel, 20, 1f / 60);
        manager.QuitRoulette();
        Tick(reel, 600, 1f / 60);
        Check(manager.RemainingCount == total && manager.LastResult == null && view.Shown == null &&
            !manager.IsSpinning && results == 1 && UnityEngine.SceneManagement.SceneManager.Loaded == null,
            "Exit while spinning restores all candidates without leaving");
        manager.Spin();
        manager.StopSpin();
        manager.QuitRoulette();
        Tick(reel, 600, 1f / 60);
        Check(results == 1 && !manager.IsStopping && manager.RemainingCount == total,
            "Exit during braking cancels pending result");
        manager.QuitRoulette();
        Check(UnityEngine.SceneManagement.SceneManager.Loaded == "Title", "Second Exit returns to title");
        return "PASS: " + paths + " brake paths; sinner selection, duplicate settings, indefinite spinning, gradual stop, reset, title return.";
    }
}
