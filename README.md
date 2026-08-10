# ZOCO Tasks — Backend

Gestor interno de comercios para seguimiento de oportunidades comerciales.
API REST en **ASP.NET Core 10** sobre **PostgreSQL**, con detección de
conflictos de edición concurrente y análisis asistido por IA.

> El frontend vive en un repositorio aparte: [`zocotasks-frontend`](https://github.com/valentin21103/zocotasks-frontend).
>
> **API desplegada:** https://zocotasks-backend.onrender.com — el plan
> gratuito suspende el servicio tras un rato sin tráfico; la primera
> petición después de eso tarda cerca de un minuto en responder.

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
| Modelo de dominio, persistencia y migración | ✅ Verificado contra base real |
| API REST: CRUD, búsqueda, filtros, orden, paginación | ✅ Verificado |
| Concurrencia optimista (ETag / If-Match / 409) | ✅ Verificado con dos escrituras concurrentes |
| Validación server-side y manejo de errores | ✅ |
| Autenticación JWT + roles Admin/Vendedor | ✅ Verificado en producción |
| Feature "Analizar oportunidad" (Google Gemini) | ✅ Verificado contra el proveedor real |
| Tests unitarios | ✅ 18 en verde |
| CI (GitHub Actions) | ✅ |
| Docker + deploy público (Render) | ✅ Desplegado y verificado |
| Rate limiting | ✅ Verificado en producción |
| Frontend (Angular) | ✅ Desplegado en Vercel |

Lo marcado como verificado no significa "compila": significa que se comprobó su
comportamiento contra PostgreSQL real.
Ver [Verificaciones](#verificaciones-realizadas).

## Endpoints

Todos requieren `Authorization: Bearer <token>`, salvo `/api/auth/login` y
`/api/health`.

```
POST   /api/auth/login                      público

GET    /api/comercios                       búsqueda full text, filtros, orden, paginación
GET    /api/comercios/{id}                  detalle + interacciones · devuelve ETag
POST   /api/comercios                       201 + Location
PUT    /api/comercios/{id}                  requiere If-Match
PATCH  /api/comercios/{id}/estado           requiere If-Match
DELETE /api/comercios/{id}                  baja lógica · solo Admin

GET    /api/comercios/{id}/interacciones
POST   /api/comercios/{id}/interacciones
DELETE /api/comercios/{id}/interacciones/{interaccionId}    solo Admin

POST   /api/comercios/{id}/analizar         "Analizar oportunidad" (Gemini)

GET    /api/catalogos/estados
GET    /api/catalogos/rubros
GET    /api/catalogos/tipos-interaccion

GET    /api/rubros                          ABM de rubros · solo Admin
POST   /api/rubros
PUT    /api/rubros/{id}
DELETE /api/rubros/{id}

GET    /api/health                          sonda de vida, público, no toca la base
GET    /api/health/db                       verifica conexión y migraciones · solo Admin
```

### Códigos de respuesta

Todos los errores salen como `ProblemDetails` (RFC 7807) con un campo `codigo`
estable, para que el cliente discrimine sin depender del texto.

| Código | Cuándo |
|---|---|
| `400` | Formato inválido — incluye el detalle **campo por campo** |
| `401` | Falta el token o no es válido |
| `403` | El rol del usuario no alcanza (por ejemplo, Vendedor intentando eliminar) |
| `404` | No existe o fue dado de baja |
| `409` | Transición de estado inválida **o** conflicto de concurrencia |
| `422` | Regla de negocio: CUIT repetido, rubro dado de baja |
| `428` | Falta el header `If-Match` en una operación que modifica |

---

## Qué resuelve

Un equipo comercial necesita registrar comercios interesados y seguir su avance
por un embudo de ventas:

```
Nuevo → Contactado → Interesado → Documentación → Aprobado
   └────────────────────────────────────────────→ Rechazado
```

Ese es el orden natural, pero el movimiento entre estados es libre: se puede
saltear etapas, corregir hacia atrás o reabrir una oportunidad ya cerrada. Un
pipeline rígido dejaba trabado a un vendedor que cargó mal un estado, sin
ninguna forma de corregirlo.

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
- `API` referencia a `Infrastructure` para registrar los repositorios y el
  `DbContext` en `Program.cs`, en el arranque — no en el resto del código.

La consecuencia práctica más visible: la columna `search_vector` es de tipo
`tsvector`, cuyo tipo CLR pertenece a Npgsql. Como `Domain` no puede
referenciarlo, se modeló como **shadow property** en la configuración de EF.
La entidad `Comercio` nunca la conoce.

---

## Modelo de datos

```mermaid
erDiagram
    COMERCIO ||--o{ INTERACCION : "registra"
    RUBRO ||--o{ COMERCIO : "clasifica"
    ESTADO_COMERCIO ||--o{ COMERCIO : "estado actual de"
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

### Las decisiones que definen este modelo

**1. Estado como enum en C# *y* tabla lookup; rubro y tipo de interacción solo
como tabla.** La regla que ordena el modelo:

> Si el valor **tiene lógica asociada**, va como enum en el código.
> Si es **una etiqueta que el usuario administra**, va como tabla.

| | Lógica asociada | Lista | Modelado |
|---|---|---|---|
| Estado | Sí — máquina de estados | Cerrada: la consigna dice «estados posibles» | enum + tabla lookup |
| Tipo de interacción | No | Abierta: la consigna dice «por ejemplo» | tabla |
| Rubro | No | Abierta, la administra el usuario | tabla |

El estado lleva además tabla porque necesita `orden` (posición en el embudo) y
`es_final`, dos columnas que un enum no puede tener. No hay duplicación: el seed
de la tabla se genera recorriendo el enum, así que agregar un estado es tocar un
solo lugar.

**2. La máquina de estados vive en el dominio.**
`Comercio.CambiarEstado()` es el único camino para transicionar, y valida contra
`MaquinaEstadoComercio` antes de mutar. Ningún camino de código puede persistir
un estado inválido, ni siquiera por descuido.

**3. Soft delete.**
Borrar un comercio en duro se llevaría por cascada sus interacciones — la
evidencia del trabajo comercial. `fecha_eliminacion` nullable con filtro global
de EF: los eliminados desaparecen de toda consulta salvo que se pidan
explícitamente.

### Lo que deliberadamente **queda fuera** del modelo

Dos estructuras que aparecen naturalmente en un CRM y que acá se descartaron a
conciencia. Se documentan porque decidir qué no construir también es diseño:

**Una tabla de historial de estados.** Registrar cada transición del pipeline
daría trazabilidad del embudo, pero la consigna define la entrada del análisis
como *«la información del comercio y de sus notas/interacciones»* — el historial
no está en esa lista.

*Costo asumido:* el análisis no puede saber hace cuánto que el comercio está en
su estado actual. *Se justificaría* con requerimientos de reporting sobre el
embudo —tiempo promedio por etapa, tasa de conversión— que hoy no están
pedidos.

**Persistir el resultado del análisis de IA.** La consigna pide **generar** el
análisis, no guardarlo. Regenerarlo en cada consulta garantiza que siempre
refleje el estado actual del comercio, sin riesgo de presentar uno viejo como
vigente. Persistirlo, además, abre la puerta a un módulo de «análisis
anteriores» que la consigna no pide.

*Costos asumidos:* cada consulta es una llamada al modelo, que se paga y demora
segundos; y como los modelos de lenguaje no son determinísticos, el texto puede
variar entre corridas. *Se justificaría* con volumen real de uso: la
implementación sería SHA256 del contexto como clave de caché.

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
migraciones de EF Core.

---

## Variables de entorno

Todas están documentadas en [`.env.example`](.env.example), versionado sin
valores. El doble guion bajo es el separador de secciones de la configuración de
.NET: `ConnectionStrings__ZocoDb` equivale a `ConnectionStrings:ZocoDb`.

| Variable | Obligatoria | Para qué |
|---|:---:|---|
| `ConnectionStrings__ZocoDb` | Sí | Base de datos principal |
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
| Tablas creadas | 9 + historial de migraciones |
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

18 tests unitarios en verde: transiciones de estado sobre `Comercio`, validación
de rubros, y el servicio de análisis (incluye que una falla del proveedor de IA
devuelva una respuesta degradada en vez de propagar el error).

El **409 por conflicto de concurrencia** está verificado a mano contra Neon en
producción, con los valores reales de `xmin` — ver
[Verificaciones](#verificaciones-realizadas).

---

## Bonus implementados

| Bonus | Estado | Nota |
|---|:---:|---|
| Autenticación y roles | ✅ | JWT (12 h, sin refresh) + roles Admin/Vendedor, verificado en producción |
| Búsqueda full text | ✅ | Columna generada + índice GIN, diccionario español con stemming verificado |
| Paginación | ✅ | Con tope de 100 por página |
| Migraciones | ✅ | Con seed de catálogos incluido |
| Docker | ✅ | Imagen multietapa, corre con usuario sin privilegios |
| CI/CD | ✅ | GitHub Actions build + test; Render despliega solo si el CI pasa (`After CI checks pass`) |
| Deploy público | ✅ | [zocotasks-backend.onrender.com](https://zocotasks-backend.onrender.com) |
| Rate limiting | ✅ | 2 intentos por minuto por IP en `/api/auth/login` |

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

Las variables de entorno requeridas están documentadas en
[.env.example](.env.example).

---

## Convenciones

- Español para los nombres de dominio (`Comercio`, `Interaccion`), inglés para
  los términos técnicos (`Repository`, `Service`, `Dto`).
- `snake_case` en la base, `PascalCase` en C#, resuelto con
  `EFCore.NamingConventions`.
- `async/await` con `CancellationToken` en todo el acceso a datos.
- Un commit por bloque funcional terminado.
