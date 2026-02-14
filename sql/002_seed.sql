INSERT INTO dbo.MonitorCheckType(Code, Description, Category, DefaultWarningThreshold, DefaultCriticalThreshold)
VALUES
('PING', 'Reachability by ICMP', 'NETWORK', NULL, NULL),
('TCP_PORT', 'TCP connectivity to SQL port', 'NETWORK', NULL, NULL),
('SQL_LOGIN', 'Validate SQL login/connectivity', 'SQL', NULL, NULL),
('DISK_FREE', 'Minimum free disk % from dm_os_volume_stats', 'SQL', 20, 10);

-- Example target server
INSERT INTO dbo.MonitoredServer(ServerName, HostOrIp, Environment, IsEnabled)
VALUES ('SQLPROD01', 'SQLPROD01', 'PROD', 1);

-- Enable checks for that server
INSERT INTO dbo.ServerCheckConfig(ServerId, CheckTypeId, WarningThreshold, CriticalThreshold)
SELECT s.ServerId, c.CheckTypeId,
       COALESCE(c.DefaultWarningThreshold, NULL),
       COALESCE(c.DefaultCriticalThreshold, NULL)
FROM dbo.MonitoredServer s
CROSS JOIN dbo.MonitorCheckType c
WHERE s.ServerName = 'SQLPROD01';
