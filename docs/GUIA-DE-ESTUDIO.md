# Guía de estudio — defensa técnica

Preguntas probables con su respuesta. Al final,
**[lo que es difícil de defender](#5-lo-difícil-de-defender)**: los puntos
débiles reales del proyecto, dichos sin maquillaje, porque la peor situación en
una defensa es que te encuentren algo que vos no viste.

**Regla general para responder:** no digas *"usé X porque es buena práctica"*.
Decí *"usé X porque el problema era Y, y la alternativa Z tenía este costo
concreto"*. Lo que se evalúa es el criterio, no el catálogo.

---

## Índice

1. [Arquitectura](#1-arquitectura)
2. [Modelo de datos](#2-modelo-de-datos)
3. [Concurrencia](#3-concurrencia)
4. [Infraestructura](#4-infraestructura)
5. [Lo difícil de defender](#5-lo-difícil-de-defender)
6. [Las tres respuestas que hay que tener redondas](#6-las-tres-respuestas-que-hay-que-tener-redondas)

---

## 1. Arquitectura

### ¿Por qué cuatro proyectos y no uno solo?

Porque la separación en proyectos **la hace cumplir el compilador**. Si `Domain`
no referencia a `Infrastructure`, es literalmente imposible que una entidad
termine dependiendo de EF Core, ni por descuido ni por apuro. Con carpetas
dentro de un mismo assembly, la separación depende de que todos se acuerden de
respetarla, y eso dura hasta el primer viernes a las siete de la tarde.

*Si te repreguntan "¿no es sobreingeniería para un CRUD?":* en un CRUD sin
lógica de dominio, sí, y elegiría un assembly único. Acá hay una máquina de
estados con reglas de transición y un feature de IA, así que hay dominio real
que aislar.

### ¿Qué gana concretamente `Domain` sin dependencias?

Tres cosas medibles:

1. `MaquinaEstadoComercio` es una clase estática pura. El test de las
   transiciones es una llamada a función: sin base, sin contenedor de DI, sin
   mocks, milisegundos.
2. Cambiar EF Core por Dapper no toca una sola entidad.
3. Obliga a que las decisiones de persistencia vivan en la configuración de EF
   en lugar de filtrarse como atributos sobre las entidades.

### Dame un ejemplo de que esa regla te costó algo

**Este es el mejor ejemplo que tenés, usalo.** La columna `search_vector` es de
tipo `tsvector`. Su tipo en C# es `NpgsqlTsVector`, que viene del paquete de
Npgsql. Ponerla como propiedad de `Comercio` habría metido una dependencia a
Npgsql dentro de `Domain` y roto la regla.

La resolví como **shadow property**: la propiedad se declara en
`ComercioConfiguration` (en Infrastructure), la columna existe en la base, el
repositorio la consulta con `EF.Property<NpgsqlTsVector>(...)`, y la entidad
`Comercio` nunca se entera de que existe.

Muestra que la regla no era decorativa: hubo un caso donde molestó y se
respetó igual.

### ¿Por qué los DTOs están en Business y no en Domain?

Un DTO es un contrato de **aplicación**: describe lo que la API expone, que no
es lo mismo que lo que el dominio modela. Si vivieran en `Domain`, la capa más
pura terminaría conociendo la forma de los JSON, y cambiar una respuesta de la
API obligaría a tocar el dominio.

---

## 2. Modelo de datos

### ¿Por qué el estado está como enum *y* como tabla? ¿No es duplicar?

Es la pregunta más probable de todas. La respuesta:

Cada mitad aporta algo que la otra no puede dar:

| | Aporta |
|---|---|
| **Enum en C#** | Tipo fuerte, exhaustividad en `switch`, permite que la máquina de estados viva en el dominio |
| **Tabla en la base** | Integridad referencial (la base rechaza un estado inexistente), joins para reporting, y dos columnas que un enum no puede tener: `orden` y `es_final` |

Y **no hay duplicación real, porque hay una sola fuente de verdad**: el seed de
la tabla se genera recorriendo el enum y consultando `MaquinaEstadoComercio`
(ver `CatalogosSeed.Estados`). Agregar un estado es tocar el enum y nada más; la
tabla se sincroniza sola en la próxima migración.

*Si te repreguntan "¿y si querés estados configurables por el usuario?":* ahí el
enum sobra y va solo tabla. Pero entonces la máquina de estados también tendría
que ser configurable, que es otro problema.

### Entonces, ¿por qué `rubro` no tiene enum?

**Porque el criterio es el opuesto, y ese contraste es el punto.** Los estados
cambian cuando cambia el código: agregar un estado implica cambiar la máquina de
estados, así que un enum es correcto. Los rubros cambian **sin** que cambie el
código: agregar "Farmacia" no debería requerir un deploy. Por eso es tabla pura
con ABM.

El flag `activo` en lugar de borrar: si eliminás un rubro, los comercios
históricos que lo referencian quedan huérfanos. Desactivarlo lo saca de los
combos nuevos sin romper el pasado.

### ¿Para qué `historial_estado`? El estado actual ya está en `comercio`

Tres razones, y la tercera la justifica sola:

1. Cubre parte del bonus de auditoría.
2. Permite medir el embudo: cuánto tarda un comercio en pasar de Contactado a
   Interesado.
3. **Le da señal temporal al feature de IA.** Un comercio veinte días trabado en
   "Documentación" es información crítica para recomendar el próximo paso, y es
   un dato que *no existe en ningún otro lado*. Mirando solo el estado actual y
   las notas, el modelo no puede saberlo. Sin esta tabla el análisis ve texto
   plano; con ella ve una trayectoria.

**El detalle que suma:** el historial no se escribe a mano desde los servicios.
La única forma de cambiar de estado es `Comercio.CambiarEstado()`, que valida la
transición contra la máquina de estados y escribe el historial en la misma
operación. No hay camino de código que pueda mover un comercio sin dejar rastro.

### ¿Por qué guardás el análisis de IA en vez de devolverlo y listo?

Tres razones:

- **Costo.** `hash_contexto` es el SHA256 de lo que se le mandó al modelo. Si
  nadie tocó el comercio desde el último análisis, el hash coincide y se
  devuelve el guardado sin volver a pagar tokens.
- **Latencia.** Un cache hit responde en milisegundos en vez de segundos.
- **Trazabilidad.** Se puede ver cómo evolucionó el interés estimado en el
  tiempo, que es información comercial en sí misma.

Y un detalle de diseño: `es_degradado` marca las respuestas producidas cuando el
proveedor falla. **Nunca se cachean ni se presentan como análisis válido.** Si
el modelo no respondió, el sistema lo dice; no inventa un nivel de interés.

### ¿Por qué `jsonb` y no una tabla para las preguntas sugeridas?

Normalizar tiene sentido cuando vas a **consultar** por esos datos: "dame todos
los análisis que sugieren preguntar por volumen mensual". Acá son arrays de solo
lectura que se muestran completos junto al análisis y nunca se filtran ni se
joinean. Una tabla `pregunta_sugerida` con su PK, su FK y su índice sería
infraestructura sin uso.

*Condición de reversión:* en cuanto aparezca el primer requerimiento de buscar
o agregar por el contenido de esos arrays, se normaliza.

### ¿Por qué soft delete?

Un borrado físico se llevaría por cascada las interacciones y el historial —
justamente la evidencia del trabajo comercial. Borrar un comercio no debería
borrar el registro de que se lo llamó tres veces.

Se implementa con `fecha_eliminacion` nullable más `HasQueryFilter` global: los
eliminados desaparecen de **toda** consulta automáticamente, y recuperarlos
exige pedir `IgnoreQueryFilters()` de forma explícita. El default es el seguro.

### ¿Por qué `citext` y no normalizar a minúsculas en el código?

Normalizar en el código funciona hasta que alguien inserta por otro camino: un
script, una migración de datos, otro servicio. `citext` mueve la regla al **tipo
de la columna**, así que la comparación case-insensitive la garantiza el motor y
el índice único rechaza `Juan@mail.com` contra `juan@mail.com` sin que nadie
tenga que acordarse de nada.

### ¿Por qué índice GIN y no B-tree en `search_vector`?

Un B-tree indexa **un valor por fila** y sirve para comparaciones de orden. Un
`tsvector` contiene **muchos lexemas por fila**, y la consulta pregunta
"¿contiene este lexema?". GIN es un índice invertido: mapea cada lexema al
conjunto de filas que lo contienen. Es la estructura correcta para el problema;
un B-tree directamente no sirve.

### ¿Por qué el diccionario `spanish` y no `simple`?

Porque hace *stemming*: reduce las palabras a su raíz. Verificado contra la base
real — buscar `problema` encuentra "Problemas", buscar `sucursal` encuentra
"sucursales", y `de` se descarta como stopword. El vector almacenado es
`'parrill' 'problem' 'sucursal' 'transferent'`: raíces, no palabras literales.
Con `simple`, buscar "problema" no encontraría "Problemas".

---

## 3. Concurrencia

> Es el requisito destacado de la consigna. Tiene que salir redondo.

### ¿Cómo evitás que dos usuarios se pisen?

Con **concurrencia optimista** sobre `xmin`, una columna de sistema de
PostgreSQL que guarda el ID de la transacción que escribió la fila por última
vez y **cambia sola en cada UPDATE**.

El flujo es HTTP estándar:
1. El `GET` devuelve `xmin` en el header `ETag`.
2. El `PUT` exige ese valor en `If-Match`.
3. Si no coincide, EF lanza `DbUpdateConcurrencyException` y el middleware
   responde **409 Conflict** con el estado actual del registro.

### ¿Por qué optimista y no un lock?

Un bloqueo pesimista (`SELECT ... FOR UPDATE`) mantiene una transacción abierta
mientras el usuario piensa qué escribir. En HTTP eso es inaceptable: no sabés si
el usuario va a guardar en dos segundos o se fue a almorzar con el formulario
abierto. Optimista asume que el conflicto es raro, no bloquea a nadie, y lo
detecta al momento de guardar.

### ¿Por qué `xmin` y no una columna `version` propia?

Porque una columna propia **hay que acordarse de incrementarla en cada UPDATE**.
Un solo camino de código que lo olvide —un `ExecuteUpdate`, un script de
mantenimiento, un bulk update— rompe la garantía en silencio, que es el peor
tipo de rotura.

`xmin` no la incrementa nadie: la mantiene el motor. Elimina por construcción la
clase entera de bugs.

Verificado empíricamente, no solo diseñado:

```
INSERT ... RETURNING id, xmin  →  id=1, xmin=2056
UPDATE ... RETURNING id, xmin  →  id=1, xmin=2057
```

### ¿Cuál es la desventaja de `xmin`?

**Tenés que reconocerla, no esconderla.** Dos:

1. **Es específico de PostgreSQL.** Si mañana hubiera que soportar SQL Server,
   `xmin` no existe y habría que migrar a una columna propia. Lo asumo porque el
   proyecto ya está casado con Postgres a conciencia: usa `citext`, `tsvector` y
   `jsonb`. No es una dependencia nueva.
2. **`xmin` puede reciclarse.** Los transaction IDs de PostgreSQL son de 32 bits
   y dan la vuelta cada ~4 mil millones de transacciones. En teoría dos versiones
   distintas de una fila podrían tener el mismo `xmin`. En la práctica el
   `VACUUM` congela las filas viejas mucho antes, y la ventana entre que un
   usuario abre un formulario y lo guarda es de minutos.

### Un detalle técnico que suma si lo mencionás

`xmin` es una **columna de sistema**: no se puede crear en un `CREATE TABLE`.
Si EF intentara generarla en la migración, fallaría con *"column name xmin
conflicts with a system column name"*. Verifiqué el DDL generado para confirmar
que Npgsql la reconoce y la excluye. Es el tipo de cosa que solo se descubre
mirando el SQL, no confiando en que compila.

### ¿Por qué `ETag`/`If-Match` y no un campo en el body?

Porque son HTTP estándar (RFC 9110) para exactamente este problema. Cualquier
cliente genérico, proxy o herramienta de API los entiende sin documentación. Un
campo `version` en el JSON sería una convención privada que hay que explicar en
cada integración.

### ¿Por qué el 409 devuelve el registro y no solo el error?

Porque un 409 pelado deja al usuario sin salida más que recargar y perder lo que
escribió. Devolviendo la versión actual, el front puede mostrar qué cambió y
ofrecer resolver el conflicto.

---

## 4. Infraestructura

### ¿Por qué Neon y no PostgreSQL local o Docker?

Es PostgreSQL real, no emulado: `citext`, `tsvector`, `jsonb` y `xmin`
funcionan igual que en una instancia propia, y lo verifiqué. Permite que el
evaluador vea la aplicación contra una base real sin instalar nada, y la región
`sa-east-1` minimiza la latencia desde Argentina.

**Contrapartida que asumí:** Neon escala a cero cuando no hay tráfico, así que
la primera consulta tras un período de inactividad despierta el compute y tarda
más. Lo mitigo con `EnableRetryOnFailure(maxRetryCount: 5)` para que ese
arranque en frío se reintente en vez de presentarse como un error.

### Neon te da una cadena con `-pooler`. ¿Por qué no la usaste?

**Buena pregunta para lucirse.** El sufijo `-pooler` conecta a un PgBouncer en
lugar de a Postgres directo.

PgBouncer resuelve el problema de **muchos procesos efímeros** abriendo
conexiones: Lambda, Vercel, funciones serverless, donde cada invocación abre la
suya y agota el límite del servidor. Una API ASP.NET Core no tiene ese problema:
es un proceso de larga vida y **Npgsql ya poolea del lado del cliente**. Poner
PgBouncer encima agrega un salto de red y restricciones sin resolver nada nuevo.

Y las restricciones no son teóricas: el PgBouncer de Neon corre en *transaction
mode*, donde cada transacción puede caer en una conexión física distinta. Eso
rompe todo lo que dependa del estado de sesión —prepared statements, `SET`,
tablas temporales, advisory locks— y **las migraciones de EF Core usan
justamente advisory locks**. Se ve en la salida de `database update`:
*"Acquiring an exclusive lock for migration application"*.

*Condición de reversión:* si la API escalara a varias instancias, la pooled pasa
a ser la correcta para la aplicación, manteniendo la directa para migraciones.

### ¿Por qué `SSL Mode=VerifyFull` y no `Trust Server Certificate=true`?

`Trust Server Certificate=true` cifra el tráfico pero **acepta cualquier
certificado**, lo que anula la protección contra man-in-the-middle: un atacante
que intercepte la conexión presenta el suyo y el cliente lo acepta. Se usa
cuando el servidor tiene un certificado autofirmado y no queda otra.

Neon usa certificados de una CA pública real, así que se puede validar de
verdad. `VerifyFull` además valida que el hostname coincida, que es lo que
cierra el ataque por completo.

### ¿Cómo manejás los secretos?

`appsettings.json` tiene la clave de conexión **vacía a propósito**. El valor
real llega por `user-secrets` en desarrollo o por variable de entorno en CI y
deploy. Se versiona `.env.example` con las claves y sin los valores, para que
quien clona sepa qué necesita.

`user-secrets` guarda el archivo **fuera del árbol del repositorio**
(`%APPDATA%\Microsoft\UserSecrets\<id>`), así que no hay forma de commitearlo
por accidente. Y esto importa porque un secreto commiteado no se borra sacándolo
en otro commit: queda en la historia de git para siempre, y hay bots escaneando
GitHub buscando exactamente eso.

Además, la aplicación **falla en el arranque** con un mensaje explícito si falta
la cadena, en lugar de levantar bien y explotar en el primer request con un
error críptico.

### Vi un pin raro de `Microsoft.OpenApi`. ¿Qué es?

Un pin de seguridad. `dotnet restore` reportó `NU1903`:
`Microsoft.AspNetCore.OpenApi` arrastra transitivamente `Microsoft.OpenApi`
2.0.0, que tiene una vulnerabilidad de severidad alta
(GHSA-v5pm-xwqc-g5wc).

Probé tres caminos, en orden:
1. Subir el paquete padre a la última (10.0.10) — **no alcanza**, sigue
   trayendo la 2.0.0. Verificado con `dotnet list package --include-transitive`.
2. Subir a `Microsoft.OpenApi` 3.x — descartado, cambia la API de forma
   incompatible con este release de ASP.NET Core.
3. Pinear la última 2.x (2.7.5) — funciona, la advertencia desaparece.

Una referencia directa gana sobre la resolución transitiva de NuGet. Está
comentado en el `.csproj` como deuda técnica, con instrucción de removerlo
cuando ASP.NET Core actualice su dependencia: un pin sin explicación se
convierte en un misterio que nadie se anima a tocar.

---

## 5. Lo difícil de defender

> Leé esta sección dos veces. Es lo que te pueden encontrar.

### 5.1 Hay cuatro tablas que todavía no usa nadie

`usuario`, `rol`, `usuario_rol` y `audit_log` están creadas en la base y **no
hay una sola línea de código que las escriba**. Hoy son esquema muerto.

**Si preguntan:** "Están previstas para autenticación y auditoría, que son
bonus. El esquema quedó definido desde la primera migración para no tener que
partir el modelo después."

**Pero la verdad incómoda:** si al final no llegás a implementar auth ni
auditoría, **borralas antes de entregar**. Es peor que te pregunten "¿y esto
para qué está?" y tengas que decir "no llegué" a que simplemente no estén. Una
tabla vacía sin código que la use se lee como planificación fallida.

### 5.2 El pipeline no permite retroceder

Modelé las transiciones sin marcha atrás: de `Interesado` solo se puede ir a
`Documentación` o `Rechazado`, nunca volver a `Contactado`.

**Es la lectura estricta de la consigna**, que presenta el pipeline como lineal.
Pero en un CRM real los vendedores retroceden estados todo el tiempo: se cargó
mal, el comercio se enfrió, hay que rehacer documentación.

**Si preguntan:** "Es lo que dice la consigna, y prefiero una regla explícita y
testeable a una permisiva. Está centralizado en un diccionario en
`MaquinaEstadoComercio`: habilitar retrocesos es cambiar esas líneas y agregar
el caso al test." Tener identificado *dónde* se cambia es lo que muestra que fue
una decisión y no un olvido.

### 5.3 La factory de diseño agrega fricción

`ZocoDbContextFactory` permite generar migraciones sin conexión, pero EF la
prioriza sobre la configuración de la aplicación, así que **los comandos de EF
leen la variable de entorno y no `user-secrets`**. Eso obliga a exportar la
variable a mano antes de cada `database update`.

**Es un concepto extra que hay que explicar.** Está documentado en el README,
pero si te resulta difícil de justificar, la alternativa es eliminar la factory
y dejar que EF use el host de la aplicación: menos código, menos fricción, a
cambio de no poder generar migraciones sin configuración.

### 5.4 `IGenericRepository<T>` es la decisión más discutible

Todavía no está escrito. **La crítica estándar es correcta: `DbSet<T>` ya *es*
un repositorio**, y `IQueryable` ya es el patrón de especificación. Envolverlo
en una interfaz genérica suele ser una capa que agrega indirección sin agregar
capacidad.

**El argumento a favor**, si se implementa: permite testear servicios sin
levantar base, y evita repetir el CRUD de los catálogos.

**El argumento honesto en contra:** los tests de integración van contra
PostgreSQL real igual, así que el beneficio de "testear sin base" es menor de lo
que suena, y los catálogos casi no tienen CRUD.

*Recomendación:* usar repositorios **concretos** (`IComercioRepository`) donde
hay queries reales con includes, filtros, orden y paginación, y no crear el
genérico salvo que aparezca duplicación real. Es más fácil de defender "no lo
hice porque no hacía falta" que "lo hice porque se usa".

### 5.5 `EntidadBase` casi no hace nada

Solo tiene `Id`. Es una abstracción muy delgada.

**Si preguntan:** es un punto de extensión para el interceptor de auditoría, que
necesita identificar entidades con clave entera de forma genérica. Si la
auditoría no se implementa, esta clase queda difícil de justificar.

### 5.6 Once tablas para un enunciado que pedía dos

La consigna pide `Comercio` e `Interacciones`. Hay once tablas.

**El desglose defendible:**

| Tabla | Justificación |
|---|---|
| `comercio`, `interaccion` | Pedidas explícitamente |
| `estado_comercio`, `tipo_interaccion` | Lookups para integridad referencial |
| `rubro` | Pedido como campo; es tabla porque cambia sin deploy |
| `historial_estado` | Insumo del feature de IA + auditoría del embudo |
| `analisis_oportunidad` | Persistencia del feature pedido, con cache |
| `usuario`, `rol`, `usuario_rol`, `audit_log` | **Bonus — ver 5.1** |

Siete de once se defienden solas. Las otras cuatro dependen de que se
implementen los bonus.

---

## 6. Las tres respuestas que hay que tener redondas

Si solo podés memorizar tres cosas, que sean estas.

### 1. Concurrencia optimista con `xmin`

*"PostgreSQL tiene una columna de sistema, `xmin`, que guarda el ID de la
transacción que escribió la fila y cambia sola en cada UPDATE. La mapeé como
token de concurrencia. La ventaja sobre una columna `version` propia es que no
hay que acordarse de incrementarla: la mantiene el motor, así que ningún camino
de código puede saltearse la garantía. El GET la devuelve como `ETag`, el PUT la
exige en `If-Match`, y si no coincide se responde 409 con el estado actual para
que el front pueda resolver el conflicto en vez de obligar a recargar."*

### 2. Por qué el estado es enum y tabla a la vez

*"El enum da tipo fuerte y permite que la máquina de estados viva en el dominio;
la tabla da integridad referencial y las columnas `orden` y `es_final`, que un
enum no puede tener. No hay duplicación porque el seed de la tabla se genera
recorriendo el enum: hay una sola fuente de verdad. El contraste está en
`rubro`, que es tabla pura sin enum, porque los rubros cambian sin que cambie el
código y los estados no."*

### 3. Qué hace `historial_estado` que no hace `comercio.estado`

*"El estado actual te dice dónde está el comercio; el historial te dice hace
cuánto y por dónde pasó. Eso es lo que le da señal temporal al análisis de IA:
un comercio veinte días trabado en Documentación es información crítica para
recomendar el próximo paso, y no existe en ningún otro lado. Además, la única
forma de cambiar de estado es `Comercio.CambiarEstado()`, que valida la
transición y escribe el historial en la misma operación, así que no hay forma de
mover un comercio sin dejar rastro."*
