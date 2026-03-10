// GeoCare.Core/Models/ImportJob.cs
namespace GeoCare.Core.Models;

public enum ImportJobStatus { Pending, Running, Completed, Failed }

public class ImportJob
{
    public string          Id          { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public ImportJobStatus Status      { get; set; }  = ImportJobStatus.Pending;
    public string          Mensaje     { get; set; }  = string.Empty;
    public int             Importados  { get; set; }
    public int             Duplicados  { get; set; }
    public DateTime        CreadoEn    { get; init; } = DateTime.UtcNow;
    public DateTime?       TerminadoEn { get; set; }

    // Resumen por tipo de institución
    public List<ResumenTipo> ResumenPorTipo { get; set; } = [];

    public record ResumenTipo(string Tipo, int Cantidad);
}