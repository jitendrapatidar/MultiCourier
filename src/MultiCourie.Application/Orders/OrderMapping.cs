using MultiCourie.Application.Orders.Dtos;
using MultiCourie.Application.Orders.Models;
using MultiCourie.Domain.Entities;

namespace MultiCourie.Application.Orders;

internal static class OrderMapping
{
    public static NormalizedOrder ToNormalized(CreateOrderRequestDto dto) => new()
    {
        OrderId = dto.OrderId,
        CourierPartner = dto.CourierPartner,
        CustomerCode = dto.CustomerCode,
        ServiceType = dto.ServiceType,
        PayMode = dto.PayMode,
        DeclaredValue = dto.DeclaredValue,
        CollectableValue = dto.CollectableValue,
        WeightKg = dto.WeightKg,
        LengthCm = dto.LengthCm,
        BreadthCm = dto.BreadthCm,
        HeightCm = dto.HeightCm,
        Pieces = dto.Pieces,
        Consignee = MapParty(dto.Consignee),
        Shipper = MapParty(dto.Shipper),
        ReturnTo = MapParty(dto.ReturnTo),
        Invoice = new InvoiceDetails
        {
            InvoiceNumber = dto.Invoice.InvoiceNumber,
            InvoiceDate = dto.Invoice.InvoiceDate,
            InvoiceValue = dto.Invoice.InvoiceValue
        },
        Item = new ItemDetails
        {
            Description = dto.Item.Description,
            Quantity = dto.Item.Quantity
        }
    };

    private static PartyAddress MapParty(PartyAddressDto dto) => new()
    {
        Name = dto.Name,
        Address = dto.Address,
        City = dto.City,
        State = dto.State,
        Pincode = dto.Pincode,
        Mobile = dto.Mobile,
        Email = dto.Email,
        Country = dto.Country,
        AddressType = dto.AddressType
    };

    public static OrderResponseDto ToResponse(ShipmentOrder order, bool idempotentReplay = false) => new()
    {
        OrderId = order.OrderId,
        CourierPartner = order.CourierPartner,
        CourierOrderId = order.CourierOrderId,
        AwbNumber = order.AwbNumber,
        Status = ShipmentStatusMapper.ToApiString(order.Status),
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt,
        IdempotentReplay = idempotentReplay
    };
}
