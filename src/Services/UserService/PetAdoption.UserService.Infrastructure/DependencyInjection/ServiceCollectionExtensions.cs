namespace PetAdoption.UserService.Infrastructure.DependencyInjection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
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
        // MongoDB
        var mongoConnectionString = configuration.GetConnectionString("MongoDb")
            ?? throw new InvalidOperationException("MongoDB connection string is not configured");

        var databaseName = configuration["Database:Name"] ?? "UserDb";

        var mongoClient = new MongoClient(mongoConnectionString);
        var mongoDatabase = mongoClient.GetDatabase(databaseName);

        services.AddSingleton<IMongoDatabase>(mongoDatabase);

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserQueryStore, UserQueryStore>();
        services.AddScoped<IOutboxRepository, OutboxRepository>();

        // Security Services
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

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

        // Query Handlers
        services.AddScoped<IQueryHandler<GetUserByIdQuery, UserDto>, GetUserByIdQueryHandler>();
        services.AddScoped<IQueryHandler<GetUserByEmailQuery, UserDto>, GetUserByEmailQueryHandler>();
        services.AddScoped<IQueryHandler<GetUsersQuery, GetUsersResponse>, GetUsersQueryHandler>();

        return services;
    }
}
