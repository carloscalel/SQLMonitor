-- PivotSqlMonitor schema for adminDB
CREATE TABLE dbo.MonitoredServer
(
    ServerId            INT IDENTITY(1,1) PRIMARY KEY,
    ServerName          NVARCHAR(128) NOT NULL,
    HostOrIp            NVARCHAR(128) NOT NULL,
    SqlInstance         NVARCHAR(128) NOT NULL DEFAULT('MSSQLSERVER'),
    Port                INT NOT NULL DEFAULT(1433),
    Environment         NVARCHAR(32) NOT NULL,
    IsEnabled           BIT NOT NULL DEFAULT(1),
    ConnectTimeoutSec   INT NOT NULL DEFAULT(5),
    CommandTimeoutSec   INT NOT NULL DEFAULT(5),
    CreatedAtUtc        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAtUtc        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.MonitorCheckType
(
    CheckTypeId                 INT IDENTITY(1,1) PRIMARY KEY,
    Code                        NVARCHAR(64) NOT NULL UNIQUE,
    Description                 NVARCHAR(256) NOT NULL,
    Category                    NVARCHAR(32) NOT NULL,
    DefaultWarningThreshold     DECIMAL(18,2) NULL,
    DefaultCriticalThreshold    DECIMAL(18,2) NULL,
    IsEnabled                   BIT NOT NULL DEFAULT(1)
);

CREATE TABLE dbo.ServerCheckConfig
(
    ServerCheckConfigId INT IDENTITY(1,1) PRIMARY KEY,
    ServerId            INT NOT NULL FOREIGN KEY REFERENCES dbo.MonitoredServer(ServerId),
    CheckTypeId         INT NOT NULL FOREIGN KEY REFERENCES dbo.MonitorCheckType(CheckTypeId),
    IsEnabled           BIT NOT NULL DEFAULT(1),
    WarningThreshold    DECIMAL(18,2) NULL,
    CriticalThreshold   DECIMAL(18,2) NULL,
    ExtraConfigJson     NVARCHAR(MAX) NULL,
    CONSTRAINT UQ_ServerCheck UNIQUE(ServerId, CheckTypeId)
);

CREATE TABLE dbo.MonitorRun
(
    RunId            INT IDENTITY(1,1) PRIMARY KEY,
    StartedAtUtc     DATETIME2 NOT NULL,
    FinishedAtUtc    DATETIME2 NULL,
    Status           NVARCHAR(32) NOT NULL,
    TriggeredBy      NVARCHAR(64) NOT NULL,
    TotalChecks      INT NOT NULL DEFAULT(0),
    SuccessCount     INT NOT NULL DEFAULT(0),
    WarningCount     INT NOT NULL DEFAULT(0),
    CriticalCount    INT NOT NULL DEFAULT(0),
    ErrorCount       INT NOT NULL DEFAULT(0),
    ErrorMessage     NVARCHAR(4000) NULL
);

CREATE TABLE dbo.MonitorResult
(
    ResultId         BIGINT IDENTITY(1,1) PRIMARY KEY,
    RunId            INT NOT NULL FOREIGN KEY REFERENCES dbo.MonitorRun(RunId),
    ServerId         INT NOT NULL FOREIGN KEY REFERENCES dbo.MonitoredServer(ServerId),
    CheckTypeId      INT NOT NULL FOREIGN KEY REFERENCES dbo.MonitorCheckType(CheckTypeId),
    StartedAtUtc     DATETIME2 NOT NULL,
    FinishedAtUtc    DATETIME2 NOT NULL,
    DurationMs       BIGINT NOT NULL,
    Status           NVARCHAR(32) NOT NULL,
    MetricValue      DECIMAL(18,2) NULL,
    MetricText       NVARCHAR(512) NULL,
    RawPayloadJson   NVARCHAR(MAX) NULL
);

CREATE TABLE dbo.AlertEvent
(
    AlertId          BIGINT IDENTITY(1,1) PRIMARY KEY,
    ResultId         BIGINT NOT NULL FOREIGN KEY REFERENCES dbo.MonitorResult(ResultId),
    AlertLevel       NVARCHAR(32) NOT NULL,
    AlertChannel     NVARCHAR(32) NOT NULL,
    SentAtUtc        DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    AckBy            NVARCHAR(128) NULL,
    AckAtUtc         DATETIME2 NULL
);

CREATE INDEX IX_MonitorResult_RunServer ON dbo.MonitorResult(RunId, ServerId);
CREATE INDEX IX_MonitorResult_StatusTime ON dbo.MonitorResult(Status, FinishedAtUtc DESC);
