using System;

public sealed class ReelSpinPlan
{
    public double InitialSpeed { get; }
    public double CruiseDuration { get; }
    public double BrakeDuration { get; }
    public double Duration => CruiseDuration + BrakeDuration;
    public double EndPosition { get; }
    public double StartPosition { get; }

    private ReelSpinPlan(double start, double speed, double duration, double end)
    {
        StartPosition = start;
        InitialSpeed = speed;
        BrakeDuration = duration;
        EndPosition = end;
    }

    public static ReelSpinPlan BrakeFrom(int count, int targetIndex, double cycleSeconds,
        double minimumBrakeSeconds, double position)
    {
        if (count < 1 || targetIndex < 0 || targetIndex >= count)
            throw new ArgumentOutOfRangeException(nameof(targetIndex));
        if (!IsPositive(cycleSeconds) || !IsPositive(minimumBrakeSeconds) ||
            double.IsNaN(position) || double.IsInfinity(position) || position < 0)
            throw new ArgumentOutOfRangeException(nameof(position));
        double speed = count / cycleSeconds;
        double minimumEnd = position + speed * minimumBrakeSeconds / 4;
        double end = Math.Ceiling((minimumEnd - targetIndex) / count) * count + targetIndex;
        return new ReelSpinPlan(position, speed, 4 * (end - position) / speed, end);
    }

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
        double minimumDistance = InitialSpeed * (minimumCruiseSeconds + brakeSeconds / 4);
        double cycles = Math.Ceiling((minimumDistance - targetIndex) / count);
        EndPosition = cycles * count + targetIndex;
        CruiseDuration = EndPosition / InitialSpeed - brakeSeconds / 4;
    }

    public double PositionAt(double seconds)
    {
        if (seconds <= 0) return StartPosition;
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
