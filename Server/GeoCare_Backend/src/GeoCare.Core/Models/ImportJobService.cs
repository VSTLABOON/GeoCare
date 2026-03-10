// GeoCare.Infrastructure/Services/ImportJobService.cs
using System.Collections.Concurrent;
using GeoCare.Core.Models;

namespace GeoCare.Infrastructure.Services;

/// <summary>
/// Almacén en memoria de los jobs de importación activos/recientes.
/// Singleton — un solo diccionario compartido por toda la aplicación.
/// Los jobs se limpian automáticamente después de 2 horas para no
/// acumular entradas indefinidamente.
/// </summary>
public class ImportJobService
{
    private readonly ConcurrentDictionary<string, ImportJob> _jobs = new();

    public ImportJob Crear()
    {
        var job = new ImportJob();
        _jobs[job.Id] = job;
        LimpiarViejos();
        return job;
    }

    public ImportJob? Obtener(string id) =>
        _jobs.TryGetValue(id, out var job) ? job : null;

    private void LimpiarViejos()
    {
        var limite = DateTime.UtcNow.AddHours(-2);
        foreach (var (key, job) in _jobs)
            if (job.CreadoEn < limite)
                _jobs.TryRemove(key, out _);
    }
}