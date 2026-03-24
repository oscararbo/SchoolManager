using Back.Api.Domain.Entities;
using Back.Api.Domain.Repositories;
using Back.Api.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class ImportRepository(AppDbContext context) : IImportRepository
{
    public Task<List<ImportCursoLookup>> GetCursosAsync() => context.Cursos
        .AsNoTracking()
        .Select(c => new ImportCursoLookup(c.Id, c.Nombre))
        .ToListAsync();

    public Task<List<ImportProfesorLookup>> GetProfesoresAsync() => context.Profesores
        .AsNoTracking()
        .Select(p => new ImportProfesorLookup(p.Id, p.Correo))
        .ToListAsync();

    public Task<List<ImportEstudianteLookup>> GetEstudiantesAsync() => context.Estudiantes
        .AsNoTracking()
        .Select(e => new ImportEstudianteLookup(e.Id, e.Correo, e.CursoId))
        .ToListAsync();

    public Task<List<ImportAsignaturaLookup>> GetAsignaturasAsync() => context.Asignaturas
        .AsNoTracking()
        .Select(a => new ImportAsignaturaLookup(a.Id, a.Nombre, a.CursoId))
        .ToListAsync();

    public Task<List<(int EstudianteId, int AsignaturaId)>> GetMatriculasAsync() => context.EstudianteAsignaturas
        .AsNoTracking()
        .Select(x => new ValueTuple<int, int>(x.EstudianteId, x.AsignaturaId))
        .ToListAsync();

    public Task<List<ImportImparticionLookup>> GetImparticionesAsync() => context.ProfesorAsignaturaCursos
        .AsNoTracking()
        .Select(x => new ImportImparticionLookup(x.ProfesorId, x.AsignaturaId, x.CursoId))
        .ToListAsync();

    public async Task AddCursosAsync(IEnumerable<string> nombres)
    {
        context.Cursos.AddRange(nombres.Select(nombre => new Curso { Nombre = nombre }));
        await context.SaveChangesAsync();
    }

    public async Task AddAsignaturasAsync(IEnumerable<(string Nombre, int CursoId)> asignaturas)
    {
        context.Asignaturas.AddRange(asignaturas.Select(x => new Asignatura { Nombre = x.Nombre, CursoId = x.CursoId }));
        await context.SaveChangesAsync();
    }

    public async Task AddProfesoresAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash)> profesores)
    {
        context.Profesores.AddRange(profesores.Select(x => new Profesor
        {
            Nombre = x.Nombre,
            Correo = x.Correo,
            Contrasena = x.ContrasenaHash
        }));
        await context.SaveChangesAsync();
    }

    public async Task AddEstudiantesAsync(IEnumerable<(string Nombre, string Correo, string ContrasenaHash, int CursoId)> estudiantes)
    {
        context.Estudiantes.AddRange(estudiantes.Select(x => new Estudiante
        {
            Nombre = x.Nombre,
            Correo = x.Correo,
            Contrasena = x.ContrasenaHash,
            CursoId = x.CursoId
        }));
        await context.SaveChangesAsync();
    }

    public async Task AddMatriculasAsync(IEnumerable<(int EstudianteId, int AsignaturaId)> matriculas)
    {
        context.EstudianteAsignaturas.AddRange(matriculas.Select(x => new EstudianteAsignatura
        {
            EstudianteId = x.EstudianteId,
            AsignaturaId = x.AsignaturaId
        }));
        await context.SaveChangesAsync();
    }

    public async Task AddImparticionesAsync(IEnumerable<(int ProfesorId, int AsignaturaId, int CursoId)> imparticiones)
    {
        context.ProfesorAsignaturaCursos.AddRange(imparticiones.Select(x => new ProfesorAsignaturaCurso
        {
            ProfesorId = x.ProfesorId,
            AsignaturaId = x.AsignaturaId,
            CursoId = x.CursoId
        }));
        await context.SaveChangesAsync();
    }
}
