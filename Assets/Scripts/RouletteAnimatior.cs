using System;
using System.Collections.Generic;
using UnityEngine;

// 기존 파일 이름을 유지합니다.
public sealed class RouletteAnimatior : MonoBehaviour
{
    private ReelSpinPlan plan;
    private IReadOnlyList<IdentityData> entries;
    private Action<IdentityData, IdentityData, float> render;
    private Action completed;
    private Action cancelled;
    private double elapsed;
    private double position, speed;
    private int target;
    private float cycle, braking;

    public void Play(IReadOnlyList<IdentityData> candidates, int targetIndex,
        float cycleSeconds, float brakeSeconds,
        Action<IdentityData, IdentityData, float> onFrame, Action onComplete, Action onCancel)
    {
        Cancel();
        entries = candidates;
        target = targetIndex;
        cycle = cycleSeconds;
        braking = brakeSeconds;
        speed = candidates.Count / (double)cycleSeconds;
        position = 0;
        render = onFrame;
        completed = onComplete;
        cancelled = onCancel;
        elapsed = 0;
        RenderPosition(0);
    }

    private void Update()
    {
        if (entries == null) return;
        if (plan == null)
        {
            position = (position + speed * Time.unscaledDeltaTime) % entries.Count;
            RenderPosition(position);
            return;
        }
        elapsed += Time.unscaledDeltaTime;
        RenderPosition(plan.PositionAt(elapsed));
        if (elapsed < plan.Duration) return;
        Action callback = completed;
        Clear();
        callback?.Invoke();
    }

    public void RequestStop()
    {
        if (entries == null || plan != null) return;
        plan = ReelSpinPlan.BrakeFrom(entries.Count, target, cycle, braking, position);
        elapsed = 0;
    }

    private void RenderPosition(double position)
    {
        double whole = Math.Floor(position);
        int index = (int)(whole % entries.Count);
        render?.Invoke(entries[index], entries[(index + 1) % entries.Count], (float)(position - whole));
    }

    public void Cancel()
    {
        Action callback = cancelled;
        Clear();
        callback?.Invoke();
    }

    private void Clear()
    {
        plan = null;
        entries = null;
        render = null;
        completed = null;
        cancelled = null;
    }

    private void OnDisable() => Cancel();
}
