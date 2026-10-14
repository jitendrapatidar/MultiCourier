using MultiCourie.Application.Common;

namespace MultiCourie.Application.Couriers;

public sealed class CourierPartnerResolver
{
    private readonly IReadOnlyDictionary<string, ICourierPartner> _partners;

    public CourierPartnerResolver(IEnumerable<ICourierPartner> partners)
    {
        _partners = partners.ToDictionary(
            p => p.PartnerKey,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> SupportedPartners => _partners.Keys.ToList();

    public ICourierPartner Resolve(string courierPartner)
    {
        if (_partners.TryGetValue(courierPartner.Trim(), out var partner))
        {
            return partner;
        }

        throw new AppException(
            ErrorCodes.UnknownCourier,
            $"Courier partner '{courierPartner}' is not supported.",
            statusCode: 400);
    }
}
