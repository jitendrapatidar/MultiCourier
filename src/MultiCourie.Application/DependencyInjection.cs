using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MultiCourie.Application.Couriers;
using MultiCourie.Application.Orders;
using MultiCourie.Application.Orders.Validators;

namespace MultiCourie.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton<CourierPartnerResolver>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddValidatorsFromAssemblyContaining<CreateOrderRequestValidator>();
        return services;
    }
}
