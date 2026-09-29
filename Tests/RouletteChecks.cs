using System;
using System.Collections.Generic;

// Unity 없이 실행할 수 있는 데이터/회전 경로 검증입니다.
public static class RouletteChecks
{
    public static string Run()
    {
        int paths = 0;
        foreach (double frameSeconds in new[] { 1.0 / 30, 1.0 / 60, 1.0 / 144, 0.25 })
        {
            for (int count = 1; count <= 189; count++)
            {
                for (int target = 0; target < count; target++)
                {
                    var plan = new ReelSpinPlan(count, target, 1, 1, 4);
                    Check(Math.Abs(plan.PositionAt(1) - count) < 1e-8, "One cycle per second");
                    Check(plan.Duration >= 5 - 1e-8 && plan.Duration < 6 + 1e-8, "Duration bounds");
                    Check(plan.EndPosition % count == target, "Predetermined target");
                    double previous = 0;
                    double previousStep = double.PositiveInfinity;
                    for (double time = frameSeconds; time <= plan.Duration + frameSeconds; time += frameSeconds)
                    {
                        double position = plan.PositionAt(time);
                        double step = position - previous;
                        Check(step >= -1e-8, "No reversal");
                        Check(position <= plan.EndPosition, "No overshoot");
                        Check(step <= previousStep + 1e-8, "Speed never increases");
                        previous = position;
                        previousStep = step;
                    }
                    Check(previous == plan.EndPosition, "Exact landing even after a long frame");
                    Check(plan.PositionAt(plan.Duration + 100) == plan.EndPosition, "Stable stop");
                    paths++;
                }
            }
        }

        var database = IdentityDatabase.Default;
        var pool = database.CreateDrawPool();
        var seen = new HashSet<string>();
        var random = new Random(17);
        while (pool.Count > 0)
        {
            int index = random.Next(pool.Count);
            Check(seen.Add(pool[index].Id), "No repeated identity");
            pool.RemoveAt(index);
        }
        Check(seen.Count == 189 && database.Identities.Count == 189, "Original data preserved");
        Check(database.CreateDrawPool().Count == 189, "Reset restores all identities");
        var yiSangPool = database.CreateDrawPool("이상");
        Check(yiSangPool.Count == 16, "Yi Sang test has 16 candidates");
        foreach (IdentityData identity in yiSangPool)
            Check(identity.SinnerName == "이상", "Test pool contains only Yi Sang");
        yiSangPool.Clear();
        Check(database.CreateDrawPool("이상").Count == 16, "Test reset restores 16 candidates");
        Check(database.Identities.Count == 189, "Filtering preserves the complete database");
        return $"PASS: {paths} paths (1–189 candidates, every target, 30/60/144 FPS and 250 ms frames); unique draws and reset.";
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
