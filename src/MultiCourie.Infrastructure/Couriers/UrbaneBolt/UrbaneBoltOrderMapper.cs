using System.Text.Json;
using MultiCourie.Application.Orders.Models;
using MultiCourie.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace MultiCourie.Infrastructure.Couriers.UrbaneBolt;
/// <summary>
/// Maps a normalized order to the Urbane Bolt manifest payload format for shipment creation.
/// </summary>
public sealed class UrbaneBoltOrderMapper
{
    private readonly UrbaneBoltOptions _options;

    public UrbaneBoltOrderMapper(IOptions<UrbaneBoltOptions> options) => _options = options.Value;
    /// <summary>
    /// Builds the manifest payload for a given normalized order in the format expected by Urbane Bolt's API.
    /// </summary>
    /// <param name="order"></param>
    /// <returns></returns>
    public string BuildManifestPayload(NormalizedOrder order)
    {
        var manifest = new[]
        {
            new Dictionary<string, object?>
            {
                ["customerCode"] = order.CustomerCode ?? _options.CustomerCode,
                ["orderNumber"] = order.OrderId,
                ["declaredValue"] = order.DeclaredValue,
                ["itemDescription"] = order.Item.Description,
                ["collectableValue"] = order.CollectableValue,
                ["height"] = order.HeightCm,
                ["length"] = order.LengthCm,
                ["pieces"] = order.Pieces,
                ["weight"] = order.WeightKg,
                ["breadth"] = order.BreadthCm,
                ["serviceType"] = order.ServiceType,
                ["payMode"] = order.PayMode,
                ["rtnCity"] = order.ReturnTo.City,
                ["rtnName"] = order.ReturnTo.Name,
                ["consCity"] = order.Consignee.City,
                ["consName"] = order.Consignee.Name,
                ["rtnEmail"] = order.ReturnTo.Email,
                ["rtnState"] = order.ReturnTo.State,
                ["shprCity"] = order.Shipper.City,
                ["shprName"] = order.Shipper.Name,
                ["consEmail"] = order.Consignee.Email,
                ["consState"] = order.Consignee.State,
                ["rtnMobile"] = ParseMobile(order.ReturnTo.Mobile),
                ["shprEmail"] = order.Shipper.Email,
                ["shprState"] = order.Shipper.State,
                ["consMobile"] = ParseMobile(order.Consignee.Mobile),
                ["rtnAddress"] = order.ReturnTo.Address,
                ["rtnAddressType"] = order.ReturnTo.AddressType,
                ["rtnCountry"] = order.ReturnTo.Country,
                ["rtnPincode"] = ParsePincode(order.ReturnTo.Pincode),
                ["shprMobile"] = ParseMobile(order.Shipper.Mobile),
                ["consAddress"] = order.Consignee.Address,
                ["consAddressType"] = order.Consignee.AddressType,
                ["consCountry"] = order.Consignee.Country,
                ["consPincode"] = ParsePincode(order.Consignee.Pincode),
                ["invoiceNumber"] = order.Invoice.InvoiceNumber,
                ["invoiceDate"] = order.Invoice.InvoiceDate,
                ["shprAddress"] = order.Shipper.Address,
                ["shprAddressType"] = order.Shipper.AddressType,
                ["shprCountry"] = order.Shipper.Country,
                ["shprPincode"] = ParsePincode(order.Shipper.Pincode),
                ["invoiceValue"] = order.Invoice.InvoiceValue,
                ["itemQuantity"] = order.Item.Quantity
            }
        };

        return JsonSerializer.Serialize(manifest);
    }

    private static long ParseMobile(string mobile)
    {
        var digits = new string(mobile.Where(char.IsDigit).ToArray());
        return long.TryParse(digits, out var value) ? value : 0;
    }

    private static int ParsePincode(string pincode) =>
        int.TryParse(pincode, out var value) ? value : 0;
}
