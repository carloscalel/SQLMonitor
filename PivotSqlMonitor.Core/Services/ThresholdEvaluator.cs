using PivotSqlMonitor.Core.Enums;

namespace PivotSqlMonitor.Core.Services;

public static class ThresholdEvaluator
{
    public static MonitorStatus EvaluateLowIsBad(decimal value, decimal? warningThreshold, decimal? criticalThreshold)
    {
        if (criticalThreshold.HasValue && value <= criticalThreshold.Value)
        {
            return MonitorStatus.Critical;
        }

        if (warningThreshold.HasValue && value <= warningThreshold.Value)
        {
            return MonitorStatus.Warning;
        }

        return MonitorStatus.Ok;
    }

    public static MonitorStatus EvaluateHighIsBad(decimal value, decimal? warningThreshold, decimal? criticalThreshold)
    {
        if (criticalThreshold.HasValue && value >= criticalThreshold.Value)
        {
            return MonitorStatus.Critical;
        }

        if (warningThreshold.HasValue && value >= warningThreshold.Value)
        {
            return MonitorStatus.Warning;
        }

        return MonitorStatus.Ok;
    }
}
