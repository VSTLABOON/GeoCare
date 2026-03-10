using GeoCare.Core.Helpers;

namespace GeoCare.Core.DTOs;

/// <summary>
/// DTO de respuesta para los endpoints de consulta de hospitales.
/// Expone solo los campos que el frontend necesita — nunca expone la entidad
/// <c>Hospital</c> directamente para no acoplar la API al modelo de dominio.
/// </summary>
public class HospitalResponseDto
{
    public int    Id      { get; set; }
    public string Name    { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de institución como string — el frontend no necesita conocer el enum.
    /// Valores posibles (coinciden con <see cref="GeoCare.Core.Entities.HospitalType"/>):
    ///   "Private" · "Imss" · "ImssB" · "Issste" · "SectorSalud"
    ///   "Pemex"   · "Sedena" · "Semar" · "CruzRoja" · "Dif"
    /// </summary>
    public string Type { get; set; } = string.Empty;

    public double Latitud  { get; set; }
    public double Longitud { get; set; }

    /// <summary>
    /// Distancia en metros al punto de consulta.
    /// Calculada en el controller multiplicando los grados devueltos por
    /// <c>STDistance()</c> (columna <c>geometry</c>) × 111 320 m/°.
    /// Migrar la columna a <c>geography</c> en AppDbContext eliminaría esa conversión.
    /// </summary>
    public double DistanciaMetros { get; set; }

    /// <summary>Distancia redondeada a kilómetros — lista para mostrar en UI.</summary>
    public double DistanciaKm => Math.Round(DistanciaMetros / 1000, 2);

    /// <summary>
    /// Personal ocupado según el DENUE (Campo 6). GeoCare solo importa valores ≥ 4.
    ///   4 → 31–50  · 5 → 51–100  · 6 → 101–250  · 7 → 251+
    /// </summary>
    public int Estrato { get; set; }

    /// <summary>
    /// Descripción legible del estrato delegada a <see cref="EstratoHelper.Describir"/>.
    /// El frontend la consume directamente sin lógica adicional en React.
    /// </summary>
    public string EstratoDesc => EstratoHelper.Describir(Estrato);
}