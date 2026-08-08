# Decisiones de arquitectura e infraestructura

Documento vivo. Cada decisión sigue el mismo formato:
**contexto → opciones → qué elegí → por qué → cuándo elegiría distinto.**

La última columna es la importante: una decisión sin condiciones de reversión es
un dogma, no una decisión de ingeniería.

---

## Índice

- [1. Arquitectura](#1-arquitectura)
- [2. Modelo de datos](#2-modelo-de-datos)
- [3. Concurrencia optimista](#3-concurrencia-optimista)
- [4. Infraestructura de base de datos](#4-infraestructura-de-base-de-datos)
- [5. Configuración y secretos](#5-configuración-y-secretos)
- [6. Seguridad de dependencias](#6-seguridad-de-dependencias)
- [7. Tooling y repositorio](#7-tooling-y-repositorio)
- [9. Funcionalidad inteligente](#9-funcionalidad-inteligente)
- [10. Entrega: CI/CD, contenedor y deploy](#10-entrega-cicd-contenedor-y-deploy)

---

## 1. Arquitectura

### 1.1 Arquitectura en capas con dependencias hacia adentro

**Contexto.** Cuatro proyectos: API, Business, Infrastructure, Domain.

**Opciones.** Proyecto único por capa técnica (Controllers/Services/Data en un
mismo assembly); arquitectura en capas con proyectos separados; vertical slices.

**Decisión.** Capas separadas en proyectos, con las dependencias apuntando hacia
`Domain`:

```
API ──────> Business ──────> Domain
 └──> Infrastructure ──┘
```

**Por qué.** La separación en proyectos no es cosmética: el compilador la
**hace cumplir**. Si `Domain` no referencia a `Infrastructure`, es literalmente
imposible que una entidad termine dependiendo de EF Core, por descuido o por
apuro. Una separación en carpetas dentro de un mismo assembly depende de que
todos se acuerden de respetarla.

**Cuándo elegiría distinto.** En un CRUD chico sin lógica de dominio, cuatro
proyectos son ceremonia pura y elegiría un assembly único. Acá se justifica
porque hay una máquina de estados, reglas de transición y un feature de IA.

---

### 1.2 `Domain` sin una sola dependencia externa

**Contexto.** La capa de entidades podría usar atributos de EF Core, de
validación, o tipos del proveedor de base.

**Decisión.** Cero paquetes NuGet en `ZocoTasks.Domain`. Ni EF Core, ni
FluentValidation, ni ASP.NET.

**Por qué.** Tres consecuencias concretas:
1. Las reglas de dominio se testean **sin levantar base ni contenedor de DI**.
   `MaquinaEstadoComercio` es una clase estática pura: el test es una llamada.
2. Cambiar de EF Core a Dapper no toca una sola entidad.
3. Obliga a que las decisiones de persistencia vivan donde corresponde, en la
   configuración de EF, en lugar de filtrarse como atributos en las entidades.

**Consecuencia real que tuve que resolver.** La columna `search_vector` es de
tipo `tsvector`, cuyo tipo CLR (`NpgsqlTsVector`) pertenece al paquete de
Npgsql. Ponerla como propiedad de `Comercio` habría roto la regla. La resolví
como **shadow property** declarada en `ComercioConfiguration`: la columna existe
en la base y el repositorio la consulta con `EF.Property<>`, pero la entidad
nunca la conoce. Ver [2.6](#26-search_vector-como-shadow-property).

**Cuándo elegiría distinto.** Nunca en este proyecto. En un prototipo de una
tarde, permitiría atributos de EF en las entidades para ir más rápido.

---

### 1.3 El registro de dependencias vive en `Program.cs`, no en un archivo por capa

**Contexto.** Cada capa tenía un `DependencyInjection.cs` con un método de
extensión (`AddInfrastructure`, `AddBusiness`), y `Program.cs` solo los llamaba.

**El argumento a favor de esa forma.** La capa API no necesitaba conocer
`ZocoDbContext` ni las clases de repositorio: bastaba con invocar un método.
Es una separación de capas más estricta.

**Decisión: se aplanó todo a `Program.cs`.**

**Por qué.** El beneficio era real pero chico, y el costo era concreto: para
responder *"¿dónde te conectás a la base?"* había que abrir otro archivo y saber
qué es un método de extensión. En un proyecto de cuatro assemblies y un solo
desarrollador, esa indirección no se paga. **Código que no se puede explicar de
un vistazo resta más de lo que suma la prolijidad.**

Hoy la conexión se lee en la línea 17 de `Program.cs`, junto al resto del
arranque, en orden de arriba hacia abajo.

**Lo que se perdió, dicho explícitamente.** `ZocoTasks.API` ahora referencia por
nombre a `ZocoDbContext` y a las tres clases de repositorio.

**Cuándo volvería atrás.** Con varios equipos tocando el mismo `Program.cs`, o
cuando el arranque pase de unas 120 líneas y empiece a ser difícil de leer.

---

### 1.4 DTOs en `Business`, no en `Domain`

**Decisión.** Los DTOs viven en `ZocoTasks.Business/DTOs/`.

**Por qué.** Un DTO es un **contrato de aplicación**: refleja lo que la API
expone, que no es lo mismo que lo que el dominio modela. Si vivieran en
`Domain`, la capa más pura terminaría conociendo la forma de la API, y cambiar
un JSON de respuesta obligaría a tocar el dominio.

---

## 2. Modelo de datos

### 2.1 Estado como enum en C# **y** tabla lookup en la base

**Contexto.** El pipeline tiene 6 estados fijos:
`Nuevo → Contactado → Interesado → Documentación → Aprobado / Rechazado`.

**Opciones.**
| Opción | Problema |
|---|---|
| Solo `varchar` con el nombre | Sin integridad: nada impide guardar `"Aprobadoo"` |
| Solo enum en C#, `int` sin FK | La base no valida nada; un `UPDATE` manual rompe el modelo |
| Solo tabla, sin enum | El código pierde el tipo fuerte; `if (estado.Id == 3)` es ilegible |
| **Ambos** | Duplicación aparente |

**Decisión.** `enum EstadoComercioEnum : short` en el código, persistido como
`smallint` con **FK real** contra `estado_comercio`. La PK de la tabla lookup
*es* el enum.

**Por qué.** Cada mitad aporta algo que la otra no puede:
- El **enum** da tipo fuerte, exhaustividad en `switch`, y permite que la
  máquina de estados viva en el dominio.
- La **tabla** da integridad referencial (la base rechaza un estado inexistente),
  joins para reporting, y dos columnas que el enum no puede tener: `orden`
  (posición en el embudo) y `es_final` (Aprobado y Rechazado son terminales).

La duplicación es aparente porque **hay una sola fuente de verdad**: el seed de
la tabla se genera recorriendo el enum y consultando `MaquinaEstadoComercio`.
Ver `CatalogosSeed.Estados`. Agregar un estado es tocar el enum y nada más.

**Cuándo elegiría distinto.** Si los estados fueran configurables por el
usuario, el enum sobra y va solo tabla. Si nunca hubiera reporting ni
necesidad de `orden`/`es_final`, iría solo enum.

---

### 2.2 Rubro como tabla pura, **sin** enum

**Decisión.** `rubro` es una tabla con ABM y flag `activo`. No hay
`RubroEnum`.

**Por qué.** Es exactamente el criterio opuesto al del estado, y el contraste es
el punto: **los estados cambian cuando cambia el código; los rubros cambian sin
que cambie el código.** Agregar "Farmacia" no debería requerir un deploy.

El flag `activo` en lugar de borrar: si se elimina un rubro, los comercios
históricos que lo referencian quedan huérfanos. Desactivarlo lo saca de los
combos nuevos sin romper el pasado.

---

### 2.3 Sin tabla de historial de estados

**Contexto.** Un CRM suele registrar cada transición del pipeline en una tabla
aparte: estado anterior, estado nuevo, usuario, fecha y motivo.

**El argumento a favor.** Daría *señal temporal* al feature de IA: un comercio
veinte días trabado en "Documentación" es información útil para recomendar el
próximo paso, y no existe en ningún otro campo.

**Decisión: no se modela.** Dos razones:

1. La consigna define la entrada del análisis como *"la información del comercio
   y de sus **notas/interacciones**"*. El historial no está en esa lista, y el
   ejemplo que la propia consigna da se resuelve enteramente con texto.
2. El bonus de auditoría, si se implementa, registra **todo** cambio de forma
   genérica desde un interceptor de `SaveChanges`. Una tabla dedicada a auditar
   únicamente el estado sería un caso particular de algo ya cubierto.

**Costo asumido.** El análisis pierde la noción de "hace cuánto que está acá".

**Cuándo la agregaría.** Si apareciera un requerimiento de reporting sobre el
embudo —tiempo promedio por etapa, tasa de conversión entre estados— la tabla
se vuelve necesaria y el interceptor genérico no alcanza, porque guarda los
cambios como texto y no en un formato consultable.

**Lo que no depende de ella.** La integridad del pipeline:
`Comercio.CambiarEstado()` es el único camino para transicionar y valida contra
la máquina de estados antes de mutar, con tabla o sin ella.

---

### 2.4 El análisis **no** se persiste

**Contexto.** "Analizar oportunidad" llama a un modelo de lenguaje. Se puede
guardar el resultado con un hash del contexto para cachearlo, o devolverlo al
vuelo.

**Decisión.** No se persiste. El endpoint arma el contexto, llama al proveedor y
devuelve el resultado.

**Por qué.** La consigna dice que el sistema debe **generar** el análisis, no
guardarlo. Regenerarlo en cada consulta tiene además una propiedad deseable:
siempre refleja el estado actual del comercio, sin riesgo de presentar un
análisis viejo como si fuera vigente. Y persistirlo abría la puerta a un módulo
de "análisis anteriores" que nadie pidió: alcance que no estaba en la consigna,
en una prueba con límite de tiempo.

**Costos asumidos, explícitos.**
- Cada consulta es una llamada al modelo: se paga y demora segundos.
- Los modelos de lenguaje **no son determinísticos**. El mismo contexto produce
  textos distintos entre corridas, así que el usuario puede ver el análisis
  redactado de otra forma sin que haya cambiado nada del comercio.

**Cuándo elegiría distinto.** Con volumen de uso real, el costo por token y la
latencia justifican el caché. La implementación sería directa: SHA256 del
contexto como clave, y devolver lo guardado si el hash coincide.

---

### 2.6 `search_vector` como shadow property

**Contexto.** Bonus de búsqueda full text. La columna es `tsvector`, cuyo tipo
CLR viene del paquete de Npgsql, que `Domain` no puede referenciar
([1.2](#12-domain-sin-una-sola-dependencia-externa)).

**Opciones.**
1. Propiedad `NpgsqlTsVector` en `Comercio` → rompe la pureza de Domain.
2. Calcular el vector en el servicio al guardar → hay que acordarse siempre, y
   se desincroniza con cualquier `UPDATE` que no pase por el servicio.
3. **Columna generada por la base + shadow property en EF.**

**Decisión.** La 3.

```sql
search_vector tsvector GENERATED ALWAYS AS (
  to_tsvector('spanish',
    coalesce(nombre_comercial,'') || ' ' ||
    coalesce(nombre_contacto,'')  || ' ' ||
    coalesce(notas,''))) STORED
```

**Por qué.** `GENERATED ALWAYS ... STORED` significa que **PostgreSQL la
mantiene**. No hay forma de que quede desactualizada: ni un `UPDATE` manual, ni
un bug en el servicio, ni una migración de datos pueden desincronizarla. Y al
ser shadow property, `Domain` sigue sin conocer a Npgsql.

**Por qué diccionario `spanish` y no `simple`.** Hace *stemming*: reduce las
palabras a su raíz. Verificado contra la base real — buscar `problema` encuentra
"Problemas", buscar `sucursal` encuentra "sucursales", y `de` se descarta como
stopword. El vector almacenado es `'parrill' 'problem' 'sucursal' 'transferent'`,
raíces y no palabras literales. Con `simple`, buscar "problema" no encontraría
"Problemas".

**Índice GIN y no B-tree.** Un B-tree indexa un valor por fila y sirve para
comparaciones de orden. Un `tsvector` contiene *muchos* lexemas por fila y la
consulta pregunta "¿contiene este lexema?". GIN (Generalized Inverted Index) es
un índice invertido: mapea cada lexema a las filas que lo contienen. Es la
estructura correcta para el problema.

---

### 2.7 Soft delete

**Decisión.** `fecha_eliminacion` nullable + `HasQueryFilter` global.

**Por qué.** Un borrado físico se llevaría puestas las interacciones y el
historial por cascada — que son justamente la evidencia del trabajo comercial.
Borrar un comercio no debería borrar el registro de que se lo llamó tres veces.

El filtro global hace que los eliminados desaparezcan de **toda** consulta
automáticamente; recuperarlos requiere pedir `IgnoreQueryFilters()` de forma
explícita. El default es seguro, y ver los borrados es una decisión consciente.

---

### 2.8 `citext` para los emails

**Opciones.** `varchar` normalizando a minúsculas en cada insert; `varchar` con
índice funcional `lower(email)`; `citext`.

**Decisión.** `citext` (extensión nativa de PostgreSQL).

**Por qué.** Normalizar en el código funciona hasta que alguien inserta por otro
camino — un script, una migración de datos, otro servicio. `citext` mueve la
regla al **tipo de la columna**: la comparación case-insensitive la garantiza el
motor, y el índice único rechaza `Juan@mail.com` contra `juan@mail.com` sin que
nadie tenga que acordarse de nada.

---

### 2.9 `snake_case` en la base, `PascalCase` en C#

**Decisión.** Paquete `EFCore.NamingConventions` con
`.UseSnakeCaseNamingConvention()`.

**Por qué.** PostgreSQL pliega los identificadores sin comillas a minúsculas. Si
EF genera `"NombreComercial"`, cada consulta manual en psql o en un dashboard
necesita comillas dobles exactas. Con snake_case, `select nombre_comercial from
comercio` simplemente funciona. Es el idioma nativo de Postgres.

---

## 3. Concurrencia optimista

> Este es el requisito destacado de la consigna: *"dos usuarios no deben poder
> modificar el mismo registro accidentalmente sin detectar el conflicto"*.

### 3.1 `xmin` en lugar de una columna de versión propia

**Contexto.** Hay que detectar que dos usuarios editaron el mismo comercio.

**Opciones.**
| Opción | Problema |
|---|---|
| Bloqueo pesimista (`SELECT FOR UPDATE`) | Mantiene transacciones abiertas mientras el usuario piensa. Inaceptable en HTTP. |
| Columna `version int` propia | Hay que acordarse de incrementarla en cada `UPDATE`. Un solo camino que la olvide rompe la garantía. |
| `rowversion` / `timestamp` | No existe en PostgreSQL, es de SQL Server. |
| **Columna de sistema `xmin`** | Específica de PostgreSQL. |

**Decisión.** `xmin`, mapeada a la propiedad `Comercio.Version` (un `uint`).

**Por qué.** `xmin` es una columna de sistema que PostgreSQL mantiene sola:
guarda el ID de la transacción que escribió la fila por última vez, y **cambia
en cada UPDATE sin excepción**. No hay forma de olvidarse de incrementarla,
porque no la incrementa nadie: es el motor. Elimina por construcción la clase
entera de bugs de "me olvidé de subir la versión".

**Verificado empíricamente contra Neon**, no solo en el diseño:

```
INSERT ... RETURNING id, xmin  →  id=1, xmin=2056
UPDATE ... RETURNING id, xmin  →  id=1, xmin=2057
```

**Detalle no obvio.** `xmin` es una columna de sistema: **no se puede crear en un
`CREATE TABLE`**. Confirmé que la migración generada no intenta hacerlo — si lo
intentara, fallaría con *"column name xmin conflicts with a system column name"*.
Npgsql la reconoce y la excluye del DDL.

**Por qué `Version` sí es propiedad de la entidad y `search_vector` no.** Porque
`uint` es un tipo de la BCL y no arrastra ninguna dependencia a `Domain`,
mientras que `NpgsqlTsVector` sí. Además el servicio **necesita leer** el valor
para emitir el `ETag`, cosa que no hace falta con el vector de búsqueda.

**Cuándo elegiría distinto.** Si el sistema tuviera que soportar otro motor
además de PostgreSQL, `xmin` deja de servir y habría que ir a una columna
propia. Hoy el proyecto está casado con Postgres a conciencia (`citext`,
`tsvector`, `jsonb`), así que no es un costo nuevo.

### 3.2 Flujo HTTP: `ETag` / `If-Match` / `409`

**Decisión.**
1. El `GET` devuelve el `xmin` en el header `ETag`.
2. El `PUT` exige ese valor en `If-Match`.
3. Si `SaveChanges` detecta el conflicto, se responde **409 Conflict** con el
   estado actual del registro.

**Por qué esos headers y no un campo en el body.** `ETag` e `If-Match` son
HTTP estándar (RFC 9110) para exactamente este problema. Un cliente genérico,
un proxy o una herramienta de API los entienden sin documentación. Un campo
`version` en el JSON sería una convención privada que hay que explicar.

**Por qué devolver el estado actual en el 409.** Un 409 pelado deja al usuario
sin salida más que recargar y perder lo que escribió. Devolviendo la versión
actual, el front puede mostrar qué cambió y ofrecer resolver el conflicto.

---

## 4. Infraestructura de base de datos

### 4.1 PostgreSQL gestionado en Neon

**Decisión.** Neon (PostgreSQL serverless), región `sa-east-1` (São Paulo).

**Por qué.** Es PostgreSQL real, no un emulado: `citext`, `tsvector`, `jsonb` y
`xmin` funcionan igual que en una instancia propia — verificado. La región
sudamericana minimiza latencia desde Argentina. El tier gratuito alcanza de
sobra para la prueba y permite que el evaluador vea la app contra una base real
sin instalar nada.

**Contrapartida asumida.** Neon escala a cero cuando no hay tráfico. La primera
consulta tras un período de inactividad despierta el compute y tarda más. Lo
mitigo con `EnableRetryOnFailure(maxRetryCount: 5)`, para que ese arranque en
frío se reintente en lugar de presentarse al usuario como un error.

---

### 4.2 Conexión **directa**, no la *pooled*

**Contexto.** Neon ofrece dos endpoints:

```
ep-xxx-pooler.sa-east-1.aws.neon.tech   ← PgBouncer
ep-xxx.sa-east-1.aws.neon.tech          ← Postgres directo
```

**Decisión.** La directa (sin `-pooler`).

**Por qué.** PgBouncer resuelve el problema de **muchos procesos efímeros**
abriendo conexiones: Lambda, Vercel, funciones serverless, donde cada invocación
abre la suya y agota el límite del servidor. Una API ASP.NET Core **no tiene ese
problema**: es un proceso de larga vida y Npgsql ya poolea del lado del cliente.
Poner PgBouncer encima agrega un salto de red y restricciones sin resolver nada
nuevo.

Las restricciones no son teóricas. El PgBouncer de Neon corre en *transaction
mode*, donde cada transacción puede caer en una conexión física distinta. Eso
rompe todo lo que dependa del estado de sesión: prepared statements, `SET`,
tablas temporales y advisory locks. **Las migraciones de EF Core usan
justamente advisory locks** para serializar la aplicación del esquema — se ve en
la salida de `database update`: *"Acquiring an exclusive lock for migration
application"*.

**Cuándo elegiría distinto.** Si la API escalara a varias instancias, o si se
desplegara como funciones serverless, la pooled pasa a ser la correcta para la
aplicación — manteniendo la directa para las migraciones.

---

### 4.3 `SSL Mode=VerifyFull` en vez de `Trust Server Certificate=true`

**Decisión.** `SSL Mode=VerifyFull`.

**Por qué.** `Trust Server Certificate=true` cifra el tráfico pero **acepta
cualquier certificado**, lo que anula la protección contra man-in-the-middle:
un atacante que intercepte la conexión presenta su propio certificado y el
cliente lo acepta feliz. Se usa cuando el servidor tiene un certificado
autofirmado y no queda otra. **Neon usa certificados de una CA pública real**,
así que se puede validar de verdad y no hay razón para bajar la guardia.

`VerifyFull` además valida que el hostname del certificado coincida con el que
se está conectando, que es lo que cierra el ataque por completo.

---

### 4.4 `Maximum Pool Size=20`

**Por qué.** Npgsql abre hasta **100** conexiones por defecto. El compute chico
de Neon tolera bastante menos, y una sola instancia de API no necesita cien
conexiones simultáneas contra la base. Capearlo evita que un pico de tráfico
agote el servidor y tumbe la aplicación entera.

---

### 4.5 `channel_binding` se descarta al traducir la cadena

**Contexto.** La cadena que da Neon es una URI de **libpq**:

```
postgresql://user:pass@host/neondb?sslmode=require&channel_binding=require
```

**Decisión.** Traducirla a formato Npgsql descartando `channel_binding`.

**Por qué.** `channel_binding` es un parámetro de **libpq** (el cliente C de
Postgres); no existe en el formato de cadena de conexión de Npgsql, y dejarlo
provoca un error de parseo. No se pierde seguridad: Npgsql **ya negocia
SCRAM-SHA-256-PLUS automáticamente** cuando la conexión va por SSL. La
protección es la misma, solo que no se declara.

Copiar la URI de Neon tal cual en una app .NET es un error frecuente. La
traducción completa:

| libpq (Neon) | Npgsql (.NET) |
|---|---|
| `postgresql://user:pass@host/db` | `Host=…;Database=…;Username=…;Password=…` |
| `sslmode=require` | `SSL Mode=VerifyFull` (más estricto, ver [4.3](#43-ssl-modeverifyfull-en-vez-de-trust-server-certificatetrue)) |
| `channel_binding=require` | *(se omite: automático)* |
| *(no aplica)* | `Maximum Pool Size=20` |

---

### 4.6 Seed por migración (`HasData`) y no por script aparte

**Decisión.** Los catálogos viajan dentro de la migración vía `HasData`.

**Por qué.** Una base recién creada queda **consistente con un solo comando**
(`dotnet ef database update`). No hay un paso manual que alguien pueda olvidar,
y el estado de los catálogos queda versionado en git junto al esquema: se puede
ver en qué commit se agregó un rubro.

**Contrapartida asumida.** `HasData` exige PKs explícitas y fijas, y EF gestiona
esas filas como propias. Para `rubro`, que tiene ABM, significa que las 9 filas
iniciales son "de la migración" y las que agregue el usuario son suyas. Es
aceptable porque son solo la carga inicial.

---

## 5. Configuración y secretos

### 5.1 Ningún secreto en el repositorio

**Decisión.** `appsettings.json` tiene la clave de conexión **vacía**. El valor
real llega por `user-secrets` (desarrollo) o variable de entorno
`ConnectionStrings__ZocoDb` (CI y deploy). Se versiona `.env.example` con las
claves y sin los valores.

**Por qué.** Un secreto commiteado no se borra con un commit que lo saque:
queda en la historia de git para siempre, y hay bots que escanean GitHub
buscando exactamente eso. `user-secrets` guarda el archivo **fuera del árbol del
repositorio** (en `%APPDATA%\Microsoft\UserSecrets\<id>`), así que no hay forma
de commitearlo por accidente.

**Por qué `.env.example` igual se versiona.** Sin él, quien clona no sabe qué
variables necesita. Documenta el *contrato de configuración* sin exponer valores.

### 5.2 Fallar en el arranque si falta la cadena de conexión

**Decisión.** `AddInfrastructure` lanza una excepción con un mensaje explícito
si la clave está vacía.

**Por qué.** La alternativa es que la app levante bien y explote en el primer
request con un error de conexión críptico. Un error de configuración tiene que
ser evidente **de inmediato** y decir exactamente qué falta y cómo definirlo.

---

## 6. Seguridad de dependencias

### 6.1 Pin de `Microsoft.OpenApi` en 2.7.5

**Contexto.** `dotnet restore` reportó:

```
NU1903: El paquete "Microsoft.OpenApi" 2.0.0 tiene una vulnerabilidad
de gravedad alta conocida (GHSA-v5pm-xwqc-g5wc)
```

No es una dependencia directa: la arrastra `Microsoft.AspNetCore.OpenApi`.

**Opciones probadas, en orden.**
1. **Subir el paquete padre a 10.0.10** (la última). *Falló*: sigue arrastrando
   `Microsoft.OpenApi` 2.0.0. Verificado con `dotnet list package
   --include-transitive`.
2. **Subir a `Microsoft.OpenApi` 3.x** (la última estable es 3.9.0). Descartado:
   3.x introduce cambios de API incompatibles con el `AddOpenApi` de este
   release de ASP.NET Core.
3. **Pinear la última 2.x (2.7.5).** Funciona: la advertencia desaparece.

**Decisión.** Referencia directa a `Microsoft.OpenApi` 2.7.5.

**Por qué.** Una referencia directa gana sobre la resolución transitiva de
NuGet, así que eleva la versión sin tocar el paquete padre. Quedarse en la línea
2.x mantiene la compatibilidad de API.

**Deuda técnica asumida y documentada.** Es un pin de seguridad, no una
dependencia real del código. Está comentado en el `.csproj` con instrucción de
revisarlo y quitarlo cuando ASP.NET Core actualice su dependencia. Un pin sin
explicación se convierte en un misterio que nadie se anima a tocar.

---

## 7. Tooling y repositorio

### 7.1 `dotnet-ef` como herramienta **local**, no global

**Decisión.** Manifiesto `.config/dotnet-tools.json` versionado, con
`dotnet-ef` 10.0.10.

**Por qué.** Una herramienta global depende de qué tenga instalado cada máquina:
si el evaluador tiene `dotnet-ef` 9, los comandos pueden comportarse distinto o
fallar. Con el manifiesto versionado, `dotnet tool restore` instala **la misma
versión exacta** para todos, y queda registrado en git.

### 7.2 Factory de diseño para las migraciones

**Decisión.** `ZocoDbContextFactory : IDesignTimeDbContextFactory<ZocoDbContext>`.

**Por qué.** Sin ella, generar una migración obliga a arrancar el host completo
de la API y, por lo tanto, a tener la cadena de conexión real disponible. La
factory desacopla el **diseño** del esquema de su **aplicación**: `migrations
add` solo necesita construir el modelo en memoria, y recién `database update` se
conecta de verdad.

**Contrapartida conocida.** EF prioriza la factory sobre el service provider de
la aplicación, así que los comandos de EF leen la variable de entorno
`ConnectionStrings__ZocoDb` y **no** `user-secrets`. Está documentado en el
README.

### 7.3 `.gitattributes` con `text=auto eol=lf`

**Por qué.** Un repositorio creado en Windows guarda CRLF. Cuando el CI corre en
Linux, git ve *todos* los archivos como modificados y los diffs se vuelven
inútiles. Con `text=auto eol=lf`, el repositorio guarda LF y cada sistema recibe
en el checkout lo que le corresponde. Es barato ahora e imposible de arreglar
limpio después.

---

## 8. Revisión crítica

> Autocrítica del estado actual. Una decisión que nadie revisa deja de ser una
> decisión y pasa a ser una costumbre.

### 8.0 El criterio que ordena el tamaño del modelo

**Una tabla existe si responde a algo que la consigna pide o a un bonus que se
va a implementar. No alcanza con que sea "buena idea".**

Es el criterio que deja afuera el historial de estados y la persistencia del
análisis (ver [2.3](#23-sin-tabla-de-historial-de-estados) y
[2.4](#24-el-análisis-no-se-persiste)), y el que se aplica a lo que sigue.

### 8.1 Deuda abierta: cuatro tablas sin código que las use

`usuario`, `rol`, `usuario_rol` y `audit_log` existen en la base y **ningún
código las escribe todavía**. Se justifican solo si se implementan los bonus de
autenticación y auditoría.

**Criterio de decisión:** el mismo de 8.0. Si al cierre del proyecto esos bonus
no están implementados, **estas tablas se eliminan antes de entregar**. Una
tabla vacía sin código asociado se lee como planificación fallida, no como
previsión.

### 8.2 A revisar: la factory de diseño agrega fricción

`ZocoDbContextFactory` permite generar migraciones sin conexión, pero EF la
prioriza sobre el service provider de la aplicación, así que los comandos de EF
leen la variable de entorno `ConnectionStrings__ZocoDb` y **no** `user-secrets`.
Obliga a exportar la variable a mano antes de cada `database update`.

**Alternativa:** eliminar la factory y dejar que EF use el host de la
aplicación. Menos código y menos fricción, a cambio de no poder generar
migraciones sin configuración presente. Pendiente de evaluar.

### 8.3 A no hacer salvo que aparezca la necesidad: `IGenericRepository<T>`

El plan original lo contemplaba. La crítica estándar es correcta: `DbSet<T>` ya
*es* un repositorio e `IQueryable` ya es el patrón de especificación, así que
un genérico encima suele agregar indirección sin agregar capacidad.

**Decisión revisada:** usar repositorios **concretos** (`IComercioRepository`)
donde hay queries reales con includes, filtros, orden y paginación, y **no**
crear el genérico salvo que aparezca duplicación real. El argumento "permite
testear sin base" pierde fuerza porque los tests de integración van contra
PostgreSQL real de todos modos.

### 8.4 Bajo observación: `EntidadBase`

Solo aporta `Id`. Es un punto de extensión para el interceptor de auditoría, que
necesita identificar entidades con clave entera de forma genérica. **Si la
auditoría no se implementa, esta clase queda sin justificación** y conviene
eliminarla.

---

## 8.5 Capa de aplicación y API

### 8.5.1 Repositorios concretos, sin `IGenericRepository<T>`

**Decisión.** `IComercioRepository`, `IInteraccionRepository` y
`ICatalogoRepository`. No hay repositorio genérico.

**Por qué.** `DbSet<T>` ya *es* un repositorio genérico: envolverlo no elimina
repetición, le cambia el nombre. Y el mínimo común denominador que puede exponer
una interfaz genérica (traer por id, traer todos) no alcanza para la consulta
principal, que necesita includes, full text search, orden dinámico, paginación y
proyección. Para cubrirla habría que exponer `IQueryable`, y ahí la abstracción
deja de abstraer: quien la use termina escribiendo LINQ de EF Core igual.

**El argumento de testabilidad, que es más flojo de lo que suena.** Se dice que
un repositorio permite testear servicios sin base. Pero un mock no valida FKs,
no aplica el índice único de CUIT y no lanza `DbUpdateConcurrencyException`: los
tests pasan y producción rompe. Los tests que importan acá van contra PostgreSQL
real.

### 8.5.2 La validación tiene dos niveles, y devuelven códigos distintos

| Qué se valida | Dónde | Respuesta |
|---|---|---|
| Formato, obligatoriedad, largos, CUIT por módulo 11 | FluentValidation | **400** con el detalle campo por campo |
| CUIT repetido, rubro inexistente o dado de baja | El servicio, consultando la base | **422** |

**Por qué separarlos.** Un 400 dice "escribiste mal"; un 422 dice "está bien
escrito pero el estado del sistema no lo permite". Para el usuario son problemas
distintos y ameritan mensajes distintos: uno se corrige en el formulario, el
otro no.

### 8.5.3 La validación se dispara en el servicio, no en el controller

**Por qué.** Así vale sin importar quién llame: la API hoy, un job de
importación o un test mañana. Si viviera en el controller, cualquier otro camino
de entrada se saltearía las reglas.

### 8.5.4 El cambio de estado tiene su propio endpoint

**Decisión.** `PATCH /api/comercios/{id}/estado`, y `ActualizarComercioDto`
**no** incluye el campo estado.

**Por qué.** Si el estado se pudiera mandar en el `PUT` general, alguien podría
escribir `estado: "Aprobado"` en una edición cualquiera y saltearse la máquina
de estados. Separándolo, pasar por las reglas del pipeline es inevitable: no hay
otro camino.

### 8.5.5 Orden por lista blanca

**Decisión.** El campo de ordenamiento se resuelve con un `switch` sobre valores
conocidos; lo que no está, cae al orden por defecto.

**Por qué.** La alternativa —concatenar el nombre del campo recibido dentro de
la consulta— es una puerta de inyección SQL. Con la lista blanca, un parámetro
malicioso simplemente no matchea.

### 8.5.6 Las fechas de auditoría se completan en `SaveChangesAsync`

**Por qué.** En un solo lugar, para toda entidad `IAuditable`. Si cada servicio
tuviera que setearlas, alcanza con que un camino se olvide para dejar datos
inconsistentes — y es el tipo de bug que no se nota hasta que alguien ordena por
fecha.

### 8.5.7 Los enums viajan como texto en el JSON

**Por qué.** `"estado": "Documentacion"` en lugar de `"estado": 4`. La API se
lee sola y el frontend no tiene que mantener su propia tabla de equivalencias,
que es una fuente clásica de desincronización.

### 8.5.8 CORS expone el header `ETag`

**Por qué.** Por defecto el navegador **no deja leer** headers de respuesta que
no sean los de la lista segura. Sin `WithExposedHeaders("ETag")`, el front
recibe el ETag pero JavaScript no puede verlo, y sin verlo no puede mandar
`If-Match`. Toda la concurrencia optimista se cae por una línea que falta.

---

## 9. Funcionalidad inteligente

> Estado: **decidido, pendiente de implementar.**

### 9.1 OpenAI como proveedor, con *structured outputs*

**Contexto.** La consigna es explícita: *"no importa qué proveedor o tecnología
utilicen"*. El problema real no es cuál elegir, sino **cómo garantizar que la
respuesta tenga siempre la forma que la consigna pide**: resumen, nivel de
interés, próximo paso, tres preguntas y datos faltantes.

**Opciones para obtener esa estructura.**

| Opción | Problema |
|---|---|
| Pedir el formato en el prompt y parsear texto libre | Frágil. El modelo agrega preámbulos, cambia el orden, devuelve cuatro preguntas en vez de tres |
| Pedir JSON en el prompt y deserializar | Mejor, pero el JSON puede venir malformado o envuelto en ```` ``` ```` |
| **JSON Schema con `strict: true`** | Ninguno relevante |

**Decisión.** OpenAI con *structured outputs*: se le pasa el JSON Schema de
`AnalisisOportunidadDto` con `strict: true`, y la API **garantiza a nivel de
decodificación** que la respuesta valida contra ese esquema.

**Por qué.** Elimina por completo la clase de bugs de "el modelo respondió algo
que no puedo parsear". No hay que escribir defensas contra formatos
inesperados: la respuesta o cumple el contrato, o la llamada falla de forma
explícita. Es la diferencia entre confiar en un prompt y tener una garantía.

**Cuándo elegiría distinto.** Si el proveedor tuviera que ser intercambiable, el
esquema estricto es específico de OpenAI y habría que degradar a "pedir JSON y
validar a mano" para mantener portabilidad.

### 9.2 Fallo del proveedor: respuesta degradada, nunca un 500

**Decisión.** Si la llamada falla —sin API key, timeout, cuota agotada, caída
del servicio— el endpoint **no** devuelve 500. Devuelve el análisis con nivel de
interés `Indeterminado` y un mensaje explicando que no se pudo generar.

**Por qué.** Un proveedor externo caído no es un error de esta aplicación. El
resto del sistema sigue funcionando perfectamente, y el usuario merece saber
"no se pudo analizar ahora" en vez de una pantalla de error genérica. Además,
el enum `NivelInteres` tiene el valor `Indeterminado` justamente para esto:
**si el modelo no respondió, el sistema no inventa un nivel de interés.**

Se aplica un timeout explícito para que un proveedor lento no deje colgada la
request.

---

## 10. Entrega: CI/CD, contenedor y deploy

> Estado: **decidido, pendiente de implementar.**

### 10.1 GitHub Actions valida; Render despliega

**Contexto.** Render puede desplegar solo con cada push al repositorio. Si se
deja así, **despliega aunque los tests fallen** — que es exactamente lo que un
pipeline de CI/CD debería impedir.

**Opciones.**
1. Auto-deploy de Render activado + Actions corriendo tests en paralelo. Los
   tests informan, pero no frenan nada: el código roto llega a producción igual.
2. Actions construye la imagen, la sube a un registry y Render la toma. Más
   control, bastante más plomería.
3. **Auto-deploy de Render apagado + Actions dispara el deploy solo si los tests
   pasan.**

**Decisión.** La 3. El workflow hace `restore → build → test`, y **solo si todo
pasa y la rama es `main`**, hace una llamada al *Deploy Hook* de Render.

**Por qué.** Es el pipeline más simple que realmente **corta**. Los tests dejan
de ser decorativos: si fallan, el deploy no ocurre. Y no hay que administrar un
registry de imágenes ni credenciales de registry — Render sigue construyendo,
solo que cuando se le avisa.

La URL del Deploy Hook es un secreto y va en **GitHub Secrets**, nunca en el
repositorio.

### 10.2 Dockerfile multi-etapa

**Decisión.** Dos etapas: el SDK de .NET compila y publica; la imagen final
parte del runtime de ASP.NET y solo copia el resultado.

**Por qué.** El SDK pesa varias veces más que el runtime y **no tiene nada que
hacer en producción**: incluirlo agrega superficie de ataque (compiladores,
herramientas) sin aportar nada en tiempo de ejecución.

La imagen corre con un **usuario no root**. Si alguien logra ejecutar código
dentro del contenedor, no arranca con privilegios de administrador.

### 10.3 Las migraciones corren al arrancar la aplicación

**Contexto.** En Render no hay una consola donde correr `dotnet ef database
update` a mano después de cada deploy.

**Decisión.** La aplicación aplica las migraciones pendientes en el arranque.

**Por qué.** Un deploy deja la base y el código sincronizados sin intervención
manual, que es justamente lo que se espera de un despliegue automatizado.

**Contrapartida conocida y asumida.** Con varias instancias arrancando a la vez,
dos podrían intentar migrar en simultáneo. EF toma un lock a nivel de base para
serializarlo, pero el patrón correcto a partir de cierta escala es un paso de
migración separado del arranque (Render lo soporta como *Pre-Deploy Command*).
Para una instancia única es innecesario.

### 10.4 Render: qué esperar del tier gratuito

No es una decisión sino una limitación a tener presente, porque afecta la
demostración: el servicio **se suspende tras unos 15 minutos sin tráfico**, y la
primera request después de eso tarda alrededor de un minuto en responder
mientras el contenedor vuelve a levantar. Neon hace lo mismo con su compute.

Mitigación al demostrar: pegarle una vez al endpoint de salud unos minutos antes
de mostrar la aplicación.

Las variables de entorno se cargan en el panel de Render, **nunca dentro de la
imagen**: una imagen con secretos adentro los expone a cualquiera que la
descargue.

---

## Pendientes de decidir

- Proveedor de IA concreto y estrategia de prompt para "Analizar oportunidad"
- Estrategia de paginación (offset vs keyset)
- Alcance del interceptor de auditoría (qué entidades, qué granularidad)
- Base y aislamiento de los tests de integración
- Si se implementa ABM de rubros o alcanza con el catálogo sembrado
- Si se permiten retrocesos en el pipeline (hoy no se permiten, ver
  [Guía de estudio §5.2](GUIA-DE-ESTUDIO.md#52-el-pipeline-no-permite-retroceder))
