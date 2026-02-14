-- Run in msdb database on SQL Central server.
USE msdb;
GO

EXEC dbo.sp_add_job
    @job_name = N'PivotSqlMonitor Job',
    @enabled = 1,
    @description = N'Runs PivotSqlMonitor executable every 5 minutes';
GO

EXEC dbo.sp_add_jobstep
    @job_name = N'PivotSqlMonitor Job',
    @step_name = N'Run EXE',
    @subsystem = N'CMDEXEC',
    @command = N'"C:\Apps\PivotSqlMonitor\PivotSqlMonitor.App.exe"',
    @retry_attempts = 2,
    @retry_interval = 1;
GO

EXEC dbo.sp_add_schedule
    @schedule_name = N'Every 5 Minutes',
    @freq_type = 4,
    @freq_interval = 1,
    @freq_subday_type = 4,
    @freq_subday_interval = 5,
    @active_start_time = 0;
GO

EXEC dbo.sp_attach_schedule
    @job_name = N'PivotSqlMonitor Job',
    @schedule_name = N'Every 5 Minutes';
GO

EXEC dbo.sp_add_jobserver
    @job_name = N'PivotSqlMonitor Job';
GO
