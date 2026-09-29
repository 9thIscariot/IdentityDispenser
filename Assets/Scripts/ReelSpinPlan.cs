using System;

/// <summary>프레임 속도와 무관하게 미리 정한 인덱스에 정확히 멈추는 슬롯 이동 경로.</summary>
public sealed class ReelSpinPlan
{
    public double InitialSpeed { get; }
    public double CruiseDuration { get; }
    public double BrakeDuration { get; }
    public double Duration => CruiseDuration + BrakeDuration;
    public double EndPosition { get; }

    public ReelSpinPlan(int count, int targetIndex, double cycleSeconds,
        double minimumCruiseSeconds, double brakeSeconds)
    {
        if (count < 1) throw new ArgumentOutOfRangeException(nameof(count));
        if (targetIndex < 0 || targetIndex >= count)
            throw new ArgumentOutOfRangeException(nameof(targetIndex));
        if (!IsPositive(cycleSeconds) || !IsPositive(minimumCruiseSeconds) || !IsPositive(brakeSeconds))
            throw new ArgumentOutOfRangeException(nameof(cycleSeconds));

        InitialSpeed = count / cycleSeconds;
        BrakeDuration = brakeSeconds;
        // 감속 중 속도는 v0 * (1 - t / D)^3. 적분한 이동 거리는 v0 * D / 4.
        double minimumDistance = InitialSpeed * (minimumCruiseSeconds + brakeSeconds / 4);
        double cycles = Math.Ceiling((minimumDistance - targetIndex) / count);
        EndPosition = cycles * count + targetIndex;
        CruiseDuration = EndPosition / InitialSpeed - brakeSeconds / 4;
    }

    public double PositionAt(double seconds)
    {
        if (seconds <= 0) return 0;
        if (seconds >= Duration) return EndPosition;
        if (seconds <= CruiseDuration) return InitialSpeed * seconds;
        double remaining = 1 - (seconds - CruiseDuration) / BrakeDuration;
        return EndPosition - InitialSpeed * BrakeDuration / 4 * Math.Pow(remaining, 4);
    }

    private static bool IsPositive(double value)
    {
        return value > 0 && !double.IsInfinity(value) && !double.IsNaN(value);
    }
}
