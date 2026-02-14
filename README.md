# PivotSqlMonitor

Implementación inicial (Fase 1 + Fase 2) de un monitor centralizado para SQL Server en C#/.NET.

## Incluye

- Fase 1
  - Motor de corrida (`MonitorRun`) y almacenamiento (`MonitorResult`).
  - Checks: `PING`, `TCP_PORT`, `SQL_LOGIN`, `DISK_FREE`.
  - Repositorio SQL central (`adminDB`) vía Dapper.
- Fase 2
  - Configuración por servidor/check (`ServerCheckConfig`).
  - Umbrales Warning/Critical para checks métricos.
  - Tabla de alertas (`AlertEvent`) lista para integración con email/Teams.

## Estructura

- `PivotSqlMonitor.App`: ejecutable principal.
- `PivotSqlMonitor.Core`: modelos, contratos e integración del motor.
- `PivotSqlMonitor.Infrastructure`: checks concretos y acceso a datos.
- `sql/001_schema.sql`: DDL completo.
- `sql/002_seed.sql`: datos iniciales de ejemplo.
- `sql/003_sql_agent_job.sql`: creación de SQL Agent Job con **CmdExec** (sin PowerShell).

## Flujo de ejecución

1. Inserta corrida en `MonitorRun`.
2. Lee servidores habilitados.
3. Lee checks habilitados por servidor.
4. Ejecuta checks registrados.
5. Guarda resultados por check.
6. Cierra corrida con totales de estado.

## Publicación y ejecución

> Requiere SDK de .NET 8 para compilar.

Ejemplo:

```bash
dotnet restore
dotnet publish PivotSqlMonitor.App/PivotSqlMonitor.App.csproj -c Release -r win-x64 --self-contained true
```

Copia el `.exe` publicado al servidor pivote y programa `sql/003_sql_agent_job.sql`.
