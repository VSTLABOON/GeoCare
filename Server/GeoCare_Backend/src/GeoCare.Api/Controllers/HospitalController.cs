using GeoCare.Core.DTOs;
using GeoCare.Core.Entities;
using GeoCare.Core.Models;
using GeoCare.Infrastructure.Data;
using GeoCare.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Microsoft.AspNetCore.Http.Timeouts;

namespace GeoCare.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HospitalController : ControllerBase
{
    private readonly AppDbContext    _context;
    private readonly InegiService    _inegiService;
    private readonly GeometryFactory _geometryFactory;

    // Metros por grado de latitud (valor estándar, suficientemente preciso para Puebla).
    // Necesario porque NetTopologySuite con SQL Server usa columna 'geometry' por defecto,
    // y STDistance sobre geometry devuelve grados, no metros.
    // Alternativa a largo plazo: migrar la columna Location a 'geography' en EF Core
    // para que STDistance devuelva metros directamente.
    private const double MetrosPorGrado = 111_320.0;

    public HospitalController(AppDbContext context, InegiService inegiService)
    {
        _context         = context;
        _inegiService    = inegiService;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/hospital/cercanos?lat=19.0413&lng=-98.2062&metros=5000
    // ─────────────────────────────────────────────────────────────────────────
    // ─────────────────────────────────────────────────────────────────────────
    // GET api/hospital/cercanos?lat=19.0413&lng=-98.2062&metros=2000&limite=10&pagina=1
    //
    // Paginación por offset — el frontend llama con pagina=1, 2, 3... para
    // implementar un botón "Ver más" sin recargar todos los resultados.
    //
    // La respuesta incluye metadatos de paginación para que el frontend sepa
    // si hay más páginas disponibles y pueda mostrar/ocultar el botón.
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet("cercanos")]
    public async Task<ActionResult<object>> GetCercanos(
        [FromQuery] double lat,
        [FromQuery] double lng,
        [FromQuery] int    metros = 2000,
        [FromQuery] int    limite = 10,
        [FromQuery] int    pagina = 1)
    {
        if (lat == 0 || lng == 0)
            return BadRequest(new { error = "Debes enviar lat y lng como query params." });

        if (metros is < 100 or > 50000)
            return BadRequest(new { error = "El radio debe estar entre 100 y 50 000 metros." });

        if (limite is < 1 or > 100)
            return BadRequest(new { error = "El límite debe estar entre 1 y 100." });

        if (pagina < 1)
            return BadRequest(new { error = "La página debe ser mayor a 0." });

        var puntoUsuario = _geometryFactory.CreatePoint(new Coordinate(lng, lat));
        double radioGrados = metros / MetrosPorGrado;

        // Consulta base — se reutiliza para el total y para la página actual.
        var query = _context.Hospitals
            .Where(h => h.Location.Distance(puntoUsuario) <= radioGrados)
            .OrderBy(h => h.Location.Distance(puntoUsuario));

        // Total en la zona — necesario para saber si hay más páginas.
        // EF Core lo traduce a un COUNT separado, no trae todos los registros.
        var total = await query.CountAsync();

        if (total == 0)
            return NotFound(new
            {
                mensaje    = $"No hay hospitales en un radio de {metros}m.",
                sugerencia = "Aumenta el radio o importa más datos del DENUE."
            });

        int skip = (pagina - 1) * limite;

        var hospitales = await query
            .Skip(skip)
            .Take(limite)
            .Select(h => new HospitalResponseDto
            {
                Id              = h.Id,
                Name            = h.Name,
                Address         = h.Address,
                Type            = h.Type.ToString(),
                Latitud         = h.Location.Y,
                Longitud        = h.Location.X,
                DistanciaMetros = h.Location.Distance(puntoUsuario) * MetrosPorGrado,
                Estrato         = h.Estrato
            })
            .ToListAsync();

        return Ok(new
        {
            // Metadatos de paginación — el frontend los usa para el botón "Ver más"
            total,
            pagina,
            limite,
            totalPaginas = (int)Math.Ceiling(total / (double)limite),
            hayMas       = skip + hospitales.Count < total,
            // Lista de hospitales de esta página
            resultados   = hospitales
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // GET api/hospital
    // ─────────────────────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Hospital>>> GetHospitales()
    {
        return await _context.Hospitals.ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/hospital
    // ─────────────────────────────────────────────────────────────────────────
    [HttpPost]
    public async Task<ActionResult<Hospital>> PostHospital(Hospital hospital)
    {
        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetHospitales), new { id = hospital.Id }, hospital);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // POST api/hospital/importar-puebla
    //
    // Usa BuscarAreaAct del DENUE para traer todos los hospitales del estado
    // de Puebla (entidad 21), iterando sobre las siguientes clases SCIAN:
    //   621111 — Hospital general público
    //   621112 — Hospital general privado
    //   621113 — Hospital de especialidad público
    //   621114 — Hospital de especialidad privado
    //   621210 — Clínicas y similares con internamiento
    //
    // Usa la política de timeout "import" (sin límite) definida en Program.cs.
    // [RequestTimeout(int)] recibe milisegundos — usar el overload int aquí
    // con 300 equivaldría a 0.3 segundos, cancelando el request de inmediato.
    // ─────────────────────────────────────────────────────────────────────────
    // POST api/hospital/importar-puebla → inicia el job, responde 202 inmediato
    [HttpPost("importar-puebla")]
    public IActionResult ImportarDesdeInegi(
        [FromServices] ImportJobService jobService,
        [FromServices] IServiceScopeFactory scopeFactory)
    {
        var job = jobService.Crear();
        job.Status = ImportJobStatus.Running;

        // Fire-and-forget en un scope propio — necesario porque DbContext
        // no es thread-safe y no puede compartirse entre el request actual
        // y el Task en background.
        _ = Task.Run(async () =>
        {
            await using var scope   = scopeFactory.CreateAsyncScope();
            var inegiService        = scope.ServiceProvider.GetRequiredService<InegiService>();
            var context             = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            try
            {
                var hospitalesInegi = await inegiService.ImportarTodosLosHospitalesDePuebla();

                if (hospitalesInegi.Count == 0)
                {
                    job.Status  = ImportJobStatus.Failed;
                    job.Mensaje = "El DENUE no devolvió resultados para Puebla.";
                    return;
                }

                // ── Deduplicación por coordenadas redondeadas ──────────────────
                var ubicacionesExistentes = await context.Hospitals
                    .Select(h => new { h.Location.X, h.Location.Y })
                    .ToListAsync();

                var existentes = ubicacionesExistentes
                    .Select(e => (X: Math.Round(e.X, 6), Y: Math.Round(e.Y, 6)))
                    .ToHashSet();

                var nuevos = hospitalesInegi
                    .Where(h => !existentes.Contains((
                        X: Math.Round(h.Location.X, 6),
                        Y: Math.Round(h.Location.Y, 6))))
                    .ToList();

                if (nuevos.Count > 0)
                {
                    context.Hospitals.AddRange(nuevos);
                    await context.SaveChangesAsync();
                }

                job.Status      = ImportJobStatus.Completed;
                job.Importados  = nuevos.Count;
                job.Duplicados  = hospitalesInegi.Count - nuevos.Count;
                job.Mensaje     = nuevos.Count > 0
                    ? $"¡Éxito! Se importaron {nuevos.Count} hospitales."
                    : "Todos los hospitales ya estaban registrados.";
                job.ResumenPorTipo = nuevos
                    .GroupBy(h => h.Type.ToString())
                    .Select(g => new ImportJob.ResumenTipo(g.Key, g.Count()))
                    .ToList();
            }
            catch (Exception ex)
            {
                job.Status  = ImportJobStatus.Failed;
                job.Mensaje = ex.Message;
            }
            finally
            {
                job.TerminadoEn = DateTime.UtcNow;
            }
        });

        // 202 Accepted — el cliente debe hacer polling a /estado/{job.Id}
        return Accepted(new
        {
            jobId      = job.Id,
            statusUrl  = $"/api/hospital/importar-estado/{job.Id}",
            mensaje    = "Importación iniciada. Consulta statusUrl para ver el progreso."
        });
    }

    // GET api/hospital/importar-estado/{jobId} → polling de estado
    [HttpGet("importar-estado/{jobId}")]
    public ActionResult<ImportJob> GetEstadoImport(
        string jobId,
        [FromServices] ImportJobService jobService)
    {
        var job = jobService.Obtener(jobId);

        if (job is null)
            return NotFound(new { error = $"Job '{jobId}' no encontrado o expirado." });

        return Ok(job);
    }
}