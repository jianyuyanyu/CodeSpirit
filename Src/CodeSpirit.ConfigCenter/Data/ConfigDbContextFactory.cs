using CodeSpirit.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;

namespace CodeSpirit.ConfigCenter.Data
{
    /// <summary>
    /// Design-time DbContext Factory for EF Core migrations
    /// </summary>
    public class ConfigDbContextFactory : IDesignTimeDbContextFactory<ConfigDbContext>
    {
        public ConfigDbContext CreateDbContext(string[] args)
        {
            // Build configuration
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            // Get connection string from configuration
            string connectionString = configuration.GetConnectionString("config-api");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Could not find connection string 'config-api' in appsettings.json");
            }

            // Create DbContext options
            var optionsBuilder = new DbContextOptionsBuilder<ConfigDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            // Create a mock CurrentUser for migrations
            var currentUser = new NullCurrentUser();

            // Return the DbContext with options
            return new ConfigDbContext(
                optionsBuilder.Options, 
                new EmptyServiceProvider(), 
                currentUser);
        }
    }

    /// <summary>
    /// Empty IServiceProvider for design-time factory
    /// </summary>
    internal class EmptyServiceProvider : IServiceProvider
    {
        public object GetService(Type serviceType)
        {
            return null;
        }
    }

    /// <summary>
    /// Null implementation of ICurrentUser for design-time factory
    /// </summary>
    internal class NullCurrentUser : ICurrentUser
    {
        public long? Id => null;
        public string UserName => "MIGRATION_USER";
        public bool IsAuthenticated => false;
        public string[] Roles => Array.Empty<string>();
        public IEnumerable<Claim> Claims => Array.Empty<Claim>();

        public bool IsInRole(string role)
        {
            return false;
        }
    }
}