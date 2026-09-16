# Prompt Maestro — Administración y Control de Vigencia de Contratos de Proveedores

> **Objetivo:** usar este documento como instrucción principal para Claude / Claude Code / Codex para construir la prueba técnica Full Stack del Banco de Machala.

---

## 1. Rol que debes asumir

Actúa como un **Senior Full Stack Developer especializado en .NET 10, ASP.NET Core Web API, Angular, PostgreSQL, Supabase, Docker, Git y arquitectura limpia**.

Necesito que construyas conmigo, paso a paso, una prueba técnica Full Stack para el **Banco de Machala**.

El proyecto consiste en una aplicación web para:

# Administración y Control de Vigencia de Contratos de Proveedores

Quiero una solución profesional, limpia, mantenible, bien estructurada y suficientemente sencilla como para poder explicarla posteriormente en una entrevista técnica.

**Evita sobrearquitectura innecesaria.**

---

# 2. Stack tecnológico obligatorio

## Backend

Utilizar:

- .NET 10
- ASP.NET Core Web API
- C#
- Entity Framework Core
- PostgreSQL
- Supabase como proveedor de PostgreSQL
- JWT para autenticación
- Swagger / OpenAPI
- FluentValidation, DataAnnotations o validaciones equivalentes
- Dependency Injection nativa de .NET
- Arquitectura limpia y sencilla

Estructura sugerida:

```text
backend/
├── src/
│   ├── Contratos.Api/
│   ├── Contratos.Application/
│   ├── Contratos.Domain/
│   └── Contratos.Infrastructure/
├── tests/
│   └── Contratos.Tests/
└── Contratos.sln
```

Si consideras que para una prueba técnica de 24 horas una estructura más sencilla es mejor, puedes simplificarla, pero debes explicar la decisión.

---

# 3. Frontend

Utilizar:

- Angular
- TypeScript
- Angular Router
- Reactive Forms
- HttpClient
- Guards
- Interceptors
- Signals cuando tenga sentido
- Tailwind CSS

Prefiero una interfaz:

- moderna;
- profesional;
- limpia;
- responsive;
- sobria;
- consistente con un sistema administrativo bancario;
- fácil de entender y demostrar.

No quiero un diseño excesivamente complejo.

Estructura sugerida:

```text
frontend/
└── src/
    └── app/
        ├── core/
        │   ├── guards/
        │   ├── interceptors/
        │   ├── services/
        │   └── models/
        ├── features/
        │   ├── auth/
        │   └── contratos/
        ├── shared/
        ├── app.routes.ts
        └── app.config.ts
```

---

# 4. Identidad visual obligatoria del frontend

## 4.1 Colores

La identidad visual debe utilizar como color principal:

```text
#009845
```

Color neutro/fondo solicitado:

```text
#f0f0f0
```

> Nota: el valor proporcionado originalmente fue `#f0f0f`, que no es un color hexadecimal CSS válido porque tiene 5 dígitos. Para el proyecto se interpretará como `#f0f0f0`.

Paleta recomendada:

```css
:root {
  --color-primary: #009845;
  --color-primary-hover: #007a38;
  --color-primary-light: #e6f5ed;

  --color-background: #f0f0f0;
  --color-surface: #ffffff;

  --color-text-primary: #1f2937;
  --color-text-secondary: #6b7280;
  --color-border: #d1d5db;

  --color-success: #009845;
  --color-warning: #f59e0b;
  --color-danger: #dc2626;
  --color-disabled: #9ca3af;
}
```

El color **#009845** debe ser el color principal de marca y utilizarse en:

- botones primarios;
- encabezados o elementos destacados;
- links activos;
- iconos seleccionados;
- indicadores de navegación;
- badges positivos;
- foco de formularios;
- elementos representativos de la aplicación.

No saturar toda la pantalla de verde.

Mantener superficies claras y buen contraste.

---

## 4.2 Tipografía

Utilizar:

```text
Roboto
```

como familia tipográfica principal.

Ejemplo:

```css
html,
body {
  font-family: "Roboto", Arial, sans-serif;
}
```

Usar Roboto de manera consistente en:

- login;
- navbar;
- sidebar;
- encabezados;
- formularios;
- botones;
- tablas;
- modales;
- mensajes;
- badges.

Preferentemente cargarla de forma apropiada para producción.

Si se usa Google Fonts, documentarlo.

---

## 4.3 Favicon

Existe un archivo adjunto:

```text
favicon.ico
```

Debes integrarlo al proyecto Angular.

Ubicación recomendada según la versión/configuración utilizada:

```text
frontend/public/favicon.ico
```

o la ubicación apropiada definida por Angular.

Verifica que `index.html` contenga una referencia equivalente a:

```html
<link rel="icon" type="image/x-icon" href="favicon.ico">
```

No reemplaces el favicon por uno generado automáticamente.

Utiliza **el archivo proporcionado con este proyecto**.

---

## 4.4 Lineamientos visuales

La aplicación debe parecer un sistema corporativo real.

### Layout

Preferir:

```text
┌────────────────────────────────────────────────────┐
│ Navbar                                              │
├───────────────┬────────────────────────────────────┤
│ Sidebar       │ Contenido                           │
│               │                                    │
│ Dashboard     │                                    │
│ Contratos     │                                    │
│ Nuevo         │                                    │
│               │                                    │
└───────────────┴────────────────────────────────────┘
```

El sidebar puede omitirse si una navbar superior sencilla ofrece mejor experiencia para una prueba técnica.

Priorizar simplicidad y presentación profesional.

### Navbar

Mostrar:

- nombre del sistema;
- usuario autenticado;
- acceso a contratos;
- opcionalmente dashboard;
- botón de cerrar sesión.

### Cards

Usar cards limpias:

```css
background: #ffffff;
border-radius: 8px;
border: 1px solid #e5e7eb;
```

Evitar sombras exageradas.

### Botón primario

```css
background: #009845;
color: #ffffff;
```

Hover:

```css
background: #007a38;
```

### Inputs

Utilizar bordes neutros.

En `focus`:

```css
border-color: #009845;
```

y un `box-shadow` sutil.

### Tabla

Debe ser:

- clara;
- legible;
- responsive;
- con encabezados diferenciados;
- filas con hover sutil;
- botones de acción consistentes.

### Estados de contratos

Usar badges con texto e indicador visual.

```text
Activo      -> verde
Por vencer  -> amarillo/naranja
Vencido     -> rojo
Inactivo    -> gris
```

No depender exclusivamente del color.

Siempre mostrar el texto del estado.

---

# 5. Base de datos

Utilizar:

**Supabase PostgreSQL**

Usar Entity Framework Core para acceder a PostgreSQL.

La conexión debe realizarse mediante variables de entorno.

Nunca colocar claves, contraseñas o connection strings reales directamente en Git.

Crear/configurar cuando corresponda:

```text
appsettings.json
appsettings.Development.json
.env.example
```

Utilizar migraciones de Entity Framework Core.

---

# 6. Integración MCP de Supabase

Tengo o voy a configurar un servidor MCP de Supabase para Claude.

Cuando tengas acceso mediante MCP:

1. inspecciona primero el proyecto Supabase existente;
2. no elimines recursos existentes sin autorización;
3. revisa las tablas existentes;
4. revisa constraints;
5. revisa índices;
6. revisa relaciones;
7. revisa políticas existentes;
8. crea solamente los objetos requeridos para esta aplicación;
9. utiliza SQL compatible con PostgreSQL;
10. documenta cualquier modificación realizada.

Si existe acceso mediante MCP, úsalo como apoyo para inspeccionar Supabase, pero mantén también las migraciones de Entity Framework Core dentro del repositorio para que la estructura de base de datos quede versionada.

El MCP es una herramienta de desarrollo para Claude/Codex y **no debe convertirse en una dependencia en runtime de la aplicación**.

Arquitectura esperada:

```text
Angular
   │
   │ REST / HTTPS
   ▼
ASP.NET Core .NET 10
   │
   ├── Entity Framework Core ──────► Supabase PostgreSQL
   │
   └── File Storage Service ───────► Supabase Storage
```

Evitar que Angular consulte directamente las tablas de Supabase para las operaciones principales del sistema.

El flujo principal debe ser:

```text
Angular -> .NET API -> Supabase PostgreSQL
```

---

# 7. Objetivo del proyecto

La célula de Cartera del Banco de Machala necesita una aplicación web ligera para administrar y controlar la vigencia de contratos de proveedores.

El sistema debe permitir:

- autenticarse;
- registrar contratos;
- adjuntar documentos;
- consultar contratos;
- filtrarlos;
- identificar contratos próximos a vencer;
- identificar contratos vencidos;
- visualizar o descargar el documento asociado.

---

# 8. Autenticación

Implementar un login sencillo.

Endpoint obligatorio:

```http
POST /api/auth/login
```

Request sugerido:

```json
{
  "username": "admin",
  "password": "Admin123*"
}
```

Response sugerido:

```json
{
  "token": "...",
  "expiresAt": "...",
  "user": {
    "username": "admin"
  }
}
```

Puede existir un usuario inicial configurable para la prueba técnica.

Preferiblemente no guardar una contraseña en texto plano.

Si usamos base de datos para usuarios, almacenar hash de contraseña.

Implementar:

- JWT Bearer Authentication;
- expiración del token;
- `[Authorize]` en endpoints protegidos.

En Angular:

- AuthService;
- login;
- almacenamiento controlado del token;
- AuthGuard;
- HTTP interceptor;
- logout.

No almacenar información sensible innecesaria en el navegador.

---

# 9. Pantalla de login

El login debe ser visualmente profesional.

Preferir:

- fondo `#f0f0f0`;
- card blanca centrada;
- branding con `#009845`;
- tipografía Roboto;
- formulario compacto;
- validaciones visibles;
- botón principal verde;
- loader durante autenticación;
- manejo claro de credenciales incorrectas.

Diseño orientativo:

```text
┌───────────────────────────────────────┐
│                                       │
│        Administración de Contratos    │
│                                       │
│     ┌───────────────────────────┐     │
│     │ Usuario                   │     │
│     │ [_______________________] │     │
│     │                           │     │
│     │ Contraseña                │     │
│     │ [_______________________] │     │
│     │                           │     │
│     │   [ Iniciar sesión ]      │     │
│     └───────────────────────────┘     │
│                                       │
└───────────────────────────────────────┘
```

---

# 10. Entidad Contrato

Crear una entidad principal:

```text
Contrato
```

Campos mínimos:

```text
Id
NombreProveedor
MontoContrato
FechaInicio
FechaVencimiento
Estado
Descripcion
ArchivoNombre
ArchivoRuta o ArchivoUrl
ArchivoContentType
FechaCreacion
FechaActualizacion
```

Tipos sugeridos:

```csharp
Guid Id
string NombreProveedor
decimal MontoContrato
DateOnly/DateTime FechaInicio
DateOnly/DateTime FechaVencimiento
ContratoEstado Estado
string Descripcion
```

Para dinero usar:

```text
decimal
```

Nunca `float` ni `double`.

Configurar precisión adecuada en PostgreSQL, por ejemplo:

```text
numeric(18,2)
```

---

# 11. Estados

El sistema manejará:

```text
Activo
PorVencer
Vencido
Inactivo
```

El requerimiento original menciona:

```text
Vendido/Vencido
```

Considéralo como un probable error tipográfico.

Implementar:

```text
Vencido
```

y documentar esta decisión en el README final del proyecto.

---

# 12. Reglas para determinar estado

No depender exclusivamente de cambios manuales.

### Vencido

```text
FechaVencimiento < fecha actual
```

### Por vencer

```text
FechaVencimiento >= fecha actual
y
FechaVencimiento <= fecha actual + 30 días
```

### Activo

```text
FechaInicio <= fecha actual
y
FechaVencimiento > fecha actual + 30 días
```

### Inactivo

Debe poder ser establecido explícitamente por negocio.

Centraliza esta lógica.

No dupliques reglas de negocio en frontend y backend.

**El backend es la fuente de verdad.**

---

# 13. Validaciones

Al crear un contrato:

- NombreProveedor obligatorio.
- NombreProveedor con longitud razonable.
- MontoContrato obligatorio.
- MontoContrato mayor que 0.
- FechaInicio obligatoria.
- FechaVencimiento obligatoria.
- FechaVencimiento mayor o igual a FechaInicio.
- Descripción obligatoria.
- Archivo obligatorio.
- Solo aceptar extensiones configuradas.
- Validar MIME type.
- Limitar tamaño máximo del archivo.

Inicialmente aceptar:

```text
.pdf
.doc
.docx
```

Tamaño máximo sugerido:

```text
10 MB
```

La validación importante debe hacerse también en Backend.

Nunca confiar exclusivamente en Angular.

---

# 14. Carga de archivos

Endpoint:

```http
POST /api/contratos
```

Debe utilizar:

```text
multipart/form-data
```

Ejemplo conceptual:

```text
nombreProveedor
montoContrato
fechaInicio
fechaVencimiento
descripcion
archivo
```

Debe permitir subir al menos un archivo por contrato.

La opción preferida es:

```text
Supabase Storage
```

Diseñar una abstracción:

```csharp
IFileStorageService
```

y una implementación:

```text
SupabaseFileStorageService
```

Si por limitaciones del entorno se necesita almacenamiento local temporal, implementar:

```text
LocalFileStorageService
```

sin acoplar el dominio al proveedor concreto.

No almacenar archivos binarios grandes directamente en PostgreSQL salvo justificación técnica clara.

---

# 15. API REST

## Login

```http
POST /api/auth/login
```

## Listar contratos

```http
GET /api/contratos
```

Soportar filtros por query params.

Ejemplos:

```http
GET /api/contratos?proveedor=Microsoft
```

```http
GET /api/contratos?estado=Activo
```

```http
GET /api/contratos?fechaInicioDesde=2026-01-01
```

```http
GET /api/contratos?fechaVencimientoHasta=2026-12-31
```

Permitir combinaciones:

```http
GET /api/contratos?proveedor=software&estado=PorVencer&fechaVencimientoHasta=2026-12-31
```

Agregar paginación:

```text
page
pageSize
```

Ejemplo:

```http
GET /api/contratos?page=1&pageSize=10
```

Response sugerido:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 10,
  "totalItems": 50,
  "totalPages": 5
}
```

## Crear contrato

```http
POST /api/contratos
Content-Type: multipart/form-data
```

## Obtener contrato

```http
GET /api/contratos/{id}
```

## Archivo

```http
GET /api/contratos/{id}/archivo
```

Debe permitir:

- visualizar PDF;
- descargar archivo;
- devolver correctamente Content-Type;
- manejar archivo inexistente.

Endpoints opcionales:

```http
PUT /api/contratos/{id}
PATCH /api/contratos/{id}/estado
```

No implementar `DELETE` inicialmente salvo que sea necesario.

Si se implementa eliminación, preferir eliminación lógica.

---

# 16. Filtros del frontend

Crear pantalla:

```text
Contratos
```

Filtros:

- proveedor;
- estado;
- fecha inicio desde;
- fecha inicio hasta;
- vencimiento desde;
- vencimiento hasta;
- botón limpiar filtros.

La búsqueda por proveedor debe aceptar coincidencias parciales.

Preferentemente utilizar debounce.

---

# 17. Tabla de contratos

Mostrar:

```text
Proveedor
Monto
Fecha inicio
Fecha vencimiento
Estado
Descripción
Documento
Acciones
```

Utilizar formato monetario.

Ejemplo:

```text
$12,500.00
```

Indicadores visuales:

```text
Activo      -> verde
Por vencer  -> amarillo/naranja
Vencido     -> rojo
Inactivo    -> gris
```

No depender exclusivamente del color.

---

# 18. Dashboard

Si el tiempo lo permite, crear un pequeño dashboard con:

```text
Total contratos
Activos
Por vencer
Vencidos
Inactivos
```

Usar cards simples y profesionales.

No convertirlo en un desarrollo demasiado grande.

---

# 19. UX

Crear:

- navbar;
- nombre del sistema;
- usuario autenticado;
- botón cerrar sesión;
- loader;
- mensajes de éxito;
- mensajes de error;
- estados vacíos;
- confirmaciones cuando corresponda;
- skeleton/loading state cuando tenga sentido;
- diseño responsive.

Debe funcionar correctamente en:

- desktop;
- tablet;
- móvil.

---

# 20. Manejo de errores

Implementar en Backend un manejo centralizado de excepciones.

Utilizar:

- ProblemDetails;
- códigos HTTP apropiados.

Ejemplos:

```text
200 OK
201 Created
400 Bad Request
401 Unauthorized
404 Not Found
409 Conflict
500 Internal Server Error
```

No devolver stack traces en producción.

---

# 21. Logging

Utilizar:

```text
ILogger<T>
```

Registrar:

- errores;
- creación de contratos;
- problemas al subir archivos;
- intentos de autenticación fallidos sin registrar contraseñas.

No registrar:

- passwords;
- JWT completos;
- secrets;
- connection strings.

---

# 22. Seguridad

Implementar como mínimo:

- JWT;
- Authorization;
- CORS configurado correctamente;
- validación de entrada;
- validación de archivos;
- límites de tamaño;
- secrets mediante variables de entorno;
- protección contra path traversal;
- nombres seguros para archivos.

Nunca utilizar directamente el nombre original del archivo como ruta física.

Generar identificadores únicos.

---

# 23. Entity Framework Core

Utilizar:

```text
DbContext
DbSet<Contrato>
```

Configurar:

- constraints;
- precisión decimal;
- índices;
- longitudes máximas.

Crear índices útiles para:

```text
NombreProveedor
FechaInicio
FechaVencimiento
Estado
```

Crear migraciones.

Ejemplo:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

# 24. Supabase

Usar PostgreSQL alojado en Supabase.

Configurar la conexión mediante:

```text
ConnectionStrings__DefaultConnection
```

o variable equivalente.

Si el entorno requiere SSL, configurarlo correctamente.

No desactivar validaciones SSL de forma insegura.

---

# 25. Angular

Crear como mínimo estas rutas:

```text
/login
/contratos
/contratos/nuevo
/contratos/:id
```

Opcional:

```text
/dashboard
```

Proteger las rutas mediante:

```text
AuthGuard
```

---

# 26. Reactive Forms

Crear el formulario con:

```text
FormGroup
FormControl
Validators
```

Validar y mostrar mensajes claros.

Ejemplos:

```text
El nombre del proveedor es obligatorio.
El monto debe ser mayor que 0.
La fecha de vencimiento no puede ser anterior a la fecha de inicio.
Debe seleccionar un documento.
```

---

# 27. Modelos TypeScript

Crear interfaces estrictas.

Ejemplo:

```typescript
export interface Contrato {
  id: string;
  nombreProveedor: string;
  montoContrato: number;
  fechaInicio: string;
  fechaVencimiento: string;
  estado: ContratoEstado;
  descripcion: string;
  archivoNombre: string;
}
```

Evitar:

```typescript
any
```

salvo caso justificado.

---

# 28. Servicios Angular

Crear:

```text
AuthService
ContratosService
```

Separar correctamente responsabilidades.

El componente no debe contener toda la lógica HTTP.

---

# 29. Interceptor

Crear interceptor JWT que agregue:

```http
Authorization: Bearer TOKEN
```

a peticiones protegidas.

Si la API devuelve:

```text
401
```

cerrar la sesión de forma apropiada o redirigir al login.

---

# 30. Configuración por entorno

Frontend:

Utilizar la estrategia recomendada por la versión actual de Angular.

Backend:

```text
appsettings.json
appsettings.Development.json
Variables de entorno
```

Nunca hardcodear:

```text
localhost
password
Supabase keys
JWT secret
```

dentro del código productivo.

---

# 31. Swagger

Swagger debe permitir probar:

```text
Login
GET contratos
POST contrato
GET archivo
```

Configurar autenticación Bearer para poder colocar JWT desde Swagger.

---

# 32. Testing

Crear algunas pruebas representativas.

### Test 1

Un contrato cuyo vencimiento es dentro de 10 días:

```text
PorVencer
```

### Test 2

Un contrato cuya fecha vencimiento ya pasó:

```text
Vencido
```

### Test 3

Monto:

```text
0
```

debe ser inválido.

### Test 4

Fecha vencimiento anterior a inicio:

```text
inválido
```

Si es viable, agregar pruebas Angular básicas.

Prioriza tests sobre reglas de negocio.

---

# 33. Docker

Crear:

```text
Dockerfile backend
Dockerfile frontend
docker-compose.yml
```

Si utilizamos Supabase remoto, no es obligatorio levantar PostgreSQL dentro de Docker Compose.

Frontend y Backend deben poder levantarse con:

```bash
docker compose up --build
```

Documentar variables necesarias.

---

# 34. Git

Utilizar Conventional Commits.

Ejemplos:

```text
chore: initialize dotnet solution
feat(auth): implement jwt authentication
feat(contracts): create contract domain model
feat(contracts): add contract creation endpoint
feat(files): implement contract file upload
feat(frontend): create authentication module
feat(frontend): implement contracts listing
feat(frontend): add contract filters
style(frontend): apply corporate visual identity
test(contracts): add contract status tests
docs: add project setup instructions
chore: add docker configuration
```

No hacer un único commit enorme.

Durante el desarrollo, indicar cuándo sería un buen momento para hacer commit.

---

# 35. README final del proyecto

Crear posteriormente un README.md profesional del proyecto implementado que incluya:

```text
# Administración de Contratos

Descripción
Arquitectura
Stack tecnológico
Requisitos
Configuración
Variables de entorno
Base de datos
Migraciones
Cómo ejecutar backend
Cómo ejecutar frontend
Cómo usar Docker
Credenciales demo
Endpoints principales
Decisiones técnicas
Suposiciones
Capturas de pantalla
Mejoras futuras
```

Documentar también:

```text
Vendido/Vencido -> Vencido
```

---

# 36. Variables de entorno

Crear:

```text
.env.example
```

sin secretos reales.

Ejemplo:

```env
DATABASE_CONNECTION_STRING=

JWT_SECRET=
JWT_ISSUER=
JWT_AUDIENCE=

SUPABASE_URL=
SUPABASE_SERVICE_ROLE_KEY=
SUPABASE_STORAGE_BUCKET=contracts

FRONTEND_URL=http://localhost:4200
```

No subir:

```text
.env
```

a Git.

Agregarlo a:

```text
.gitignore
```

---

# 37. Datos de demostración

Crear seed inicial con contratos que permitan demostrar todos los estados.

Ejemplo:

```text
Proveedor Alpha
Activo

Proveedor Beta
Por vencer

Proveedor Gamma
Vencido

Proveedor Delta
Inactivo
```

Usar datos ficticios.

---

# 38. Criterios de calidad

Priorizar:

1. Que funcione.
2. Código fácil de explicar.
3. Separación correcta de responsabilidades.
4. Buen diseño de API.
5. Validaciones.
6. Manejo correcto de errores.
7. Seguridad básica.
8. Código limpio.
9. Git claro.
10. README completo.
11. Fácil ejecución por el evaluador.
12. UI profesional y consistente.

Evitar:

- microservicios;
- CQRS excesivo;
- MediatR si no aporta valor;
- event sourcing;
- abstracciones innecesarias;
- patrones utilizados únicamente para demostrar conocimiento.

Es una prueba técnica, no un core bancario real.

---

# 39. Forma de trabajo

No generes todo el proyecto sin analizar el repositorio.

Orden recomendado:

### Fase 1
Analizar requerimientos y definir arquitectura.

### Fase 2
Crear solución .NET.

### Fase 3
Configurar PostgreSQL/Supabase.

### Fase 4
Crear dominio y persistencia.

### Fase 5
Implementar autenticación JWT.

### Fase 6
Implementar API de contratos.

### Fase 7
Implementar almacenamiento de archivos.

### Fase 8
Crear aplicación Angular.

### Fase 9
Aplicar identidad visual:
- `#009845`;
- `#f0f0f0`;
- Roboto;
- favicon proporcionado.

### Fase 10
Implementar login Angular.

### Fase 11
Implementar listado de contratos.

### Fase 12
Implementar filtros.

### Fase 13
Implementar formulario.

### Fase 14
Implementar visualización/descarga de documentos.

### Fase 15
Agregar tests.

### Fase 16
Docker.

### Fase 17
README.

### Fase 18
Revisión final.

---

# 40. Reglas para modificar código

Antes de modificar archivos existentes:

1. inspecciónalos;
2. entiende su propósito;
3. evita borrar código funcional;
4. realiza cambios pequeños;
5. compila después de cambios importantes;
6. ejecuta pruebas cuando corresponda;
7. corrige warnings relevantes;
8. no ocultes errores.

Cuando encuentres un error:

- explica brevemente la causa;
- corrígelo;
- vuelve a ejecutar compilación/tests.

---

# 41. Comandos

Puedes ejecutar los comandos necesarios.

Ejemplos:

```bash
dotnet --version
dotnet restore
dotnet build
dotnet test
dotnet ef migrations add
dotnet ef database update
node --version
npm --version
npm install
npm run build
ng version
ng test
docker --version
docker compose config
```

Antes de asumir una versión o comando, verifica las herramientas instaladas.

---

# 42. No inventar resultados

Nunca digas:

```text
Compila correctamente
```

si no ejecutaste realmente:

```bash
dotnet build
```

Nunca digas:

```text
Los tests pasan
```

si no ejecutaste:

```bash
dotnet test
```

Nunca digas:

```text
Angular compila
```

sin ejecutar el build correspondiente.

---

# 43. Revisión visual obligatoria

Antes de considerar terminado el frontend:

1. verifica que Roboto se aplique globalmente;
2. verifica el color principal `#009845`;
3. verifica el fondo/neutro `#f0f0f0`;
4. verifica que el favicon proporcionado aparezca en el navegador;
5. verifica contraste y legibilidad;
6. verifica responsive;
7. verifica login;
8. verifica tabla;
9. verifica formulario;
10. verifica badges de estado;
11. verifica loaders;
12. verifica mensajes de error;
13. verifica estados vacíos;
14. verifica botones primarios/secundarios;
15. evita inconsistencias de estilos entre pantallas.

---

# 44. Revisión final general

## Backend

Revisar:

- build;
- warnings;
- autenticación;
- autorización;
- validaciones;
- filtros;
- subida de archivos;
- descarga;
- manejo de errores;
- Swagger.

## Frontend

Revisar:

- build;
- login;
- guards;
- interceptor;
- formulario;
- validaciones;
- filtros;
- responsive;
- manejo de errores;
- identidad visual;
- Roboto;
- colores;
- favicon.

## Base de datos

Revisar:

- migraciones;
- constraints;
- tipos;
- índices;
- datos iniciales.

## Seguridad

Buscar accidentalmente:

```text
passwords
API keys
connection strings
JWT secrets
Supabase keys
```

dentro del repositorio.

## Git

Revisar:

```text
.gitignore
commits
README
```

## Docker

Ejecutar:

```bash
docker compose config
```

y, si el entorno lo permite:

```bash
docker compose up --build
```

---

# 45. Entrega final

Al finalizar dame un resumen con:

```text
1. Arquitectura utilizada
2. Tecnologías utilizadas
3. Funcionalidades terminadas
4. Endpoints disponibles
5. Estructura de base de datos
6. Variables de entorno necesarias
7. Cómo ejecutar localmente
8. Cómo ejecutar con Docker
9. Credenciales de prueba
10. Tests realizados
11. Comandos de validación ejecutados
12. Decisiones técnicas
13. Suposiciones realizadas
14. Limitaciones conocidas
15. Mejoras futuras
```

---

# 46. Objetivo de la prueba

El evaluador quiere comprobar conocimientos de:

- Frontend;
- Backend;
- REST API;
- persistencia;
- carga de archivos;
- autenticación;
- Git;
- DevOps.

Cada una de estas áreas debe quedar claramente visible en la solución.

La solución debe ser suficientemente profesional para destacar técnicamente, pero suficientemente sencilla para que yo pueda explicar cada decisión durante una entrevista.

---

# 47. Primera tarea para Claude / Codex

Comienza haciendo únicamente lo siguiente:

1. Inspecciona el repositorio actual.
2. Comprueba las versiones disponibles de:
   - .NET;
   - Node.js;
   - npm;
   - Angular CLI;
   - Docker.
3. Comprueba si tienes acceso al MCP de Supabase.
4. Localiza el archivo `favicon.ico` proporcionado y confirma dónde lo integrarás en Angular.
5. Propón la arquitectura definitiva.
6. Propón la estructura de carpetas.
7. Define el modelo de datos.
8. Define los endpoints.
9. Señala cualquier ambigüedad del requerimiento.
10. Confirma la identidad visual:
    - `#009845`;
    - `#f0f0f0`;
    - Roboto;
    - favicon proporcionado.
11. Crea un plan de implementación priorizado para completar la prueba técnica.
12. Indica cuál debería ser el primer commit.

No empieces todavía a crear decenas de archivos hasta terminar este análisis inicial.

Después del análisis, empieza a implementar el proyecto de forma incremental sin detenerte a pedirme confirmación para cada archivo, salvo que exista una decisión que pueda cambiar significativamente la arquitectura.
