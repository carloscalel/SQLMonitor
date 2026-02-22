# PivotSqlMonitor

Implementación inicial (Fase 1 + Fase 2) de un monitor centralizado para SQL Server en C#/.NET.

## Incluye

- Fase 1
  - Motor de corrida (`MonitorRun`) y almacenamiento (`MonitorResult`).
  - Checks SQL: `PING`, `TCP_PORT`, `SQL_LOGIN`, `DISK_FREE` (detalle de todos los volúmenes detectados).
  - Checks MongoDB: `MONGO_PING`, `MONGO_TCP_PORT`, `MONGO_LOGIN`.
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
- `sql/004_validation_queries.sql`: consultas de validación operativa.

## Flujo de ejecución

1. Inserta corrida en `MonitorRun`.
2. Lee servidores habilitados.
3. Lee checks habilitados por servidor.
4. Ejecuta checks registrados.
5. Guarda resultados por check.
6. Cierra corrida con totales de estado.

## Publicación y ejecución

> Requiere SDK de .NET 8 para compilar.

```bash
dotnet restore
dotnet publish PivotSqlMonitor.App/PivotSqlMonitor.App.csproj -c Release -r win-x64 --self-contained true
```

Copia el `.exe` publicado al servidor pivote y programa `sql/003_sql_agent_job.sql`.

## Autenticación SQL con usuario en forma protegida

Si no quieres usar autenticación integrada de Windows, puedes usar `AuthMode = SqlLogin` y guardar usuario/password cifrados con DPAPI (`LocalMachine`).

### 1) Generar valores cifrados

```bash
PivotSqlMonitor.App.exe --encrypt-credential "monitor_user" "MI-ENTROPIA-SEGURA"
PivotSqlMonitor.App.exe --encrypt-credential "P@ssw0rd!" "MI-ENTROPIA-SEGURA"
```

### 2) Configurar `appsettings.json`

```json
"CredentialEncryption": {
  "Entropy": "MI-ENTROPIA-SEGURA"
},
"TargetSql": {
  "AuthMode": "SqlLogin",
  "EncryptConnection": true,
  "TrustServerCertificate": true,
  "UserNameEncrypted": "<base64_cifrado_usuario>",
  "PasswordEncrypted": "<base64_cifrado_password>"
}
```

> Nota: DPAPI `LocalMachine` permite descifrar únicamente en el mismo servidor donde se cifró. Ideal para servidor pivote/SQL Agent.

## Validaciones finales en SQL Server

Ejecuta `sql/004_validation_queries.sql` para validar:
- resumen de corridas recientes,
- detalle de la última corrida,
- incidencias (warning/critical/error),
- tendencia por estado última hora,
- checks más lentos.


### Diagnóstico rápido de errores comunes

- **`No se ha inicializado la propiedad ConnectionString`**
  - Significa que `ConnectionStrings:AdminDb` llegó vacío.
  - Verifica `appsettings.json` o define variable de entorno:
  - `PIVOT_ConnectionStrings__AdminDb="Server=...;Database=adminDB;..."`

- **Error al descifrar credenciales DPAPI**
  - Debes cifrar y ejecutar en la **misma máquina** (DPAPI `LocalMachine`) y con la **misma entropía**.
  - Si cambiaste servidor o entropía, vuelve a generar `UserNameEncrypted` y `PasswordEncrypted`.


- `DISK_FREE` ahora guarda el mínimo porcentaje en `MetricValue` y el detalle de todos los discos en `MetricText`/`RawPayloadJson`.


## Checks MongoDB (implementación mínima)

Se implementaron 3 checks:
- `MONGO_PING`: comando `ping` contra MongoDB.
- `MONGO_TCP_PORT`: prueba de conectividad TCP al puerto (default 27017).
- `MONGO_LOGIN`: valida conexión autenticada (`connectionStatus`).

Para `MONGO_LOGIN`, usa `ServerCheckConfig.ExtraConfigJson` con esta forma:

```json
{
  "authDatabase": "admin",
  "username": "mongo_monitor",
  "password": "CambiarPassword"
}
```

Opcionalmente también puedes enviar `connectionString` completa en `ExtraConfigJson`.
