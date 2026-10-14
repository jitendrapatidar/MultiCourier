using MultiCourie.Application.Orders.Models;

namespace MultiCourie.Application.Couriers;

public interface ICourierPartner
{
    string PartnerKey { get; }

    Task<CourierCreateResult> CreateShipmentAsync(NormalizedOrder order, CancellationToken cancellationToken = default);

    Task<CourierTrackResult> TrackShipmentAsync(string awbNumber, CancellationToken cancellationToken = default);

    Task<CourierCancelResult> CancelShipmentAsync(string awbNumber, CancellationToken cancellationToken = default);
}
