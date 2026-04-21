using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Domain.Entities;
using Back.Api.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class ImportDomainRepository(AppDbContext context) : IImportDomainRepository
{
    public Task<List<ImportCursoLookup>> GetCursosAsync(CancellationToken cancellationToken = default) => context.Cursos
        .AsNoTracking()
        .Select(c => new ImportCursoLookup(c.Id, c.Nombre))
        .ToListAsync(cancellationToken);

    public Task<List<ImportProfesorLookup>> GetProfesoresAsync(CancellationToken cancellationToken = default) => context.Profesores
        .AsNoTracking()
        .Select(p => new ImportProfesorLookup(p.Id, p.Cuenta!.Correo))
        .ToListAsync(cancellationToken);

    public Task<List<ImportEstudianteLookup>> GetEstudiantesAsync(CancellationToken cancellationToken = default) => context.Estudiantes
        .AsNoTracking()
        .Select(e => new ImportEstudianteLookup(e.Id, e.Cuenta!.Correo, e.CursoId))
        .ToListAsync(cancellationToken);

    public Task<List<ImportAsignaturaLookup>> GetAsignaturasAsync(CancellationToken cancellationToken = default) => context.Asignaturas
        .AsNoTracking()
        .Select(a => new ImportAsignaturaLookup(a.Id, a.Nombre, a.CursoId))
        .ToListAsync(cancellationToken);

    public Task<List<(int EstudianteId, int AsignaturaId)>> GetMatriculasAsync(CancellationToken cancellationToken = default) => context.EstudianteAsignaturas
        .AsNoTracking()
        .Select(matricula => new ValueTuple<int, int>(matricula.EstudianteId, matricula.AsignaturaId))
        .ToListAsync(cancellationToken);

    public Task<List<ImportImparticionLookup>> GetImparticionesAsync(CancellationToken cancellationToken = default) => context.ProfesorAsignaturaCursos
        .AsNoTracking()
        .Select(imparticion => new ImportImparticionLookup(imparticion.ProfesorId, imparticion.AsignaturaId, imparticion.CursoId))
        .ToListAsync(cancellationToken);

    public Task<List<ImportTareaLookup>> GetTareasAsync(CancellationToken cancellationToken = default) => context.Tareas
        .AsNoTracking()
        .Select(t => new ImportTareaLookup(t.Id, t.Nombre, t.Trimestre, t.AsignaturaId, t.ProfesorId))
        .ToListAsync(cancellationToken);

    public Task<List<(int EstudianteId, int TareaId)>> GetNotasAsync(CancellationToken cancellationToken = default) => context.Notas
        .AsNoTracking()
        .Select(n => new ValueTuple<int, int>(n.EstudianteId, n.TareaId))
        .ToListAsync(cancellationToken);

    public async Task AddCursosAsync(IEnumerable<string> nombres, CancellationToken cancellationToken = default)
    {
        foreach (var nombre in nombres)
        {
            var existente = await context.Cursos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(c => c.Nombre == nombre, cancellationToken);

            if (existente is null)
                context.Cursos.Add(new Curso { Nombre = nombre });
            else
                existente.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsignaturasAsync(IEnumerable<(string Nombre, int CursoId)> asignaturas, CancellationToken cancellationToken = default)
    {
        foreach (var item in asignaturas)
        {
            var existente = await context.Asignaturas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(a => a.Nombre == item.Nombre && a.CursoId == item.CursoId, cancellationToken);

            if (existente is null)
                context.Asignaturas.Add(new Asignatura { Nombre = item.Nombre, CursoId = item.CursoId });
            else
                existente.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddProfesoresAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, string Apellidos, string DNI, string Telefono, string Especialidad)> profesores, CancellationToken cancellationToken = default)
    {
        foreach (var item in profesores)
        {
            var existente = await context.Profesores
                .IgnoreQueryFilters()
                .Include(p => p.Cuenta)
                .FirstOrDefaultAsync(p => p.Cuenta != null && p.Cuenta.Correo == item.Correo, cancellationToken);

            if (existente is null)
            {
                context.Profesores.Add(new Profesor
                {
                    Nombre = item.Nombre,
                    Apellidos = item.Apellidos,
                    DNI = item.DNI,
                    Telefono = item.Telefono,
                    Especialidad = item.Especialidad,
                    Cuenta = new Cuenta
                    {
                        Correo = item.Correo,
                        Contrasena = item.ContrasenaHash,
                        Rol = "profesor"
                    }
                });
            }
            else
            {
                existente.Nombre = item.Nombre;
                existente.Apellidos = item.Apellidos;
                existente.DNI = item.DNI;
                existente.Telefono = item.Telefono;
                existente.Especialidad = item.Especialidad;
                existente.IsDeleted = false;
                if (existente.Cuenta is not null)
                {
                    existente.Cuenta.Correo = item.Correo;
                    existente.Cuenta.Contrasena = item.ContrasenaHash;
                    existente.Cuenta.Rol = "profesor";
                    existente.Cuenta.IsDeleted = false;
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddEstudiantesAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId, string Apellidos, string DNI, string Telefono, DateOnly FechaNacimiento)> estudiantes, CancellationToken cancellationToken = default)
    {
        foreach (var item in estudiantes)
        {
            var existente = await context.Estudiantes
                .IgnoreQueryFilters()
                .Include(e => e.Cuenta)
                .FirstOrDefaultAsync(e => e.Cuenta != null && e.Cuenta.Correo == item.Correo, cancellationToken);

            if (existente is null)
            {
                context.Estudiantes.Add(new Estudiante
                {
                    Nombre = item.Nombre,
                    Apellidos = item.Apellidos,
                    DNI = item.DNI,
                    Telefono = item.Telefono,
                    FechaNacimiento = item.FechaNacimiento,
                    CursoId = item.CursoId,
                    Cuenta = new Cuenta
                    {
                        Correo = item.Correo,
                        Contrasena = item.ContrasenaHash,
                        Rol = "alumno"
                    }
                });
            }
            else
            {
                existente.Nombre = item.Nombre;
                existente.Apellidos = item.Apellidos;
                existente.DNI = item.DNI;
                existente.Telefono = item.Telefono;
                existente.FechaNacimiento = item.FechaNacimiento;
                existente.CursoId = item.CursoId;
                existente.IsDeleted = false;
                if (existente.Cuenta is not null)
                {
                    existente.Cuenta.Correo = item.Correo;
                    existente.Cuenta.Contrasena = item.ContrasenaHash;
                    existente.Cuenta.Rol = "alumno";
                    existente.Cuenta.IsDeleted = false;
                }
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMatriculasAsync(IEnumerable<(int EstudianteId, int AsignaturaId)> matriculas, CancellationToken cancellationToken = default)
    {
        foreach (var item in matriculas)
        {
            var existente = await context.EstudianteAsignaturas
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(matricula => matricula.EstudianteId == item.EstudianteId && matricula.AsignaturaId == item.AsignaturaId, cancellationToken);

            if (existente is null)
            {
                context.EstudianteAsignaturas.Add(new EstudianteAsignatura
                {
                    EstudianteId = item.EstudianteId,
                    AsignaturaId = item.AsignaturaId
                });
            }
            else
            {
                existente.IsDeleted = false;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddImparticionesAsync(IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)> imparticiones, CancellationToken cancellationToken = default)
    {
        foreach (var item in imparticiones)
        {
            var existente = await context.ProfesorAsignaturaCursos
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(imparticion => imparticion.ProfesorId == item.ProfesorId && imparticion.AsignaturaId == item.AsignaturaId && imparticion.CursoId == item.CursoId, cancellationToken);

            if (existente is null)
            {
                context.ProfesorAsignaturaCursos.Add(new ProfesorAsignaturaCurso
                {
                    ProfesorId = item.ProfesorId,
                    AsignaturaId = item.AsignaturaId,
                    CursoId = item.CursoId
                });
            }
            else
            {
                existente.IsDeleted = false;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTareasAsync(IEnumerable<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)> tareas, CancellationToken cancellationToken = default)
    {
        context.Tareas.AddRange(tareas.Select(tarea => new Tarea
        {
            Nombre = tarea.Nombre,
            Trimestre = tarea.Trimestre,
            AsignaturaId = tarea.AsignaturaId,
            ProfesorId = tarea.ProfesorId
        }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpsertNotasAsync(IEnumerable<(int EstudianteId, int TareaId, decimal Valor)> notas, CancellationToken cancellationToken = default)
    {
        var notasList = notas.ToList();
        if (notasList.Count == 0)
        {
            return;
        }

        var estudianteIds = notasList.Select(n => n.EstudianteId).Distinct().ToList();
        var tareaIds = notasList.Select(n => n.TareaId).Distinct().ToList();
        var existentes = await context.Notas
            .IgnoreQueryFilters()
            .Where(n => estudianteIds.Contains(n.EstudianteId) && tareaIds.Contains(n.TareaId))
            .ToListAsync(cancellationToken);
        var existentesMap = existentes.ToDictionary(n => (n.EstudianteId, n.TareaId));

        foreach (var nota in notasList)
        {
            if (existentesMap.TryGetValue((nota.EstudianteId, nota.TareaId), out var actual))
            {
                actual.Valor = nota.Valor;
                actual.IsDeleted = false;
                continue;
            }

            var nueva = new Nota { EstudianteId = nota.EstudianteId, TareaId = nota.TareaId, Valor = nota.Valor };
            context.Notas.Add(nueva);
            existentesMap[(nota.EstudianteId, nota.TareaId)] = nueva;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
