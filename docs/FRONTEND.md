# Brief del frontend — ZOCO Tasks

Documento de traspaso para construir el frontend en su propio repositorio
(`zocotasks-front`). Contiene el contrato completo de la API que ya está
funcionando, para que el front se pueda construir sin adivinar nada.

> **Cómo usar este documento:** copiarlo entero como contexto inicial al
> arrancar el frontend. Todo lo que dice acá está implementado y verificado
> contra la base real, salvo lo marcado explícitamente como pendiente.

---

## 1. Qué es la aplicación

Herramienta interna para un equipo comercial. Un vendedor registra comercios
interesados en contratar ZOCO y les hace seguimiento hasta que se aprueban o se
caen.

El embudo tiene este orden natural:

```
Nuevo → Contactado → Interesado → Documentación → Aprobado
  └───────────────────────────────────────────→ Rechazado
```

Pero **el movimiento entre estados es libre**: se puede saltear etapas,
retroceder para corregir una carga mal hecha, y reabrir un comercio ya
`Aprobado` o `Rechazado`. La única transición que el backend rechaza es la de
un estado a sí mismo (no es un cambio). `Aprobado` y `Rechazado` ya no son
terminales en el sentido de "sin salida": siguen usándose para reportar
oportunidades cerradas, pero se pueden reabrir.

Sobre cada comercio se registran **interacciones** (llamada, WhatsApp, reunión,
email, nota interna), y hay un botón **"Analizar oportunidad"** que manda los
datos a un modelo de IA y devuelve un análisis de venta.

---

## 2. Lo que el frontend tiene que resolver

Requisitos que vienen de la consigna y que se evalúan (el frontend pesa 15%):

- [ ] Listado de comercios con **buscar**, **filtrar**, **ordenar** y **paginar**
- [ ] Alta y edición con **validación reactiva** en el formulario
- [ ] Eliminar (con confirmación)
- [ ] Ficha del comercio con el detalle y sus interacciones
- [ ] Alta de interacciones desde la ficha
- [ ] Cambio de estado (el movimiento entre estados es libre, ver sección 1)
- [ ] Botón **"Analizar oportunidad"** con estado de carga y resultado
- [ ] **Manejo del 409**: mensaje claro de "otro usuario modificó este registro"
- [ ] Interceptor HTTP para errores

El punto del **409** es el más importante: es el requisito destacado de la
consigna y el backend ya lo resuelve. El front tiene que mostrarlo bien.

---

## 3. Cómo conectarse

| | |
|---|---|
| Base URL en desarrollo | `http://localhost:5279` |
| Documentación OpenAPI | `http://localhost:5279/openapi/v1.json` |
| CORS | Ya configurado, permite cualquier origen en desarrollo |
| Autenticación | **No hay.** Todos los endpoints son públicos |

**Importante:** el backend expone el header `ETag` vía CORS
(`WithExposedHeaders("ETag")`). Sin eso el navegador no dejaría leerlo. Para
acceder al header desde Angular hay que pedir la respuesta completa:

```ts
this.http.get<ComercioDetalle>(url, { observe: 'response' })
```

Con `observe: 'response'` se accede a `res.headers.get('ETag')`. Sin eso solo
llega el cuerpo y el ETag se pierde.

---

## 4. El flujo de concurrencia — leer antes de escribir el formulario

Es lo que distingue este proyecto. **Toda operación que modifica exige el header
`If-Match`.**

```
1. GET /api/comercios/5
   ← 200  ETag: "5121"          ← guardar este valor

2. El usuario edita el formulario

3. PUT /api/comercios/5
   If-Match: "5121"             ← reenviar el mismo valor
   ← 200  ETag: "5122"          ← actualizar el guardado
```

Si otro usuario grabó en el medio:

```
3. PUT /api/comercios/5
   If-Match: "5121"             ← quedó viejo
   ← 409 Conflict
```

### Los tres casos que el front tiene que contemplar

| Situación | Respuesta | Qué mostrar |
|---|---|---|
| Se olvidó el `If-Match` | **428** | Es un bug del front, no del usuario |
| El `If-Match` quedó viejo | **409** `codigo: "conflicto_de_concurrencia"` | "Otro usuario modificó este comercio mientras lo editabas." Ofrecer recargar |
| Mandar el mismo estado que ya tiene | **409** `codigo: "estado_transicion_invalida"` | Es la única transición que el backend rechaza: no es un cambio |

**Ojo con esto:** los dos últimos son 409. Hay que mirar el campo `codigo` del
cuerpo para distinguirlos, no solo el status.

El `version` también viene en el cuerpo del detalle, además del header. Se puede
usar cualquiera de los dos; el header es el estándar.

---

## 5. Contrato de la API

### 5.1 Listado

```
GET /api/comercios
```

Todos los parámetros son opcionales:

| Parámetro | Tipo | Notas |
|---|---|---|
| `busqueda` | string | Full text en español sobre nombre, contacto y notas. Con stemming: "cobrar" encuentra "Cobra" |
| `estado` | string | `Nuevo` · `Contactado` · `Interesado` · `Documentacion` · `Aprobado` · `Rechazado` |
| `rubroId` | int | |
| `ordenarPor` | string | `nombre` · `estado` · `rubro` · `contacto`. Por defecto ordena por fecha de creación |
| `descendente` | bool | Por defecto `true` |
| `pagina` | int | Arranca en 1. Un valor inválido se corrige a 1, no da error |
| `tamanoPagina` | int | Por defecto 20, **máximo 100** |

```jsonc
// 200 OK
{
  "items": [
    {
      "id": 1,
      "nombreComercial": "Parrilla Don Zoco",
      "cuit": "20123456786",          // 11 dígitos, sin guiones
      "nombreContacto": "Juan Perez",
      "telefono": "+54 351 555-1234",  // puede ser null
      "email": "juan@mail.com",        // puede ser null
      "rubroId": 1,
      "rubro": "Gastronomía",
      "estado": "Contactado",
      "estadoNombre": "Contactado",
      "fechaCreacion": "2026-08-08T16:21:49.802Z",
      "cantidadInteracciones": 2
    }
  ],
  "total": 3,           // total de resultados, no de la página
  "pagina": 1,
  "tamanoPagina": 20,
  "totalPaginas": 1,
  "hayAnterior": false,
  "haySiguiente": false
}
```

### 5.2 Detalle

```
GET /api/comercios/{id}
```

Devuelve `ETag` en los headers.

```jsonc
// 200 OK   |   ETag: "5121"
{
  "id": 1,
  "nombreComercial": "Parrilla Don Zoco",
  "cuit": "20123456786",
  "nombreContacto": "Juan Perez",
  "telefono": "+54 351 555-1234",
  "email": "juan@mail.com",
  "rubroId": 1,
  "rubro": "Gastronomía",
  "estado": "Contactado",
  "estadoNombre": "Contactado",
  "transicionesPosibles": ["Nuevo", "Interesado", "Documentacion", "Aprobado", "Rechazado"],
  "notas": "Dos sucursales. Problemas de conciliación.",
  "fechaCreacion": "2026-08-08T16:21:49.802Z",
  "fechaActualizacion": null,
  "version": 5121,
  "interacciones": [ /* ver 5.6 */ ]
}
```

> `transicionesPosibles` trae **todos los estados menos el actual** — el
> movimiento es libre, no solo hacia el siguiente paso del embudo. Igual
> conviene armar el selector de estado con este campo y no con una lista
> hardcodeada: si el backend algún día vuelve a restringir el pipeline, el
> frontend se adapta sin cambios.

### 5.3 Crear

```
POST /api/comercios
```

```jsonc
{
  "nombreComercial": "Parrilla Don Zoco",  // obligatorio, máx 150
  "cuit": "20-12345678-6",                 // obligatorio, acepta con o sin guiones
  "nombreContacto": "Juan Perez",          // obligatorio, máx 120
  "telefono": "+54 351 555-1234",          // opcional, máx 30
  "email": "juan@mail.com",                // opcional, máx 150
  "rubroId": 1,                            // obligatorio
  "notas": "..."                           // opcional, máx 4000
}
```

Respuestas: `201` con header `Location` y `ETag` · `400` formato inválido ·
`422` CUIT repetido o rubro inexistente.

El comercio se crea siempre en estado `Nuevo`.

### 5.4 Actualizar

```
PUT /api/comercios/{id}
If-Match: "5121"        ← OBLIGATORIO
```

Mismo cuerpo que el alta. **El estado no se manda acá** — tiene su propio
endpoint.

Respuestas: `200` con el ETag nuevo · `400` · `404` · `409` · `422` · `428`.

### 5.5 Cambiar estado

```
PATCH /api/comercios/{id}/estado
If-Match: "5121"        ← OBLIGATORIO
```

```json
{ "nuevoEstado": "Interesado" }
```

Respuestas: `200` con el detalle y las nuevas `transicionesPosibles` ·
`409` si se manda el mismo estado que ya tiene el comercio · `428`.

### 5.6 Interacciones

```
GET    /api/comercios/{comercioId}/interacciones
POST   /api/comercios/{comercioId}/interacciones
DELETE /api/comercios/{comercioId}/interacciones/{interaccionId}
```

Las interacciones **no** usan `If-Match`: se agregan y se borran, no se editan.

```jsonc
// POST — cuerpo
{
  "tipo": "Llamada",                    // Llamada|WhatsApp|Reunion|Email|NotaInterna
  "fecha": "2026-08-01T14:30:00Z",      // opcional; si falta se usa el momento actual
  "detalle": "Consultó por conciliación automática."   // obligatorio, máx 2000
}

// Respuesta de la lista — ordenada de más reciente a más vieja
[
  {
    "id": 2,
    "comercioId": 1,
    "tipo": "WhatsApp",
    "tipoNombre": "WhatsApp",
    "fecha": "2026-08-08T16:36:06.277Z",
    "detalle": "Pidió cotización por escrito.",
    "fechaCreacion": "2026-08-08T16:36:06.041Z"
  }
]
```

**La fecha no puede ser futura** (con un día de margen por husos horarios).

### 5.7 Eliminar

```
DELETE /api/comercios/{id}     → 204
```

Es baja lógica: el comercio desaparece de las consultas pero sus interacciones
se conservan. Después de esto, el `GET` del mismo id devuelve `404`.

### 5.8 Catálogos — para llenar los combos

```
GET /api/catalogos/estados
GET /api/catalogos/rubros
GET /api/catalogos/tipos-interaccion
```

```jsonc
// estados
[{ "id": 1, "codigo": "Nuevo", "nombre": "Nuevo", "orden": 1, "esFinal": false }, ...]

// rubros y tipos
[{ "id": 1, "nombre": "Gastronomía" }, ...]
```

**No hardcodear estas listas en el frontend.** Si se agrega un rubro en la base,
tiene que aparecer solo. Conviene cargarlas una vez al iniciar y cachearlas en
memoria.

### 5.9 Salud

```
GET /api/health       → { "estado": "ok", "fecha": "..." }        no toca la base
GET /api/health/db    → verifica la conexión y las migraciones
```

### 5.10 Analizar oportunidad — ⚠️ PENDIENTE EN EL BACKEND

Todavía no está implementado, pero **este es el contrato comprometido**. Se
puede construir la pantalla contra este shape.

```
POST /api/comercios/{id}/analisis
```

```jsonc
// 200 OK
{
  "nivelInteres": "Alto",              // Indeterminado|Bajo|Medio|Alto
  "resumen": "Comercio gastronómico con dos sucursales...",
  "proximoPaso": "Mostrar solución POS + QR y coordinar demo.",
  "preguntasSugeridas": [
    "¿Qué volumen mensual manejan entre las dos sucursales?",
    "¿Cuántas cajas tienen por local?",
    "¿Qué documentación les falta para avanzar?"
  ],
  "datosFaltantes": ["Volumen mensual aproximado", "Cantidad de puntos de cobro"],
  "esDegradado": false,
  "modeloUtilizado": "gpt-4o-mini",
  "fechaGeneracion": "2026-08-08T18:00:00Z"
}
```

Dos cosas a contemplar en la interfaz:

- **Tarda entre 2 y 5 segundos.** Hace falta un estado de carga visible, no un
  botón que parezca colgado.
- **`esDegradado: true`** significa que el proveedor de IA falló. En ese caso
  `nivelInteres` viene `Indeterminado` y hay que mostrar un aviso en lugar de
  presentarlo como un análisis válido. **No es un error**: la respuesta llega
  con 200 y el sistema sigue funcionando.

El resultado **no se persiste**: cada clic vuelve a llamar al modelo, y el texto
puede variar entre llamadas aunque el comercio no haya cambiado.

---

## 6. Formato de errores

Todos los errores usan **ProblemDetails** (RFC 7807), con
`Content-Type: application/problem+json`.

### 400 — formato inválido

Trae el detalle campo por campo. **Usarlo para marcar los campos en el
formulario**, no mostrar un cartel genérico.

```jsonc
{
  "title": "Hay datos invalidos.",
  "status": 400,
  "instance": "/api/comercios",
  "errors": {
    "NombreComercial": ["El nombre comercial es obligatorio."],
    "Cuit": ["El CUIT no es valido: no pasa la verificacion por modulo 11."],
    "Email": ["El email no tiene un formato valido."]
  }
}
```

Las claves de `errors` son los nombres de propiedad en **PascalCase**.

### El resto

```jsonc
{
  "title": "El registro fue modificado por otro usuario",
  "status": 409,
  "detail": "Otro usuario modifico este comercio mientras lo estabas editando...",
  "instance": "/api/comercios/1",
  "codigo": "conflicto_de_concurrencia"    // ← discriminar por acá, no por el texto
}
```

| Status | `codigo` | Significado |
|---|---|---|
| 404 | `entidad_no_encontrada` | No existe o fue dado de baja |
| 409 | `estado_transicion_invalida` | Solo cuando se manda el mismo estado actual. Trae `estadoActual` y `estadoSolicitado` |
| 409 | `conflicto_de_concurrencia` | Recargar y reintentar |
| 422 | `regla_de_negocio` | CUIT repetido, rubro dado de baja |
| 428 | `precondicion_requerida` | Falta `If-Match` — bug del front |
| 500 | `error_interno` | Sin detalle en producción |

---

## 7. Valores de los enums

Viajan como **texto**, no como número.

```ts
type EstadoComercio =
  | 'Nuevo' | 'Contactado' | 'Interesado'
  | 'Documentacion'    // sin tilde en el valor; el nombre para mostrar sí la lleva
  | 'Aprobado' | 'Rechazado';

type TipoInteraccion =
  | 'Llamada' | 'WhatsApp' | 'Reunion' | 'Email' | 'NotaInterna';

type NivelInteres = 'Indeterminado' | 'Bajo' | 'Medio' | 'Alto';
```

Ojo con la diferencia entre el **valor** y el **nombre para mostrar**: el estado
4 es `"Documentacion"` como valor y `"Documentación"` como texto. Para la
interfaz usar siempre `estadoNombre` o el `nombre` del catálogo.

---

## 8. Sugerencias de implementación

**Interceptor HTTP.** Un solo lugar que atrape los errores y los traduzca a
notificaciones. Es donde conviene manejar el 409, para no repetir la lógica en
cada pantalla.

**Servicio con el ETag adentro.** Que el resto del código no tenga que acordarse
de guardar y reenviar el token — si algún camino se olvida, aparece un 428.

**El combo de estado se arma con `transicionesPosibles`.** Hoy trae todos los
estados menos el actual (el movimiento es libre), pero conviene usar el campo
igual: si el backend restringe el pipeline en el futuro, el frontend no
necesita cambios.

**Debounce en el buscador.** 300 ms alcanza; sin eso se dispara una consulta por
tecla.

**El listado no trae las notas.** Si se quieren mostrar, hay que ir al detalle.
Es deliberado: el listado mueve menos datos.

---

## 9. Datos útiles para probar

**CUIT válidos** (pasan la verificación por módulo 11):

```
20-12345678-6      30-71234567-1      27-30123456-8      33-69345023-9
```

**CUIT inválido** para probar la validación: `20-12345678-9`.

**Texto para probar la búsqueda full text.** Cargar un comercio con la nota
*"Dos sucursales. Problemas de conciliación con transferencias."* y después
buscar `sucursal` o `problema`: los encuentra aunque estén en plural, porque el
índice usa el diccionario español con stemming.

**Para levantar el backend:**

```bash
cd zocotasks-backend
dotnet run --project ZocoTasks.API
# queda en http://localhost:5279
```
