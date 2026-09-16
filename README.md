# Administración y Control de Vigencia de Contratos

Aplicación web para registrar contratos de proveedores, adjuntar su documento y controlar su vigencia (Activo, Por vencer, Vencido, Inactivo).

## Credenciales de prueba

| Usuario | Clave      |
| ------- | ---------- |
| `admin` | `admin123` |

El usuario se crea automáticamente al primer arranque con los valores `Seed__AdminUsername` y `Seed__AdminPassword` del archivo `.env`. Si la base ya tenía el usuario `admin`, se conserva su clave anterior.

## Instalación y ejecución local

### Requisitos

- [.NET SDK 10.0.401](https://dotnet.microsoft.com/download) o superior
- [Node.js](https://nodejs.org/) 22.12 o superior (con npm)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/), solo para la opción con Docker

### 1. Configuración

```bash
git clone https://github.com/JoshDiazGonzalez/AdministradordeContratos-.git
cd AdministradordeContratos-
cp .env.example .env
```

Con los valores por defecto la aplicación funciona sin más cambios en desarrollo: si `ConnectionStrings__DefaultConnection` está vacío se usa una base SQLite local y los documentos se guardan en disco. Se cargan contratos de ejemplo con los cuatro estados (`Seed__DatosDemo=true`).

Para usar Supabase, completar en `.env` la cadena del *session pooler* (`ConnectionStrings__DefaultConnection`) y, para guardar los documentos en Supabase Storage, `Storage__Provider=Supabase`, `Supabase__Url` y `Supabase__ServiceRoleKey`. Cada variable está explicada en `.env.example`. El archivo `.env` nunca se sube al repositorio.

### 2. Opción A: backend y frontend por separado

Backend (API en http://localhost:5080, Swagger en http://localhost:5080/swagger):

```bash
dotnet run --project backend/src/Contratos.Api --launch-profile http
```

Frontend (en otra terminal, aplicación en http://localhost:4200):

```bash
cd frontend
npm ci
npm start
```

### 3. Opción B: Docker

Levanta PostgreSQL, la API y el frontend. Antes, completar en `.env`:

- `Jwt__Secret`: al menos 32 caracteres aleatorios (por ejemplo `openssl rand -base64 48`).
- `POSTGRES_PASSWORD`: contraseña de la base interna (por ejemplo `openssl rand -hex 24`).

```bash
docker compose up --build
```

Aplicación en http://localhost:8080 (puerto configurable con `FRONTEND_PORT`). Las migraciones se aplican al arrancar. Para detener: `docker compose down` (añadir `-v` borra también la base y los documentos).

## Arquitectura y stack

```
Angular (SPA)  ──HTTP/JSON + JWT──▶  ASP.NET Core Web API  ──▶  PostgreSQL (Supabase)
                                             │
                                             └──────────────▶  Almacenamiento de documentos
                                                                (Supabase Storage o disco local)
```

**Backend: .NET 10 con arquitectura limpia.** Cada capa depende solo de las interiores:

- `Contratos.Domain`: entidades y reglas de negocio sin dependencias externas. El estado del contrato se calcula con las fechas (zona horaria America/Guayaquil); solo *Inactivo* se guarda.
- `Contratos.Application`: casos de uso, DTOs, validaciones (FluentValidation) e interfaces de repositorios y almacenamiento.
- `Contratos.Infrastructure`: EF Core con Npgsql, migraciones, JWT, BCrypt y las implementaciones de almacenamiento (Supabase Storage o local).
- `Contratos.Api`: controladores REST, autenticación JWT, manejo de errores con ProblemDetails y Swagger.
- `Contratos.Tests`: pruebas unitarias y de integración (xUnit).

Así la lógica de negocio se prueba sin base de datos, y cambiar de proveedor de datos o de almacenamiento no afecta al dominio.

**Frontend: Angular 21** con componentes standalone, signals, carga diferida de rutas, guards e interceptor funcional que añade el token JWT. Estilos con Tailwind CSS 4 y la identidad visual del proyecto (verde `#009845`, tipografía Roboto). Pruebas con Vitest.

**Base de datos: PostgreSQL en Supabase.** Tablas con índices para los filtros, restricciones de integridad y Row Level Security activado para que solo la API acceda a los datos. Los documentos (PDF, DOC y DOCX de hasta 10 MB) se validan por extensión, tipo y contenido real, y se guardan en un bucket privado.

**Docker:** imagen de la API sobre ASP.NET y del frontend sobre nginx, ambas con usuario sin privilegios. nginx sirve la aplicación y reenvía `/api` a la API, así que ambas comparten origen.
