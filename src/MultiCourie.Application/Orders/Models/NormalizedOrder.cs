namespace MultiCourie.Application.Orders.Models;

public sealed class NormalizedOrder
{
    public required string OrderId { get; init; }

    public required string CourierPartner { get; init; }

    public string? CustomerCode { get; init; }

    public required string ServiceType { get; init; }

    public required string PayMode { get; init; }

    public decimal DeclaredValue { get; init; }

    public decimal CollectableValue { get; init; }

    public decimal WeightKg { get; init; }

    public decimal LengthCm { get; init; }

    public decimal BreadthCm { get; init; }

    public decimal HeightCm { get; init; }

    public int Pieces { get; init; } = 1;

    public required PartyAddress Consignee { get; init; }

    public required PartyAddress Shipper { get; init; }

    public required PartyAddress ReturnTo { get; init; }

    public required InvoiceDetails Invoice { get; init; }

    public required ItemDetails Item { get; init; }
}

public sealed class PartyAddress
{
    public required string Name { get; init; }

    public required string Address { get; init; }

    public required string City { get; init; }

    public required string State { get; init; }

    public required string Pincode { get; init; }

    public required string Mobile { get; init; }

    public string? Email { get; init; }

    public string Country { get; init; } = "INDIA";

    public string AddressType { get; init; } = "Home";
}

public sealed class InvoiceDetails
{
    public required string InvoiceNumber { get; init; }

    public required string InvoiceDate { get; init; }

    public decimal InvoiceValue { get; init; }
}

public sealed class ItemDetails
{
    public required string Description { get; init; }

    public int Quantity { get; init; } = 1;
}
