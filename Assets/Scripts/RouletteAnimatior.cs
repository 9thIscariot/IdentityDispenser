using UnityEngine;
using System;
using System.Collections.Generic;

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
    private long slotIndex, targetSlot;
    private IdentityData currentEntry, nextEntry;
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
        targetSlot = -1;
        slotIndex = 0;
        currentEntry = PickNext(null, 0);
        nextEntry = PickNext(currentEntry, 1);
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
            position += speed * Time.unscaledDeltaTime;
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
        targetSlot = (long)plan.EndPosition;
        if (targetSlot == slotIndex + 1) nextEntry = entries[target];
        elapsed = 0;
    }

    private void RenderPosition(double position)
    {
        double whole = Math.Floor(position);
        while (slotIndex < (long)whole)
        {
            slotIndex++;
            currentEntry = nextEntry;
            nextEntry = PickNext(currentEntry, slotIndex + 1);
        }
        render?.Invoke(currentEntry, nextEntry, (float)(position - whole));
    }

    private IdentityData PickNext(IdentityData previous, long slot)
    {
        if (slot == targetSlot) return entries[target];

        bool hasDifferentSinner = false;
        bool hasDifferentIdentity = false;
        foreach (IdentityData entry in entries)
        {
            if (previous == null || entry.SinnerName != previous.SinnerName)
                hasDifferentSinner = true;
            if (previous == null || entry.Id != previous.Id)
                hasDifferentIdentity = true;
        }

        int choices = 0;
        foreach (IdentityData entry in entries)
            if (IsChoice(entry, previous, hasDifferentSinner, hasDifferentIdentity)) choices++;

        int choice = UnityEngine.Random.Range(0, choices);
        foreach (IdentityData entry in entries)
            if (IsChoice(entry, previous, hasDifferentSinner, hasDifferentIdentity) && choice-- == 0)
                return entry;
        return entries[0];
    }

    private static bool IsChoice(IdentityData entry, IdentityData previous,
        bool hasDifferentSinner, bool hasDifferentIdentity)
    {
        return previous == null ||
            (hasDifferentSinner ? entry.SinnerName != previous.SinnerName :
             hasDifferentIdentity ? entry.Id != previous.Id : true);
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
        currentEntry = nextEntry = null;
        targetSlot = -1;
    }

    private void OnDisable() => Cancel();
}
