INSERT INTO dbo.MonitorCheckType(Code, Description, Category, DefaultWarningThreshold, DefaultCriticalThreshold)
VALUES
('PING', 'Reachability by ICMP', 'NETWORK', NULL, NULL),
('TCP_PORT', 'TCP connectivity to SQL port', 'NETWORK', NULL, NULL),
('SQL_LOGIN', 'Validate SQL login/connectivity', 'SQL', NULL, NULL),
('DISK_FREE', 'Minimum free disk % from dm_os_volume_stats', 'SQL', 20, 10),
('MONGO_PING', 'MongoDB ping command', 'MONGO', NULL, NULL),
('MONGO_TCP_PORT', 'MongoDB TCP connectivity', 'MONGO', NULL, NULL),
('MONGO_LOGIN', 'MongoDB authenticated connection check', 'MONGO', NULL, NULL);

-- Example SQL target server
INSERT INTO dbo.MonitoredServer(ServerName, HostOrIp, Environment, IsEnabled, Port)
VALUES ('SQLPROD01', 'SQLPROD01', 'PROD', 1, 1433);

-- Enable SQL checks for SQLPROD01
INSERT INTO dbo.ServerCheckConfig(ServerId, CheckTypeId, WarningThreshold, CriticalThreshold)
SELECT s.ServerId, c.CheckTypeId,
       COALESCE(c.DefaultWarningThreshold, NULL),
       COALESCE(c.DefaultCriticalThreshold, NULL)
FROM dbo.MonitoredServer s
JOIN dbo.MonitorCheckType c ON c.Code IN ('PING', 'TCP_PORT', 'SQL_LOGIN', 'DISK_FREE')
WHERE s.ServerName = 'SQLPROD01';

-- Example Mongo target server
INSERT INTO dbo.MonitoredServer(ServerName, HostOrIp, Environment, IsEnabled, Port)
VALUES ('MONGO01', 'MONGO01', 'PROD', 1, 27017);

-- Enable Mongo checks for MONGO01
INSERT INTO dbo.ServerCheckConfig(ServerId, CheckTypeId, ExtraConfigJson)
SELECT s.ServerId, c.CheckTypeId,
       CASE WHEN c.Code = 'MONGO_LOGIN'
            THEN N'{"authDatabase":"admin","username":"mongo_monitor","password":"CambiarPassword"}'
            ELSE NULL END
FROM dbo.MonitoredServer s
JOIN dbo.MonitorCheckType c ON c.Code IN ('MONGO_PING', 'MONGO_TCP_PORT', 'MONGO_LOGIN')
WHERE s.ServerName = 'MONGO01';
