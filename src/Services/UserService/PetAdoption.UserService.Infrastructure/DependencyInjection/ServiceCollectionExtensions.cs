namespace PetAdoption.UserService.Infrastructure.DependencyInjection;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.UserService.Application.Abstractions;
using PetAdoption.UserService.Application.Commands;
using PetAdoption.UserService.Application.DTOs;
using PetAdoption.UserService.Application.Queries;
using PetAdoption.UserService.Domain.Interfaces;
using PetAdoption.UserService.Infrastructure.BackgroundServices;
using PetAdoption.UserService.Infrastructure.Messaging;
using PetAdoption.UserService.Infrastructure.Messaging.Configuration;
using PetAdoption.UserService.Infrastructure.Persistence;
using PetAdoption.UserService.Infrastructure.Security;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // SQL Server via EF Core
        var connectionString = configuration.GetConnectionString("SqlServer")
            ?? throw new InvalidOperationException("SQL Server connection string is not configured");

        services.AddDbContext<UserServiceDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQueryStore, UserQueryStore>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        // Security Services
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddHttpClient<IGoogleTokenValidator, GoogleTokenValidator>();

        // JWT Configuration
        services.Configure<JwtOptions>(
            configuration.GetSection("Jwt")
        );

        // RabbitMQ Configuration
        services.Configure<RabbitMqOptions>(
            configuration.GetSection("RabbitMq")
        );

        // RabbitMQ Topology Setup (runs on startup)
        services.AddHostedService<RabbitMqTopologySetup>();

        // Outbox Processor Background Service
        services.AddHostedService<OutboxProcessorService>();

        // Command Handlers
        services.AddScoped<ICommandHandler<RegisterUserCommand, RegisterUserResponse>, RegisterUserCommandHandler>();
        services.AddScoped<ICommandHandler<LoginCommand, LoginResponse>, LoginCommandHandler>();
        services.AddScoped<ICommandHandler<UpdateUserProfileCommand, UpdateUserProfileResponse>, UpdateUserProfileCommandHandler>();
        services.AddScoped<ICommandHandler<ChangePasswordCommand, ChangePasswordResponse>, ChangePasswordCommandHandler>();
        services.AddScoped<ICommandHandler<PromoteToAdminCommand, PromoteToAdminResponse>, PromoteToAdminCommandHandler>();
        services.AddScoped<ICommandHandler<SuspendUserCommand, SuspendUserResponse>, SuspendUserCommandHandler>();
        services.AddScoped<ICommandHandler<ActivateUserCommand, ActivateUserResponse>, ActivateUserCommandHandler>();
        services.AddScoped<ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>, RefreshTokenCommandHandler>();
        services.AddScoped<ICommandHandler<GoogleAuthCommand, GoogleAuthResponse>, GoogleAuthCommandHandler>();

        // Query Handlers
        services.AddScoped<IQueryHandler<GetUserByIdQuery, UserDto>, GetUserByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserByEmailQuery, UserDto>, GetUserByEmailQueryHandler>();
        services.AddScoped<IQueryHandler<GetUsersQuery, GetUsersResponse>, GetUsersQueryHandler>();

        return services;
    }
}
