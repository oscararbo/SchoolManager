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
        .Select(p => new ImportProfesorLookup(p.Id, p.Correo))
        .ToListAsync(cancellationToken);

    public Task<List<ImportEstudianteLookup>> GetEstudiantesAsync(CancellationToken cancellationToken = default) => context.Estudiantes
        .AsNoTracking()
        .Select(e => new ImportEstudianteLookup(e.Id, e.Correo, e.CursoId))
        .ToListAsync(cancellationToken);

    public Task<List<ImportAsignaturaLookup>> GetAsignaturasAsync(CancellationToken cancellationToken = default) => context.Asignaturas
        .AsNoTracking()
        .Select(a => new ImportAsignaturaLookup(a.Id, a.Nombre, a.CursoId))
        .ToListAsync(cancellationToken);

    public Task<List<(int EstudianteId, int AsignaturaId)>> GetMatriculasAsync(CancellationToken cancellationToken = default) => context.EstudianteAsignaturas
        .AsNoTracking()
        .Select(x => new ValueTuple<int, int>(x.EstudianteId, x.AsignaturaId))
        .ToListAsync(cancellationToken);

    public Task<List<ImportImparticionLookup>> GetImparticionesAsync(CancellationToken cancellationToken = default) => context.ProfesorAsignaturaCursos
        .AsNoTracking()
        .Select(x => new ImportImparticionLookup(x.ProfesorId, x.AsignaturaId, x.CursoId))
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
        context.Cursos.AddRange(nombres.Select(nombre => new Curso { Nombre = nombre }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAsignaturasAsync(IEnumerable<(string Nombre, int CursoId)> asignaturas, CancellationToken cancellationToken = default)
    {
        context.Asignaturas.AddRange(asignaturas.Select(x => new Asignatura { Nombre = x.Nombre, CursoId = x.CursoId }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddProfesoresAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash)> profesores, CancellationToken cancellationToken = default)
    {
        context.Profesores.AddRange(profesores.Select(x => new Profesor
        {
            Nombre = x.Nombre,
            Correo = x.Correo,
            Contrasena = x.ContrasenaHash
        }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddEstudiantesAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId)> estudiantes, CancellationToken cancellationToken = default)
    {
        context.Estudiantes.AddRange(estudiantes.Select(x => new Estudiante
        {
            Nombre = x.Nombre,
            Correo = x.Correo,
            Contrasena = x.ContrasenaHash,
            CursoId = x.CursoId
        }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddMatriculasAsync(IEnumerable<(int EstudianteId, int AsignaturaId)> matriculas, CancellationToken cancellationToken = default)
    {
        context.EstudianteAsignaturas.AddRange(matriculas.Select(x => new EstudianteAsignatura
        {
            EstudianteId = x.EstudianteId,
            AsignaturaId = x.AsignaturaId
        }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddImparticionesAsync(IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)> imparticiones, CancellationToken cancellationToken = default)
    {
        context.ProfesorAsignaturaCursos.AddRange(imparticiones.Select(x => new ProfesorAsignaturaCurso
        {
            ProfesorId = x.ProfesorId,
            AsignaturaId = x.AsignaturaId,
            CursoId = x.CursoId
        }));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddTareasAsync(IEnumerable<(string Nombre, int Trimestre, int AsignaturaId, int ProfesorId)> tareas, CancellationToken cancellationToken = default)
    {
        context.Tareas.AddRange(tareas.Select(x => new Tarea
        {
            Nombre = x.Nombre,
            Trimestre = x.Trimestre,
            AsignaturaId = x.AsignaturaId,
            ProfesorId = x.ProfesorId
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
            .Where(n => estudianteIds.Contains(n.EstudianteId) && tareaIds.Contains(n.TareaId))
            .ToListAsync(cancellationToken);
        var existentesMap = existentes.ToDictionary(n => (n.EstudianteId, n.TareaId));

        foreach (var nota in notasList)
        {
            if (existentesMap.TryGetValue((nota.EstudianteId, nota.TareaId), out var actual))
            {
                actual.Valor = nota.Valor;
                continue;
            }

            var nueva = new Nota { EstudianteId = nota.EstudianteId, TareaId = nota.TareaId, Valor = nota.Valor };
            context.Notas.Add(nueva);
            existentesMap[(nota.EstudianteId, nota.TareaId)] = nueva;
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
