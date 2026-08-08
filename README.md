# ZOCO Tasks — Backend

Gestor interno de comercios para seguimiento de oportunidades comerciales.
API REST en **ASP.NET Core 10** sobre **PostgreSQL**, con detección de
conflictos de edición concurrente y análisis asistido por IA.

> El frontend vive en un repositorio aparte: [`zocotasks-frontend`](https://github.com/valentin21103/zocotasks-frontend).

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-18-336791)](https://www.postgresql.org/)
[![EF Core](https://img.shields.io/badge/EF%20Core-10.0-512BD4)](https://learn.microsoft.com/ef/core/)

---

## Índice

- [Estado del proyecto](#estado-del-proyecto)
- [Qué resuelve](#qué-resuelve)
- [Arquitectura](#arquitectura)
- [Modelo de datos](#modelo-de-datos)
- [Concurrencia optimista](#concurrencia-optimista)
- [Cómo levantarlo](#cómo-levantarlo)
- [Variables de entorno](#variables-de-entorno)
- [Migraciones](#migraciones)
- [Tests](#tests)
- [Bonus implementados](#bonus-implementados)
- [Documentación adicional](#documentación-adicional)

---

## Estado del proyecto

| Bloque | Estado |
|---|---|
| Modelo de dominio, persistencia y migración inicial | ✅ Verificado contra base real |
| Repositorios, servicios, validación y controllers | 🚧 En curso |
| Bonus: rate limiting, full text, auditoría, CI | ⬜ Pendiente |
| Feature "Analizar oportunidad" | ⬜ Pendiente |
| Autenticación y roles | ⬜ Pendiente |
| Tests unitarios y de integración | ⬜ Pendiente |

Lo marcado como verificado no significa "compila": significa que se comprobó
su comportamiento contra PostgreSQL real. Ver [Verificaciones](#verificaciones-realizadas).

---

## Qué resuelve

Un equipo comercial necesita registrar comercios interesados y seguir su avance
por un embudo de ventas:

```
Nuevo → Contactado → Interesado → Documentación → Aprobado
   └────────────────────────────────────────────→ Rechazado
```

Sobre cada comercio se registran **interacciones** (llamada, WhatsApp, reunión,
email, nota interna), y a partir de esos datos el sistema genera un análisis de
oportunidad: resumen, nivel de interés estimado, próximo paso recomendado, tres
preguntas para el vendedor y datos faltantes.

---

## Arquitectura

Arquitectura en capas con las dependencias apuntando hacia adentro.

```
┌─────────────────┐
│  ZocoTasks.API  │  Controllers, middleware, autenticación
└────────┬────────┘
         │
    ┌────┴─────────────────────┐
    ▼                          ▼
┌──────────────────┐   ┌────────────────────────┐
│ ZocoTasks        │   │ ZocoTasks              │
│ .Business        │◄──┤ .Infrastructure        │
│                  │   │                        │
│ DTOs, servicios, │   │ EF Core, repositorios, │
│ validadores,     │   │ proveedor de IA        │
│ interfaces       │   │                        │
└────────┬─────────┘   └───────────┬────────────┘
         │                         │
         ▼                         ▼
      ┌───────────────────────────────┐
      │      ZocoTasks.Domain         │
      │  Entidades, enums, reglas     │
      │   CERO dependencias externas  │
      └───────────────────────────────┘
```

**Reglas que se cumplen por construcción, no por disciplina:**

- `Domain` no referencia ni un solo paquete NuGet. El compilador lo garantiza:
  es imposible que una entidad termine dependiendo de EF Core por descuido.
- Ningún controller toca `DbContext`. Solo habla con servicios de `Business`.
- `API` referencia a `Infrastructure` únicamente para registrar la inyección de
  dependencias en el arranque.

La consecuencia práctica más visible: la columna `search_vector` es de tipo
`tsvector`, cuyo tipo CLR pertenece a Npgsql. Como `Domain` no puede
referenciarlo, se modeló como **shadow property** en la configuración de EF.
La entidad `Comercio` nunca la conoce.

---

## Modelo de datos

```mermaid
erDiagram
    COMERCIO ||--o{ INTERACCION : "registra"
    COMERCIO ||--o{ HISTORIAL_ESTADO : "deja traza en"
    COMERCIO ||--o{ ANALISIS_OPORTUNIDAD : "genera"
    RUBRO ||--o{ COMERCIO : "clasifica"
    ESTADO_COMERCIO ||--o{ COMERCIO : "estado actual de"
    ESTADO_COMERCIO ||--o{ HISTORIAL_ESTADO : "origen y destino de"
    TIPO_INTERACCION ||--o{ INTERACCION : "tipifica"
    USUARIO ||--o{ COMERCIO : "tiene asignado"
    USUARIO ||--o{ INTERACCION : "registra"
    USUARIO ||--o{ USUARIO_ROL : "tiene"
    ROL ||--o{ USUARIO_ROL : "asignado en"

    COMERCIO {
        int id PK
        varchar nombre_comercial
        char cuit UK "11 digitos, modulo 11"
        varchar nombre_contacto
        varchar telefono
        citext email "case-insensitive"
        int rubro_id FK
        smallint estado_id FK
        int usuario_asignado_id FK "nullable"
        text notas
        timestamptz fecha_creacion
        timestamptz fecha_eliminacion "soft delete"
        tsvector search_vector "generada, indice GIN"
        xid xmin "columna de sistema, concurrencia"
    }

    INTERACCION {
        int id PK
        int comercio_id FK
        smallint tipo_id FK
        int usuario_id FK
        timestamptz fecha
        text detalle
    }

    HISTORIAL_ESTADO {
        int id PK
        int comercio_id FK
        smallint estado_anterior_id FK "nulo solo en el alta"
        smallint estado_nuevo_id FK
        int usuario_id FK
        timestamptz fecha
        varchar motivo
    }

    ANALISIS_OPORTUNIDAD {
        int id PK
        int comercio_id FK
        smallint nivel_interes
        text resumen
        text proximo_paso
        jsonb preguntas_sugeridas
        jsonb datos_faltantes
        char hash_contexto "SHA256, clave de cache"
        bool es_degradado
    }

    ESTADO_COMERCIO {
        smallint id PK
        varchar codigo UK
        smallint orden "posicion en el embudo"
        bool es_final
    }

    RUBRO {
        int id PK
        varchar nombre UK
        bool activo
    }

    TIPO_INTERACCION {
        smallint id PK
        varchar codigo UK
    }

    USUARIO {
        int id PK
        citext email UK
        varchar password_hash "BCrypt"
        bool activo
    }

    ROL {
        int id PK
        varchar nombre UK
    }

    USUARIO_ROL {
        int usuario_id PK_FK
        int rol_id PK_FK
    }
```

### Las cuatro decisiones que definen este modelo

**1. Estado como enum en C# *y* tabla lookup en la base.**
El enum da tipo fuerte y permite que la máquina de estados viva en el dominio;
la tabla da integridad referencial y dos columnas que el enum no puede tener:
`orden` (posición en el embudo) y `es_final`. No hay duplicación real: el seed
de la tabla se genera recorriendo el enum, así que agregar un estado es tocar
un solo lugar.

**2. `historial_estado` no es solo auditoría — es el insumo del feature de IA.**
Un comercio veinte días trabado en "Documentación" es información crítica para
recomendar el próximo paso, y es un dato que *no existe en ningún otro lado*.
Sin esta tabla, el análisis solo ve texto plano; con ella, ve una trayectoria.
Además, la única forma de cambiar de estado es `Comercio.CambiarEstado()`, que
valida la transición y escribe el historial en la misma operación: no hay camino
de código que pueda mover un comercio sin dejar rastro.

**3. El análisis se persiste con un hash del contexto.**
`hash_contexto` es el SHA256 de lo que se le envió al modelo. Si nadie tocó el
comercio desde el último análisis, el hash coincide y se devuelve el guardado
sin volver a pagar tokens. Sirve además para ver cómo evolucionó el interés en
el tiempo.

**4. Soft delete.**
Borrar un comercio en duro se llevaría por cascada sus interacciones y su
historial — justamente la evidencia del trabajo comercial. `fecha_eliminacion`
nullable con filtro global de EF: los eliminados desaparecen de toda consulta
salvo que se pidan explícitamente.

> El razonamiento completo de cada decisión, con las alternativas descartadas y
> las condiciones bajo las cuales elegiría distinto, está en
> **[docs/DECISIONES.md](docs/DECISIONES.md)**.

---

## Concurrencia optimista

> Requisito destacado de la consigna: *dos usuarios no deben poder modificar el
> mismo registro accidentalmente sin detectar el conflicto.*

Se resuelve con **`xmin`**, una columna de sistema de PostgreSQL que guarda el
ID de la transacción que escribió la fila por última vez y **cambia sola en cada
UPDATE**.

```csharp
builder.Property(c => c.Version)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .ValueGeneratedOnAddOrUpdate()
    .IsConcurrencyToken();
```

**Por qué `xmin` y no una columna `version` propia:** una columna propia hay que
acordarse de incrementarla en cada UPDATE, y un solo camino de código que lo
olvide rompe la garantía en silencio. `xmin` no la incrementa nadie — la
mantiene el motor. Elimina por construcción la clase entera de bugs.

### Flujo HTTP

```
┌──────────┐                                    ┌──────────┐
│ Usuario A│                                    │ Usuario B│
└────┬─────┘                                    └────┬─────┘
     │  GET /api/comercios/1                         │
     │  ◄── 200 OK  ETag: "2056"                     │
     │                                               │
     │                    GET /api/comercios/1       │
     │                    ◄── 200 OK  ETag: "2056"   │
     │                                               │
     │  PUT /api/comercios/1                         │
     │      If-Match: "2056"                         │
     │  ◄── 200 OK   (xmin pasa a 2057)              │
     │                                               │
     │                    PUT /api/comercios/1       │
     │                        If-Match: "2056"       │
     │                    ◄── 409 Conflict           │
     │                        + estado actual        │
```

`ETag` e `If-Match` son HTTP estándar (RFC 9110) para exactamente este problema:
cualquier cliente o proxy los entiende sin documentación, a diferencia de un
campo `version` en el body, que sería una convención privada.

El 409 devuelve **el estado actual del registro**, no un error pelado: así el
front puede mostrar qué cambió y ofrecer resolver el conflicto en vez de obligar
a recargar y perder el trabajo.

---

## Cómo levantarlo

### Requisitos

- [.NET SDK 10.0.300+](https://dotnet.microsoft.com/download)
- Una base PostgreSQL 14+ (el proyecto usa [Neon](https://neon.tech), pero
  sirve cualquier PostgreSQL)

### Pasos

```bash
git clone https://github.com/valentin21103/zocotasks-backend.git
cd zocotasks-backend

# 1. Herramientas locales (instala dotnet-ef en la version exacta del proyecto)
dotnet tool restore

# 2. Cadena de conexion (NO se versiona)
dotnet user-secrets set "ConnectionStrings:ZocoDb" \
  "Host=<host>;Port=5432;Database=<db>;Username=<user>;Password=<pass>;SSL Mode=VerifyFull;Maximum Pool Size=20" \
  --project ZocoTasks.API

# 3. Crear el esquema y sembrar los catalogos
$env:ConnectionStrings__ZocoDb = "<la misma cadena>"     # PowerShell
dotnet ef database update --project ZocoTasks.Infrastructure --startup-project ZocoTasks.API

# 4. Levantar
dotnet run --project ZocoTasks.API
```

La documentación OpenAPI queda en `/openapi/v1.json`.

> **Por qué el paso 3 repite la cadena como variable de entorno:** los comandos
> de EF usan un `IDesignTimeDbContextFactory`, que EF prioriza sobre la
> configuración de la aplicación y por lo tanto no lee `user-secrets`.

### Si usás Neon

Neon entrega la cadena en formato **libpq** (`postgresql://...`), que no es el
formato de .NET. La traducción:

| Neon (libpq) | .NET (Npgsql) |
|---|---|
| `postgresql://user:pass@host/db` | `Host=…;Database=…;Username=…;Password=…` |
| `sslmode=require` | `SSL Mode=VerifyFull` |
| `channel_binding=require` | *(se omite — Npgsql lo negocia solo)* |

Y **quitá el sufijo `-pooler` del host**: la conexión pooled pasa por PgBouncer
en *transaction mode*, que no sostiene los advisory locks que usan las
migraciones de EF Core. Detalle completo en
[docs/DECISIONES.md §4.2](docs/DECISIONES.md#42-conexión-directa-no-la-pooled).

---

## Variables de entorno

Todas están documentadas en [`.env.example`](.env.example), versionado sin
valores. El doble guion bajo es el separador de secciones de la configuración de
.NET: `ConnectionStrings__ZocoDb` equivale a `ConnectionStrings:ZocoDb`.

| Variable | Obligatoria | Para qué |
|---|:---:|---|
| `ConnectionStrings__ZocoDb` | Sí | Base de datos principal |
| `ConnectionStrings__ZocoDbTests` | Solo tests | Base local para tests de integración |
| `Jwt__Clave` | Sí (con auth) | Firma de los tokens, mínimo 32 caracteres |
| `Jwt__Emisor`, `Jwt__Audiencia` | Sí (con auth) | Validación del token |
| `Ia__ApiKey` | No | Proveedor de IA. Sin ella el análisis responde degradado en lugar de fallar |

**No hay ningún secreto en el repositorio.** `appsettings.json` tiene la clave de
conexión vacía a propósito; el valor real llega por `user-secrets` en desarrollo
o por variable de entorno en CI y deploy. La aplicación **falla en el arranque**
con un mensaje explícito si falta, en lugar de levantar bien y explotar en el
primer request.

---

## Migraciones

El esquema se crea y evoluciona exclusivamente con migraciones de EF Core. Los
catálogos (estados, tipos de interacción, rubros, roles) viajan **dentro** de la
migración vía `HasData`, así que una base recién creada queda consistente con un
solo comando y sin pasos manuales.

```bash
# Nueva migración
dotnet ef migrations add <Nombre> --project ZocoTasks.Infrastructure --startup-project ZocoTasks.API

# Aplicarla
dotnet ef database update --project ZocoTasks.Infrastructure --startup-project ZocoTasks.API

# Script SQL idempotente (para deploys donde no se corre la CLI)
dotnet ef migrations script --idempotent --project ZocoTasks.Infrastructure --startup-project ZocoTasks.API
```

### Verificaciones realizadas

El esquema no se dio por bueno porque compile. Se comprobó contra PostgreSQL
real (Neon, `sa-east-1`):

| Qué se verificó | Resultado |
|---|---|
| Tablas creadas | 11 + historial de migraciones |
| Catálogos sembrados | 6 estados, 5 tipos, 9 rubros, 2 roles |
| `xmin` cambia en cada UPDATE | `2056` → `2057` ✅ |
| `xmin` **no** se intenta crear en el DDL | Confirmado (es columna de sistema; crearla fallaría) |
| `search_vector` es `GENERATED ALWAYS ... STORED` | ✅ |
| Índice GIN sobre `search_vector` | ✅ (`amname = gin`) |
| Stemming español: `problema` encuentra "Problemas" | ✅ |
| Stemming español: `sucursal` encuentra "sucursales" | ✅ |
| Stopwords: `de` no matchea | ✅ |
| `email` es `citext` | ✅ (`USER-DEFINED`) |
| Índice único de CUIT rechaza duplicados | ✅ |

---

## Tests

```bash
dotnet test
```

> Pendiente — Bloque 7. Cubrirá validación de CUIT por módulo 11, transiciones
> de estado válidas e inválidas, y el test de integración que demuestra el
> **409 por conflicto de concurrencia**.

---

## Bonus implementados

| Bonus | Estado | Nota |
|---|:---:|---|
| Búsqueda full text | ✅ | Columna generada + índice GIN, diccionario español con stemming verificado |
| Migraciones | ✅ | Con seed de catálogos incluido |
| Docker | ⬜ | Descartado para desarrollo: la base es gestionada (Neon). Se evaluará un `Dockerfile` para el deploy |
| Paginación | 🚧 | Bloque 2 |
| Auditoría | ⬜ | Tabla creada, interceptor pendiente |
| Autenticación y roles | ⬜ | Tablas creadas, implementación pendiente |
| Rate limiting | ⬜ | `AddRateLimiter`, nativo en .NET 8+ |
| Tests de integración | ⬜ | Contra PostgreSQL local, no Testcontainers |
| CI/CD | ⬜ | GitHub Actions: build + test |
| Deploy público | ⬜ | |

### Nota de seguridad: `Microsoft.OpenApi` fijado en 2.7.5

`Microsoft.AspNetCore.OpenApi` 10.0.10 arrastra transitivamente
`Microsoft.OpenApi` **2.0.0**, que tiene una vulnerabilidad de severidad alta
([GHSA-v5pm-xwqc-g5wc](https://github.com/advisories/GHSA-v5pm-xwqc-g5wc)).

Subir el paquete padre a la última versión **no** la resuelve (verificado con
`dotnet list package --include-transitive`), y la línea 3.x introduce cambios de
API incompatibles con este release de ASP.NET Core. La solución es una
referencia directa a la última 2.x, que gana sobre la resolución transitiva.

Es un **pin de seguridad, no una dependencia del código**, y está comentado como
tal en el `.csproj` con instrucción de removerlo cuando ASP.NET Core actualice.

---

## Documentación adicional

| Documento | Para qué |
|---|---|
| [docs/DECISIONES.md](docs/DECISIONES.md) | Cada decisión de arquitectura e infraestructura con sus alternativas descartadas y las condiciones bajo las que elegiría distinto |
| [docs/GUIA-DE-ESTUDIO.md](docs/GUIA-DE-ESTUDIO.md) | Preguntas probables de una defensa técnica, con las respuestas |
| [.env.example](.env.example) | Contrato de configuración |

---

## Convenciones

- Español para los nombres de dominio (`Comercio`, `Interaccion`), inglés para
  los términos técnicos (`Repository`, `Service`, `Dto`).
- `snake_case` en la base, `PascalCase` en C#, resuelto con
  `EFCore.NamingConventions`.
- `async/await` con `CancellationToken` en todo el acceso a datos.
- Un commit por bloque funcional terminado.
