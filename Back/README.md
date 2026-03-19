# Back

API REST del sistema de gestion escolar. Expone autenticacion, administracion academica, panel del profesor, panel del alumno e importacion masiva por CSV.

## Stack

- ASP.NET Core 10
- Entity Framework Core + SQLite
- JWT Bearer
- Swagger UI

## Arranque local

```bash
cd Back
dotnet restore Back.Api.csproj
dotnet run --project Back.Api.csproj
```

URL de desarrollo:

- API: `http://localhost:5014`
- Swagger UI: `http://localhost:5014/swagger`

## Configuracion

Archivo principal: `appsettings.json`.

Claves relevantes:

- `ConnectionStrings:DefaultConnection`
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- `SeedAdmin:Nombre`, `SeedAdmin:Correo`, `SeedAdmin:Contrasena`

## Administrador semilla

Al iniciar la API, `Program.cs` crea/actualiza un usuario admin con la configuracion de `SeedAdmin`.

Valores por defecto:

- nombre: `Administrador`
- correo: `admin@prueba.com`
- contrasena: `Prueba1`

## Estructura de archivos

```text
Back/
├── Back.Api.csproj
├── Back.slnx
├── Program.cs
├── appsettings.json
├── README.md
├── Properties/
│   └── launchSettings.json
├── Controllers/
│   ├── Admin/
│   │   └── ImportController.cs
│   ├── Auth/
│   │   └── AuthController.cs
│   ├── Asignaturas/
│   │   └── AsignaturasController.cs
│   ├── Cursos/
│   │   └── CursosController.cs
│   ├── Estudiantes/
│   │   └── EstudiantesController.cs
│   └── Profesores/
│       └── ProfesoresController.cs
├── Data/
│   └── AppDbContext.cs
├── Dtos/
│   ├── Input/
│   │   ├── Asignaturas/
│   │   ├── Auth/
│   │   ├── Cursos/
│   │   ├── Estudiantes/
│   │   └── Profesores/
│   └── Output/
│       ├── Asignaturas/
│       ├── Auth/
│       ├── Cursos/
│       ├── Estudiantes/
│       └── Profesores/
├── Models/
│   ├── Asignatura.cs
│   ├── Curso.cs
│   ├── Estudiante.cs
│   ├── EstudianteAsignatura.cs
│   ├── Nota.cs
│   ├── Profesor.cs
│   ├── ProfesorAsignaturaCurso.cs
│   ├── RefreshToken.cs
│   └── Tarea.cs
└── Services/
	├── Interfaces/
	│   ├── Asignaturas/
	│   ├── Auth/
	│   ├── Cursos/
	│   ├── Estudiantes/
	│   ├── Profesores/
	│   └── Security/
	└── Implementations/
		├── Asignaturas/
		├── Auth/
		├── Cursos/
		├── Estudiantes/
		├── Profesores/
		└── Security/
```

## Responsabilidad por capas

- `Controllers`: entrada HTTP y delegacion en servicios.
- `Services/Implementations`: reglas de negocio y validaciones.
- `Dtos`: contratos de entrada/salida entre API y cliente.
- `Models`: entidades persistidas.
- `Data/AppDbContext`: mapeo EF, relaciones e indices.
- `Program.cs`: DI, auth, CORS, swagger, excepciones globales.

## Endpoints clave

### Auth

- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`

### Admin

- CRUD de cursos, asignaturas, profesores y estudiantes.
- `POST /api/admin/csv/cursos`
- `POST /api/admin/csv/asignaturas`
- `POST /api/admin/csv/profesores`
- `POST /api/admin/csv/estudiantes`
- `POST /api/admin/csv/imparticiones`
- `POST /api/admin/csv/matriculas`

### Profesor

- `GET /api/profesores/{id}/panel`
- `GET /api/profesores/{profesorId}/asignaturas/{asignaturaId}/alumnos`
- `POST /api/profesores/{profesorId}/tareas`
- `POST /api/profesores/{profesorId}/notas`
- `POST /api/profesores/{profesorId}/imparticiones`

### Alumno

- `GET /api/estudiantes/{id}/panel`
- `POST /api/estudiantes/{id}/asignaturas/{asignaturaId}`

## Uso de Swagger

1. Ejecuta la API.
2. Abre `http://localhost:5014/swagger`.
3. Haz login con `POST /api/auth/login`.
4. Usa `Authorize` con `Bearer TU_TOKEN`.
5. Prueba endpoints protegidos.

## Importacion CSV

Formatos:

- cursos: `nombre`
- asignaturas: `nombre,cursoNombre`
- profesores: `nombre,correo,contrasena`
- estudiantes: `nombre,correo,contrasena,cursoNombre`
- imparticiones: `profesorCorreo,asignaturaNombre,cursoNombre`
- matriculas: `estudianteCorreo,asignaturaNombre,cursoNombre`

Comportamiento:

- valida extension `.csv`
- ignora cabecera
- informa errores por linea
- evita duplicados
- en `asignaturas`, `estudiantes`, `imparticiones` y `matriculas`, si hay errores de validacion o referencias la importacion completa se cancela
- en `profesores` y `estudiantes`, las contrasenas del CSV siempre se almacenan hasheadas

Orden recomendado:

1. cursos
2. asignaturas
3. profesores
4. estudiantes
5. imparticiones
6. matriculas

## Manejo de errores

### Errores de negocio

Mensajes claros en `400`, `404` y `403` segun regla violada.

### Errores globales

`Program.cs` transforma excepciones no controladas a `ProblemDetails` (`application/problem+json`) y las registra en logs.

### Errores de autorizacion

- `401`: `Necesitas iniciar sesion para acceder a este recurso.`
- `403`: `No tienes permisos suficientes para realizar esta accion.`

## Verificacion

```bash
cd Back
dotnet build Back.Api.csproj
```
