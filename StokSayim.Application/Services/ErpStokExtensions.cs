using StokSayim.Domain.Entities;

namespace StokSayim.Application.Services;

internal static class ErpStokExtensions
{
    /// <summary>
    /// ERP stoklarından yalnızca sayım planına ait depo kodlarındaki (SayimPlanDepoKodlari) satırları döner.
    /// </summary>
    public static List<ErpStok> SadecePlanDepolari(this IEnumerable<ErpStok> erpStoklar, IEnumerable<SayimPlanDepoKodu> planDepolari)
    {
        var depolar = planDepolari
            .Select(d => (d.DepoKodu ?? string.Empty).Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return erpStoklar
            .Where(e => depolar.Contains((e.DepoKodu ?? string.Empty).Trim()))
            .ToList();
    }
}
