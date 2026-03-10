# School Manager API (Back)

Backend API en ASP.NET Core (.NET 10) para gestionar cursos, estudiantes, profesores, asignaturas y notas.

## Tecnologias

- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- SQLite

## Requisitos

- .NET SDK 10

## Ejecutar el proyecto

Desde la carpeta Back:

```bash
dotnet restore Back.Api.csproj
dotnet run --project Back.Api.csproj
```

La API queda disponible en:

- http://localhost:5014
- https://localhost:7166

## Configuracion

La configuracion principal esta en appsettings.json.

- ConnectionStrings.DefaultConnection: Data Source=school.db

## Base de datos local

El proyecto usa SQLite con esta cadena en appsettings.json:

- Data Source=school.db

La base de datos se crea automaticamente al iniciar la API si no existe.

Los archivos de base de datos (*.db, *.db-shm, *.db-wal) estan ignorados por Git en .gitignore, por lo que puedes tenerlos en local sin subirlos al repositorio.

## Estructura ampliada

```text
Back/
	Back.Api.csproj
	Back.slnx
	Program.cs
	appsettings.json
	README.md
	Properties/
		launchSettings.json
	Controllers/
		AsignaturasController.cs
		CursosController.cs
		EstudiantesController.cs
		ProfesoresController.cs
	Data/
		AppDbContext.cs
	Dtos/
		AsignarImparticionDto.cs
		CreateAsignaturaDto.cs
		CreateCursoDto.cs
		CreateEstudianteDto.cs
		CreateProfesorDto.cs
		PonerNotaDto.cs
	Models/
		Asignatura.cs
		Curso.cs
		Estudiante.cs
		EstudianteAsignatura.cs
		Nota.cs
		Profesor.cs
		ProfesorAsignaturaCurso.cs
```

## Responsabilidad por capa

- Controllers: exponen endpoints REST y validan reglas de negocio.
- Dtos: definen contratos de entrada de peticiones.
- Models: entidades del dominio academico.
- Data/AppDbContext: mapeo EF Core, claves compuestas, indices unicos y relaciones.
- Program.cs: inyeccion de dependencias, configuracion EF Core, arranque de la API.

## Endpoints principales

### Cursos

- GET /api/cursos
- GET /api/cursos/{id}
- POST /api/cursos

### Estudiantes

- GET /api/estudiantes
- GET /api/estudiantes/{id}
- POST /api/estudiantes
- POST /api/estudiantes/{id}/asignaturas/{asignaturaId}

### Profesores

- GET /api/profesores
- GET /api/profesores/{id}
- POST /api/profesores
- POST /api/profesores/{profesorId}/imparticiones
- POST /api/profesores/{profesorId}/notas

### Asignaturas

- GET /api/asignaturas
- GET /api/asignaturas/{id}
- POST /api/asignaturas

## Flujo recomendado de uso

1. Crear cursos.
2. Crear asignaturas asociadas a curso.
3. Crear profesores.
4. Crear estudiantes en su curso.
5. Matricular estudiantes en asignaturas de su curso.
6. Asignar imparticiones profesor-asignatura-curso.
7. Registrar notas.

## Reglas de negocio implementadas

- Nota entre 0 y 10.
- Un estudiante pertenece a un unico curso.
- Una asignatura pertenece a un unico curso.
- Un estudiante solo puede matricularse en asignaturas de su curso.
- No se puede duplicar una asignatura con el mismo nombre en el mismo curso.
- Una asignatura solo puede tener un profesor asignado.
- Un profesor solo puede poner nota si imparte esa asignatura al curso del estudiante.
- Un estudiante solo puede tener una nota por asignatura (se actualiza si se vuelve a registrar).

## Solucion de problemas

- Si Visual Studio Code muestra errores CS0234 o CS1061 en EntityFrameworkCore, ejecuta:

```bash
dotnet restore Back.Api.csproj
dotnet build Back.Api.csproj -c Debug
```
