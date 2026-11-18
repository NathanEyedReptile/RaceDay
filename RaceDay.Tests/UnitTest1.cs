
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace RaceDay.Tests;

public class UnitTest1
{
    [Fact]
    public async Task RegisterAndLogin_WithValidDetails_Succeeds()
    {
        using var factory = new RaceDayFactory();
        using var client = factory.CreateClient();

        // Add the roles required by the API.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            db.Roles.AddRange(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Organiser" },
                new Role { RoleId = 3, RoleName = "Participant" });

            await db.SaveChangesAsync();
        }

        var registration = new
        {
            FirstName = "Test",
            LastName = "Participant",
            Email = "test@example.com",
            Password = "RaceDayTest123!",
            Phone = "0821234567"
        };

        // Test registration.
        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Confirm the password was hashed.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            var user = await db.Users.SingleAsync();

            Assert.NotEqual(registration.Password, user.PasswordHash);
            Assert.Equal(3, user.RoleId);
        }

        // Test login.
        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                registration.Email,
                registration.Password
            });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var json = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();

        Assert.True(json.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrWhiteSpace(token.GetString()));
    }

    [Fact]
    public async Task Participant_CannotCreateEvent_ReturnsForbidden()
    {
    using var factory = new RaceDayFactory();
    using var client = factory.CreateClient();

    // Add the roles required by the API.
    using (var scope = factory.Services.CreateScope())
    {
        var db = scope.ServiceProvider
            .GetRequiredService<RaceDayDbContext>();

        db.Roles.AddRange(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Organiser" },
            new Role { RoleId = 3, RoleName = "Participant" });

        await db.SaveChangesAsync();
    }

    // Register a participant and log in.
    var registration = new
    {
        FirstName = "Test",
        LastName = "Participant",
        Email = "participant2@example.com",
        Password = "RaceDayTest123!",
        Phone = "0821234567"
    };

    var registerResponse = await client.PostAsJsonAsync(
        "/api/auth/register", registration);

    Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

    var loginResponse = await client.PostAsJsonAsync(
        "/api/auth/login",
        new
        {
            registration.Email,
            registration.Password
        });

    Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

    var loginJson = await loginResponse.Content
        .ReadFromJsonAsync<JsonElement>();

    var token = loginJson.GetProperty("token").GetString();

    Assert.False(string.IsNullOrWhiteSpace(token));

    // Use the participant's token to attempt to create an event.
    client.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", token);

    var eventResponse = await client.PostAsJsonAsync(
        "/api/events",
        new
        {
            EventName = "Test Race",
            Description = "A test event",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Durban",
            Distance = 5.0m,
            EventType = "Running",
            BannerImage = ""
        });

    var responseBody = await eventResponse.Content.ReadAsStringAsync();
    var authHeader = string.Join(
    "; ",
    eventResponse.Headers.WwwAuthenticate.Select(h => h.ToString()));

Console.WriteLine($"Auth header: {authHeader}");

Assert.True(
    eventResponse.StatusCode == HttpStatusCode.Forbidden,
    $"Expected 403 Forbidden but got {(int)eventResponse.StatusCode} " +
    $"{eventResponse.StatusCode}. Response: {responseBody}");
}


    [Fact]
    public async Task Organiser_CanCreateEvent_ReturnsCreated()
    {
        using var factory = new RaceDayFactory();
        using var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            db.Roles.AddRange(
                new Role { RoleId = 1, RoleName = "Admin" },
                new Role { RoleId = 2, RoleName = "Organiser" },
                new Role { RoleId = 3, RoleName = "Participant" });

            await db.SaveChangesAsync();
        }

        var registration = new
        {
            FirstName = "Test",
            LastName = "Organiser",
            Email = "organiser@example.com",
            Password = "RaceDayTest123!",
            Phone = "0821234567"
        };

        var registerResponse = await client.PostAsJsonAsync(
            "/api/auth/register", registration);

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        // Promote the test account to Organiser.
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RaceDayDbContext>();

            var user = await db.Users.SingleAsync();
            user.RoleId = 2;

            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { registration.Email, registration.Password });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var loginJson = await loginResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var token = loginJson.GetProperty("token").GetString();

        Assert.False(string.IsNullOrWhiteSpace(token));

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer", token);

        var eventResponse = await client.PostAsJsonAsync(
            "/api/events",
            new
            {
                EventName = "Organiser Test Race",
                Description = "An event created by an organiser",
                EventDate = DateTime.UtcNow.AddDays(7),
                Location = "Durban",
                Distance = 5.0,
                EventType = "Running",
                BannerImage = ""
            });

        Assert.Equal(
            HttpStatusCode.Created,
            eventResponse.StatusCode);
    }



}


public class RaceDayFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(
            "Jwt:Key",
            "TestKey_123456789012345678901234567890!");

        builder.UseSetting("Jwt:Issuer", "RaceDay.Api");
        builder.UseSetting("Jwt:Audience", "RaceDay.Client");
        builder.UseSetting("Jwt:ExpiryMinutes", "60");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            services.RemoveAll<
                IDbContextOptionsConfiguration<RaceDayDbContext>>();

            var databaseName = Guid.NewGuid().ToString();

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(databaseName));
        });
    }
}

