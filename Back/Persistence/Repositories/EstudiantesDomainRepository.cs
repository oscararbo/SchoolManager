using Back.Api.Application.Abstractions.Repositories;
using Back.Api.Application.Configuration;
using Back.Api.Persistence.Context;
using Back.Api.Application.Dtos;
using Back.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Back.Api.Persistence.Repositories;

public class EstudiantesDomainRepository(AppDbContext context) : IEstudiantesDomainRepository
{
    public Task<bool> ExisteAsync(int estudianteId, CancellationToken cancellationToken = default) =>
        context.Estudiantes.AnyAsync(e => e.Id == estudianteId, cancellationToken);

    public Task<bool> CorreoDuplicadoAsync(string correo, CancellationToken cancellationToken = default) =>
        context.Cuentas.AnyAsync(c => c.Correo == correo, cancellationToken);

    public Task<bool> CorreoDuplicadoExceptAsync(string correo, int exceptEstudianteId, CancellationToken cancellationToken = default) =>
        context.Cuentas.AnyAsync(c => c.Correo == correo && (c.Estudiante == null || c.Estudiante.Id != exceptEstudianteId), cancellationToken);

    public Task<bool> CursoExisteAsync(int cursoId, CancellationToken cancellationToken = default) =>
        context.Cursos.AnyAsync(c => c.Id == cursoId, cancellationToken);

    public Task<bool> AsignaturaExisteAsync(int subjectId, CancellationToken cancellationToken = default) =>
        context.Asignaturas.AnyAsync(a => a.Id == subjectId, cancellationToken);

    public Task<bool> YaMatriculadoAsync(int estudianteId, int subjectId, CancellationToken cancellationToken = default) =>
        context.EstudianteAsignaturas.AnyAsync(x => x.EstudianteId == estudianteId && x.AsignaturaId == subjectId, cancellationToken);

    public async Task<bool> AsignaturaEsDelCursoAsync(int subjectId, int cursoId, CancellationToken cancellationToken = default)
    {
        var cursoDeLaAsignatura = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == subjectId)
            .Select(a => a.CursoId)
            .FirstOrDefaultAsync();

        return cursoDeLaAsignatura == cursoId;
    }

    public async Task<AlumnoPanelResumenDto?> GetPanelResumenAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        var student = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => new { e.Id, e.Nombre, e.CursoId, Curso = e.Curso!.Nombre })
            .FirstOrDefaultAsync();

        if (student is null) return null;

        var subjects = await context.EstudianteAsignaturas
            .AsNoTracking()
            .Where(ea => ea.EstudianteId == estudianteId)
            .Select(ea => new AlumnoMateriaResumenDto
            {
                AsignaturaId = ea.AsignaturaId,
                Asignatura = ea.Asignatura!.Nombre,
                Profesor = ea.Asignatura.ProfesorAsignaturaCursos
                    .Where(pac => pac.CursoId == student.CursoId)
                    .Select(pac => pac.Profesor!.Nombre)
                    .FirstOrDefault()
            })
            .OrderBy(m => m.Asignatura)
            .ToListAsync();

        return new AlumnoPanelResumenDto
        {
            Id = student.Id,
            Nombre = student.Nombre,
            Curso = new AlumnoCursoDto { CursoId = student.CursoId, Curso = student.Curso },
            Materias = subjects
        };
    }

    public async Task<AlumnoMateriaDetalleDto?> GetMateriaDetalleAsync(int estudianteId, int subjectId, CancellationToken cancellationToken = default)
    {
        var student = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => new { e.CursoId })
            .FirstOrDefaultAsync();

        if (student is null) return null;

        var subject = await context.Asignaturas
            .AsNoTracking()
            .Where(a => a.Id == subjectId)
            .Select(a => new
            {
                a.Id,
                a.Nombre,
                Profesor = a.ProfesorAsignaturaCursos
                    .Where(pac => pac.CursoId == student.CursoId)
                    .Select(pac => pac.Profesor!.Nombre)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync();

        if (subject is null) return null;

        var tasks = await context.Tareas
            .AsNoTracking()
            .Where(t => t.AsignaturaId == subjectId)
            .OrderBy(t => t.Trimestre)
            .ThenBy(t => t.Nombre)
            .ToListAsync();

        var taskIds = tasks.Select(t => t.Id).ToList();
        var studentGrades = await context.Notas
            .AsNoTracking()
            .Where(n => n.EstudianteId == estudianteId && taskIds.Contains(n.TareaId))
            .ToListAsync();

        var gradesList = tasks.Select(t =>
        {
            var grade = studentGrades.FirstOrDefault(n => n.TareaId == t.Id);
            return new AlumnoTareaDto
            {
                TareaId = t.Id,
                Nombre = t.Nombre,
                Trimestre = t.Trimestre,
                Valor = grade?.Valor
            };
        }).ToList();

        decimal? Media(int trim)
        {
            var vals = gradesList
                .Where(n => n.Trimestre == trim && n.Valor.HasValue)
                .Select(n => n.Valor!.Value)
                .ToList();
            return vals.Count > 0 ? Math.Round(vals.Average(), 2) : null;
        }

        var t1 = Media(1);
        var t2 = Media(2);
        var t3 = Media(3);
        decimal? finalGrade = (t1.HasValue && t2.HasValue && t3.HasValue)
            ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2)
            : null;

        return new AlumnoMateriaDetalleDto
        {
            AsignaturaId = subject.Id,
            Asignatura = subject.Nombre,
            Profesor = subject.Profesor,
            Notas = gradesList,
            Medias = new MediasTrimestralesDto { T1 = t1, T2 = t2, T3 = t3 },
            NotaFinal = finalGrade
        };
    }

    #region New queries

    public async Task<string?> GetCursoNombreAsync(int cursoId, CancellationToken cancellationToken = default)
        => await context.Cursos.AsNoTracking()
            .Where(c => c.Id == cursoId)
            .Select(c => (string?)c.Nombre)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<EstudianteListItemDto>> GetAllEstudiantesAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .Select(e => new EstudianteListItemDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Apellidos = e.Apellidos,
                DNI = e.DNI,
                Telefono = e.Telefono,
                FechaNacimiento = e.FechaNacimiento,
                Correo = e.Cuenta!.Correo,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<EstudianteLookupDto>> GetSimpleEstudiantesAsync(CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .OrderBy(e => e.Nombre)
            .Select(e => new EstudianteLookupDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null
            })
            .ToListAsync(cancellationToken);

    public async Task<EstudianteDetalleDto?> GetDetalleAsync(int estudianteId, CancellationToken cancellationToken = default)
        => await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => new EstudianteDetalleDto
            {
                Id = e.Id,
                Nombre = e.Nombre,
                Apellidos = e.Apellidos,
                DNI = e.DNI,
                Telefono = e.Telefono,
                FechaNacimiento = e.FechaNacimiento,
                Correo = e.Cuenta!.Correo,
                CursoId = e.CursoId,
                Curso = e.Curso != null ? e.Curso.Nombre : null,
                Asignaturas = e.EstudianteAsignaturas.Select(estudianteAsignatura => new EstudianteAsignaturaDetalleDto
                {
                    AsignaturaId = estudianteAsignatura.AsignaturaId,
                    Nombre = estudianteAsignatura.Asignatura!.Nombre,
                    ProfesorId = estudianteAsignatura.Asignatura.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == e.CursoId)
                        .Select(pac => (int?)pac.ProfesorId)
                        .FirstOrDefault(),
                    Profesor = estudianteAsignatura.Asignatura.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == e.CursoId)
                        .Select(pac => pac.Profesor!.Nombre)
                        .FirstOrDefault()
                }).ToList(),
                Notas = e.Notas.Select(grade => new EstudianteNotaDetalleDto
                {
                    TareaId = grade.TareaId,
                    Tarea = grade.Tarea!.Nombre,
                    AsignaturaId = grade.Tarea.AsignaturaId,
                    Asignatura = grade.Tarea.Asignatura!.Nombre,
                    Trimestre = grade.Tarea.Trimestre,
                    Valor = grade.Valor,
                    ProfesorId = grade.Tarea.ProfesorId,
                    Profesor = grade.Tarea.Profesor!.Nombre
                }).ToList()
            })
            .FirstOrDefaultAsync();

    public async Task<AlumnoPanelDto?> GetPanelAlumnoAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        var student = await context.Estudiantes
            .AsNoTracking()
            .Where(e => e.Id == estudianteId)
            .Select(e => new { e.Id, e.Nombre, e.CursoId, Curso = e.Curso!.Nombre })
            .FirstOrDefaultAsync();

        if (student is null) return null;

        var subjectIds = await context.EstudianteAsignaturas
            .Where(ea => ea.EstudianteId == estudianteId)
            .Select(ea => ea.AsignaturaId)
            .ToListAsync(cancellationToken);

        var subjects = new List<AlumnoMateriaDto>();
        foreach (var subjectId in subjectIds)
        {
            var subject = await context.Asignaturas
                .AsNoTracking()
                .Where(a => a.Id == subjectId)
                .Select(a => new
                {
                    a.Id, a.Nombre,
                    Profesor = a.ProfesorAsignaturaCursos
                        .Where(pac => pac.CursoId == student.CursoId)
                        .Select(pac => pac.Profesor!.Nombre)
                        .FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (subject is not null)
            {
                var tasks = await context.Tareas
                    .AsNoTracking()
                    .Where(t => t.AsignaturaId == subjectId)
                    .OrderBy(t => t.Trimestre).ThenBy(t => t.Nombre)
                    .ToListAsync(cancellationToken);

                var taskIds = tasks.Select(t => t.Id).ToList();
                var studentGrades = await context.Notas
                    .AsNoTracking()
                    .Where(n => n.EstudianteId == estudianteId && taskIds.Contains(n.TareaId))
                    .ToListAsync(cancellationToken);

                var gradesList = tasks.Select(t =>
                {
                    var grade = studentGrades.FirstOrDefault(n => n.TareaId == t.Id);
                    return new AlumnoTareaDto { TareaId = t.Id, Nombre = t.Nombre, Trimestre = t.Trimestre, Valor = grade?.Valor };
                }).ToList();

                decimal? Media(int trim)
                {
                    var vals = gradesList.Where(n => n.Trimestre == trim && n.Valor.HasValue).Select(n => n.Valor!.Value).ToList();
                    return vals.Count > 0 ? Math.Round(vals.Average(), 2) : null;
                }

                var t1 = Media(1); var t2 = Media(2); var t3 = Media(3);
                decimal? finalGrade = (t1.HasValue && t2.HasValue && t3.HasValue)
                    ? Math.Round((t1.Value + t2.Value + t3.Value) / 3, 2) : null;

                subjects.Add(new AlumnoMateriaDto
                {
                    AsignaturaId = subject.Id,
                    Asignatura = subject.Nombre,
                    Profesor = subject.Profesor,
                    Notas = gradesList,
                    Medias = new MediasTrimestralesDto { T1 = t1, T2 = t2, T3 = t3 },
                    NotaFinal = finalGrade
                });
            }
        }

        return new AlumnoPanelDto
        {
            Id = student.Id,
            Nombre = student.Nombre,
            Curso = new AlumnoCursoDto { CursoId = student.CursoId, Curso = student.Curso },
            Materias = subjects.OrderBy(m => m.Asignatura).ToList()
        };
    }

    #endregion

    #region Mutations

    public async Task<EstudianteListItemDto> CreateEstudianteAsync(string nombre, string correo, int cursoId, string contrasenaHash, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken cancellationToken = default)
    {
        var student = await context.Estudiantes
            .IgnoreQueryFilters()
            .Include(e => e.Cuenta)
            .FirstOrDefaultAsync(e => e.Cuenta != null && e.Cuenta.Correo == correo, cancellationToken);

        if (student is null)
        {
            student = new Estudiante
            {
                Nombre = nombre,
                Apellidos = apellidos,
                DNI = dni,
                Telefono = telefono,
                FechaNacimiento = fechaNacimiento,
                CursoId = cursoId,
                Cuenta = new Cuenta
                {
                    Correo = correo,
                    Contrasena = contrasenaHash,
                    Rol = Roles.Alumno
                }
            };
            context.Estudiantes.Add(student);
        }
        else
        {
            student.Nombre = nombre;
            student.Apellidos = apellidos;
            student.DNI = dni;
            student.Telefono = telefono;
            student.FechaNacimiento = fechaNacimiento;
            student.CursoId = cursoId;
            student.IsDeleted = false;
            if (student.Cuenta is not null)
            {
                student.Cuenta.Correo = correo;
                student.Cuenta.Contrasena = contrasenaHash;
                student.Cuenta.Rol = Roles.Alumno;
                student.Cuenta.IsDeleted = false;
            }
        }

        await context.SaveChangesAsync(cancellationToken);
        var courseName = await GetCursoNombreAsync(cursoId);
        return new EstudianteListItemDto { Id = student.Id, Nombre = student.Nombre, Apellidos = student.Apellidos, DNI = student.DNI, Telefono = student.Telefono, FechaNacimiento = student.FechaNacimiento, Correo = student.Cuenta!.Correo, CursoId = student.CursoId, Curso = courseName };
    }

    public async Task MatricularAsync(int estudianteId, int subjectId, CancellationToken cancellationToken = default)
    {
        var record = await context.EstudianteAsignaturas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.EstudianteId == estudianteId && x.AsignaturaId == subjectId, cancellationToken);

        if (record is null)
        {
            context.EstudianteAsignaturas.Add(new EstudianteAsignatura { EstudianteId = estudianteId, AsignaturaId = subjectId });
        }
        else
        {
            record.IsDeleted = false;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DesmatricularAsync(int estudianteId, int subjectId, CancellationToken cancellationToken = default)
    {
        var record = await context.EstudianteAsignaturas
            .FirstOrDefaultAsync(ea => ea.EstudianteId == estudianteId && ea.AsignaturaId == subjectId, cancellationToken);
        if (record is not null)
        {
            record.IsDeleted = true;
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<EstudianteListItemDto?> UpdateEstudianteAsync(int estudianteId, string nombre, string correo, int cursoId, string? contrasenaHash, string apellidos, string dni, string telefono, DateOnly fechaNacimiento, CancellationToken cancellationToken = default)
    {
        var student = await context.Estudiantes
            .Include(e => e.Cuenta)
            .FirstOrDefaultAsync(e => e.Id == estudianteId, cancellationToken);
        if (student is null) return null;

        student.Nombre = nombre;
        student.Apellidos = apellidos;
        student.DNI = dni;
        student.Telefono = telefono;
        student.FechaNacimiento = fechaNacimiento;
        student.CursoId = cursoId;
        if (student.Cuenta is not null)
        {
            student.Cuenta.Correo = correo;
            if (contrasenaHash is not null) student.Cuenta.Contrasena = contrasenaHash;
        }

        await context.SaveChangesAsync(cancellationToken);
        var courseName = await GetCursoNombreAsync(cursoId);
        return new EstudianteListItemDto { Id = student.Id, Nombre = student.Nombre, Apellidos = student.Apellidos, DNI = student.DNI, Telefono = student.Telefono, FechaNacimiento = student.FechaNacimiento, Correo = student.Cuenta!.Correo, CursoId = student.CursoId, Curso = courseName };
    }

    public async Task DeleteEstudianteAsync(int estudianteId, CancellationToken cancellationToken = default)
    {
        var enrollments = await context.EstudianteAsignaturas.Where(ea => ea.EstudianteId == estudianteId).ToListAsync(cancellationToken);
        foreach (var matricula in enrollments) matricula.IsDeleted = true;

        var grades = await context.Notas.Where(n => n.EstudianteId == estudianteId).ToListAsync(cancellationToken);
        foreach (var grade in grades) grade.IsDeleted = true;

        var tokens = await context.RefreshTokens
            .Where(t => t.UserId == estudianteId && t.Rol == Roles.Alumno)
            .ToListAsync(cancellationToken);
        foreach (var token in tokens) token.IsDeleted = true;

        var student = await context.Estudiantes
            .Include(e => e.Cuenta)
            .FirstOrDefaultAsync(e => e.Id == estudianteId, cancellationToken);
        if (student is not null)
        {
            student.IsDeleted = true;
            if (student.Cuenta is not null)
                student.Cuenta.IsDeleted = true;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    #endregion
}

