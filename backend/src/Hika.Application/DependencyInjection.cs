using FluentValidation;
using Hika.Application.Bookings;
using Hika.Application.Drivers;
using Hika.Application.Notifications;
using Hika.Application.Payments;
using Hika.Application.Reviews;
using Hika.Application.RideAlerts;
using Hika.Application.Search;
using Hika.Application.Trips;
using Hika.Application.Users;
using Microsoft.Extensions.DependencyInjection;

namespace Hika.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserProfileService, UserProfileService>();
        services.AddScoped<IDriverProfileService, DriverProfileService>();
        services.AddScoped<IVehicleService, VehicleService>();
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<ISearchService, SearchService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRideAlertService, RideAlertService>();

        return services;
    }
}
