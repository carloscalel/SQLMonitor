/* Validaciones post-ejecución para adminDB */

/* 1) Últimas 20 corridas con resumen */
SELECT TOP (20)
    RunId,
    StartedAtUtc,
    FinishedAtUtc,
    Status,
    TotalChecks,
    SuccessCount,
    WarningCount,
    CriticalCount,
    ErrorCount
FROM dbo.MonitorRun
ORDER BY RunId DESC;

/* 2) Resultado detallado de la última corrida */
DECLARE @LastRunId INT = (SELECT MAX(RunId) FROM dbo.MonitorRun);

SELECT
    mr.RunId,
    s.ServerName,
    ct.Code AS CheckCode,
    r.Status,
    r.MetricValue,
    r.MetricText,
    r.DurationMs,
    r.FinishedAtUtc
FROM dbo.MonitorResult r
JOIN dbo.MonitoredServer s ON s.ServerId = r.ServerId
JOIN dbo.MonitorCheckType ct ON ct.CheckTypeId = r.CheckTypeId
JOIN dbo.MonitorRun mr ON mr.RunId = r.RunId
WHERE r.RunId = @LastRunId
ORDER BY s.ServerName, ct.Code;

/* 3) Solo incidencias activas (Warning/Critical/Error/Unreachable) de la última corrida */
SELECT
    s.ServerName,
    ct.Code AS CheckCode,
    r.Status,
    r.MetricValue,
    r.MetricText,
    r.FinishedAtUtc
FROM dbo.MonitorResult r
JOIN dbo.MonitoredServer s ON s.ServerId = r.ServerId
JOIN dbo.MonitorCheckType ct ON ct.CheckTypeId = r.CheckTypeId
WHERE r.RunId = (SELECT MAX(RunId) FROM dbo.MonitorRun)
  AND r.Status IN ('WARNING', 'CRITICAL', 'ERROR', 'UNREACHABLE')
ORDER BY
  CASE r.Status WHEN 'CRITICAL' THEN 1 WHEN 'ERROR' THEN 2 WHEN 'WARNING' THEN 3 ELSE 4 END,
  s.ServerName,
  ct.Code;

/* 4) Tendencia última hora por estado */
SELECT
    r.Status,
    COUNT(*) AS Total
FROM dbo.MonitorResult r
WHERE r.FinishedAtUtc >= DATEADD(HOUR, -1, SYSUTCDATETIME())
GROUP BY r.Status
ORDER BY Total DESC;

/* 5) Top checks más lentos de la última corrida */
SELECT TOP (15)
    s.ServerName,
    ct.Code AS CheckCode,
    r.DurationMs,
    r.Status,
    r.MetricText
FROM dbo.MonitorResult r
JOIN dbo.MonitoredServer s ON s.ServerId = r.ServerId
JOIN dbo.MonitorCheckType ct ON ct.CheckTypeId = r.CheckTypeId
WHERE r.RunId = (SELECT MAX(RunId) FROM dbo.MonitorRun)
ORDER BY r.DurationMs DESC;
