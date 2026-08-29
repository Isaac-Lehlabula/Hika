using FluentValidation;
using Hika.Application.Admin;
using Hika.Application.Bookings;
using Hika.Application.Chat;
using Hika.Application.Drivers;
using Hika.Application.Notifications;
using Hika.Application.Payments;
using Hika.Application.Reviews;
using Hika.Application.RideAlerts;
using Hika.Application.RideRequests;
using Hika.Application.Search;
using Hika.Application.Trips;
using Hika.Application.TrustSafety;
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
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IRideAlertService, RideAlertService>();
        services.AddScoped<IRideRequestService, RideRequestService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IBlockService, BlockService>();
        services.AddScoped<IEmergencyContactService, EmergencyContactService>();

        services.AddScoped<IAuditLogger, AuditLogger>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IAdminVerificationService, AdminVerificationService>();
        services.AddScoped<IAdminTripService, AdminTripService>();
        services.AddScoped<IAdminBookingService, AdminBookingService>();
        services.AddScoped<IAdminPaymentService, AdminPaymentService>();
        services.AddScoped<IAdminReportService, AdminReportService>();
        services.AddScoped<IAdminReviewService, AdminReviewService>();
        services.AddScoped<IAdminPlatformFeeService, AdminPlatformFeeService>();
        services.AddScoped<IAdminAnalyticsService, AdminAnalyticsService>();
        services.AddScoped<IAdminAuditLogService, AdminAuditLogService>();

        return services;
    }
}
