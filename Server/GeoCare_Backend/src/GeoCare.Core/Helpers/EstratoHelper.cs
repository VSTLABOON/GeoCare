// GeoCare.Core/Helpers/EstratoHelper.cs
namespace GeoCare.Core.Helpers;

/// <summary>
/// Convierte el código de estrato del DENUE (Campo 6) a texto legible.
/// Centralizado para evitar duplicación entre <see cref="HospitalResponseDto"/>
/// e <c>InegiService</c>.
///   1 → 0–5 personas   2 → 6–10   3 → 11–30   4 → 31–50
///   5 → 51–100         6 → 101–250             7 → 251+
/// </summary>
public static class EstratoHelper
{
    public static string Describir(int estrato) => estrato switch
    {
        1 => "0–5 personas",
        2 => "6–10 personas",
        3 => "11–30 personas",
        4 => "31–50 personas",
        5 => "51–100 personas",
        6 => "101–250 personas",
        7 => "251+ personas",
        _ => "Desconocido"
    };
}