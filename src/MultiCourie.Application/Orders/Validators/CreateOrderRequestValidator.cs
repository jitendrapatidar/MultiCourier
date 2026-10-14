using FluentValidation;
using MultiCourie.Application.Orders.Dtos;

namespace MultiCourie.Application.Orders.Validators;

public sealed class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequestDto>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.CourierPartner).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ServiceType).NotEmpty();
        RuleFor(x => x.PayMode).NotEmpty().Must(p => p is "COD" or "PPD").WithMessage("PayMode must be COD or PPD.");
        RuleFor(x => x.WeightKg).GreaterThan(0);
        RuleFor(x => x.Pieces).GreaterThan(0);
        RuleFor(x => x.Consignee).SetValidator(new PartyAddressValidator());
        RuleFor(x => x.Shipper).SetValidator(new PartyAddressValidator());
        RuleFor(x => x.ReturnTo).SetValidator(new PartyAddressValidator());
        RuleFor(x => x.Invoice).SetValidator(new InvoiceDetailsValidator());
        RuleFor(x => x.Item).SetValidator(new ItemDetailsValidator());
    }
}

public sealed class PartyAddressValidator : AbstractValidator<PartyAddressDto>
{
    public PartyAddressValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Address).NotEmpty();
        RuleFor(x => x.City).NotEmpty();
        RuleFor(x => x.State).NotEmpty();
        RuleFor(x => x.Pincode).NotEmpty().Matches(@"^\d{6}$").WithMessage("Pincode must be 6 digits.");
        RuleFor(x => x.Mobile).NotEmpty();
    }
}

public sealed class InvoiceDetailsValidator : AbstractValidator<InvoiceDetailsDto>
{
    public InvoiceDetailsValidator()
    {
        RuleFor(x => x.InvoiceNumber).NotEmpty();
        RuleFor(x => x.InvoiceDate).NotEmpty();
        RuleFor(x => x.InvoiceValue).GreaterThanOrEqualTo(0);
    }
}

public sealed class ItemDetailsValidator : AbstractValidator<ItemDetailsDto>
{
    public ItemDetailsValidator()
    {
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public sealed class BulkCreateOrdersRequestValidator : AbstractValidator<BulkCreateOrdersRequestDto>
{
    public BulkCreateOrdersRequestValidator()
    {
        RuleFor(x => x.Orders).NotEmpty().Must(o => o.Count <= 100).WithMessage("A bulk request may contain at most 100 orders.");
        RuleForEach(x => x.Orders).SetValidator(new CreateOrderRequestValidator());
    }
}
