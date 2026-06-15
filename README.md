# SchoolManager

Aplicación de gestión escolar multi‑colegio desarrollada con ASP.NET Core y Angular. El proyecto permite administrar distintos centros educativos desde una misma plataforma, separando la información de cada colegio mediante un sistema multi‑tenant y ofreciendo paneles específicos para superusuario, administradores, profesores y alumnos.

## Stack tecnológico

### Backend

- ASP.NET Core 10
- Entity Framework Core
- PostgreSQL
- MongoDB (auditoría y logs)
- JWT Bearer Authentication
- Swagger UI
- API Versioning
- Serilog

### Frontend

- Angular 21 (Standalone)
- Signals
- RxJS
- Bootstrap 5
- Bootstrap Icons
- Chart.js

### Testing

- xUnit
- Moq
- Integration Tests
- EF Core InMemory

### Infraestructura

- Docker
- Docker Compose
- GitHub Actions

---

# Estructura general

```text
SchoolManager/
│
├── Back/                      # API REST
├── Front/                     # Cliente Angular
├── Back.Tests/                # Pruebas automatizadas
├── csv-pruebas-grandes/       # Ficheros de ejemplo para importaciones
├── .github/workflows/         # CI/CD
├── postman/                   # Colecciones y recursos de pruebas
└── docker-compose.yml
```

---

# Arquitectura

El proyecto sigue una separación clara por capas para desacoplar la lógica de negocio de la infraestructura y de la interfaz de usuario.

```text
Presentation
    ↓
Application
    ↓
Domain
    ↓
Persistence
```

Además, la capa Infrastructure contiene funcionalidades transversales como seguridad, manejo de errores y auditoría.

## Presentation

Contiene los controladores REST que exponen la API.

Controladores principales:

- AuthController
- CursosController
- AsignaturasController
- ProfesoresController
- EstudiantesController
- SuperUsuarioController
- LogsController

## Application

Contiene los casos de uso, DTOs, contratos e interfaces.

Servicios organizados por dominio:

```text
Application/Services
├── Admin
├── AdminStats
├── Asignaturas
├── Audit
├── Auth
├── Cursos
├── Estudiantes
├── Imports
├── Profesores
└── SuperUsuario
```

## Domain

Contiene las entidades y reglas de negocio del sistema.

Dominios principales:

- Colegios
- Cursos
- Asignaturas
- Profesores
- Estudiantes
- Tareas
- Matrículas
- Calificaciones
- Usuarios

## Persistence

Responsable del acceso a datos.

Incluye:

- DbContext
- Repositorios
- Migraciones
- Configuración EF Core
- Acceso PostgreSQL
- Acceso MongoDB

## Infrastructure

Responsabilidades técnicas transversales:

- Seguridad JWT
- Gestión de contexto de colegio
- Manejo global de errores
- Auditoría
- Logging

---

# Gestión multi‑colegio

Una de las características principales del proyecto es su arquitectura multi‑tenant.

Cada colegio dispone de:

```text
Colegio
├── Administradores
├── Profesores
├── Estudiantes
├── Cursos
├── Asignaturas
├── Matrículas
└── Calificaciones
```

El sistema incorpora:

- Contexto de colegio.
- Aislamiento de datos entre centros.
- Superusuario global.
- Branding independiente por colegio.
- Validación automática de tenant.

Ejemplo local:

```text
http://localhost:4200/?school=default
```

---

# Roles disponibles

## Superusuario

Responsable de la administración global de la plataforma.

Funciones principales:

- Crear colegios.
- Modificar colegios.
- Configurar branding.
- Gestionar administradores.

## Administrador

Responsable de la gestión académica de un colegio.

Funciones principales:

- Gestionar cursos.
- Gestionar asignaturas.
- Gestionar profesores.
- Gestionar estudiantes.
- Gestionar matrículas.
- Gestionar imparticiones.
- Importar datos mediante CSV.
- Consultar estadísticas.

## Profesor

Funciones principales:

- Consultar asignaturas.
- Crear tareas.
- Evaluar estudiantes.
- Consultar rendimiento.

## Alumno

Funciones principales:

- Consultar notas.
- Consultar medias.
- Consultar progreso académico.

---

# Autenticación y seguridad

La autenticación se realiza mediante JWT.

Flujo simplificado:

```text
Login
 └── Access Token
        └── Refresh Token
                └── Renovación automática
```

Características:

- JWT Bearer.
- Refresh Tokens.
- Roles.
- Protección de endpoints.
- Validación de tenant.
- Gestión de sesión.

Para usuarios asociados a colegios se utiliza el header:

```http
X-School-Slug
```

Los superusuarios no requieren contexto de colegio.

---

# Funcionalidades principales

## Gestión académica

- Cursos.
- Asignaturas.
- Profesores.
- Estudiantes.
- Matrículas.
- Imparticiones.

## Evaluación

- Creación de tareas.
- Calificaciones.
- Cálculo de medias.
- Nota final.
- Seguimiento por trimestres.

## Estadísticas

El módulo AdminStats permite:

- Estadísticas por curso.
- Comparativas entre cursos.
- Indicadores académicos.
- Métricas de rendimiento.

## Importación masiva

Importación de información mediante archivos CSV.

Entidades soportadas:

- Cursos
- Asignaturas
- Profesores
- Estudiantes
- Imparticiones
- Tareas
- Matrículas
- Notas

Orden recomendado:

1. Cursos
2. Asignaturas
3. Profesores
4. Estudiantes
5. Imparticiones
6. Tareas
7. Matrículas
8. Notas

Los ejemplos incluidos pueden encontrarse en:

```text
csv-pruebas-grandes/
```

---

# Backend

## Estructura

```text
Back/
├── Application/
├── Domain/
├── Infrastructure/
├── Persistence/
├── Presentation/
├── Program.cs
├── appsettings.json
├── Dockerfile
└── ARCHITECTURE.md
```

## Convención de DTOs

Para evitar acoplamientos entre cliente y servidor se utilizan distintos tipos de DTO:

```text
RequestDto
ResponseDto
ReadModelDto
StatsDto
```

Documentación adicional:

```text
Application/Dtos/DTO_CONVENTIONS.md
```

## Endpoints destacados

### Auth

```text
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
```

### Superusuario

```text
GET    /api/superusuario/colegios
POST   /api/superusuario/colegios
PUT    /api/superusuario/colegios/{id}
DELETE /api/superusuario/colegios/{id}
```

### Administración

- Gestión de cursos.
- Gestión de asignaturas.
- Gestión de profesores.
- Gestión de estudiantes.
- Estadísticas.
- Importaciones CSV.

### Profesor

- Gestión de tareas.
- Calificaciones.
- Consulta de alumnos.

### Alumno

- Consulta de progreso.
- Consulta de asignaturas.
- Consulta de notas.

## Auditoría

El sistema incorpora auditoría mediante MongoDB.

Permite registrar:

- Operaciones relevantes.
- Eventos de sistema.
- Logs de aplicación.
- Trazabilidad de acciones.

---

# Frontend

Cliente Angular encargado de consumir la API y presentar la información al usuario.

## Estructura principal

```text
src/app
├── core
├── features
├── layouts
└── shared
```

## Core

Contiene elementos transversales.

### Guards

```text
auth.guard.ts
```

### Interceptors

```text
auth.interceptor.ts
error.interceptor.ts
```

### Servicios

```text
auth-state.service.ts
session.service.ts
tenant.service.ts
toast.service.ts
```

## Layouts

### Auth Layout

Responsable del acceso al sistema.

### Home Layout

Responsable de la navegación principal de la aplicación.

## Flujo de navegación

```text
Login
├── Superusuario
├── Administrador
├── Profesor
└── Alumno
```

Cada perfil dispone de vistas y funcionalidades específicas.

## Shared

Incluye:

- Componentes reutilizables.
- Servicios HTTP.
- Contratos.
- Tipos.
- Mappers.
- Gestión de errores.

## Servicios API

El frontend centraliza la comunicación con el backend mediante servicios especializados.

Ejemplos:

```text
school-api-auth.service
school-api-admin.service
school-api-profesor.service
school-api-alumno.service
school-api-superusuario.service
```

---

# Testing

El proyecto incluye pruebas automatizadas para distintos escenarios.

## Unit Tests

Cobertura sobre:

- Servicios.
- Repositorios.
- Controladores.
- Importaciones.

## Integration Tests

Escenarios destacados:

- Endpoints administrativos.
- Multi‑colegio.
- Disponibilidad de API.

Ejemplos:

```text
AdminEndpointsIntegrationTests
MultiSchoolIntegrationTests
ApiAvailabilityInfrastructureTests
```

---

# Docker

Arranque completo del entorno:

```bash
docker compose up --build
```

Servicios disponibles:

| Servicio | Puerto |
|-----------|----------|
| Frontend | 4200 |
| API | 5014 |
| PostgreSQL | 5432 |
| MongoDB | 27017 |

---

# Credenciales semilla

Administrador inicial:

```text
Correo: admin@prueba.com
Contraseña: Prueba1
```

Superusuario inicial:

```text
Correo: root@schoolmanager.com
Contraseña: Super123!
```

Colegio inicial:

```text
Nombre: Colegio Principal
Slug: default
```

---

# Calidad y validación

Backend:

```bash
cd Back
dotnet build Back.slnx
dotnet test
```

Frontend:

```bash
cd Front
npm install
npm run build
```

---

# Integración continua

El repositorio incluye una pipeline de GitHub Actions:

```text
.github/workflows/ci.yml
```

La pipeline ejecuta:

- Restauración de dependencias.
- Compilación.
- Validaciones.
- Ejecución de pruebas.

---

# Roadmap

La arquitectura actual permite incorporar nuevas funcionalidades sin realizar cambios importantes en la estructura base.

Posibles ampliaciones:

- Control de asistencia.
- Calendario académico.
- Notificaciones.
- Exportación Excel.
- Exportación PDF.
- Comunicación interna.
- Analítica avanzada.
- Integraciones externas.

---

# Documentación adicional

La mayor parte de las decisiones técnicas relevantes se encuentran documentadas directamente en el código y en los documentos incluidos dentro del backend, especialmente:

```text
Back/ARCHITECTURE.md
Back/Application/Dtos/DTO_CONVENTIONS.md
```

Este README pretende servir como punto de entrada para comprender la estructura general del proyecto, los módulos principales y el flujo funcional de la aplicación.
