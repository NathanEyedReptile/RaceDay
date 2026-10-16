

using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using Microsoft.AspNetCore.Identity;
using RaceDay.Api.DTOs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.OpenApi;



var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();


builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token."
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});



var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is missing.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtAudience = builder.Configuration["Jwt:Audience"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrganiserOrAdmin", policy =>
        policy.RequireRole("Organiser", "Admin"));
});

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("RaceDay")));
        builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "RaceDay API",
    status = "Running"
}));


app.MapGet("/api/events", async (RaceDayDbContext db) =>
{
    var events = await db.Events
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(events);
})
.RequireAuthorization();



app.MapGet("/api/events/{id:int}", async (int id, RaceDayDbContext db) =>
{
    var raceEvent = await db.Events
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.EventId == id);

    return raceEvent is null
        ? Results.NotFound(new { message = "Event not found." })
        : Results.Ok(raceEvent);
})
.RequireAuthorization();



app.MapPost("/api/events", async (
    Event newEvent,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(newEvent.EventName) ||
        string.IsNullOrWhiteSpace(newEvent.Description) ||
        string.IsNullOrWhiteSpace(newEvent.Location) ||
        string.IsNullOrWhiteSpace(newEvent.EventType))
    {
        return Results.BadRequest(new
        {
            message = "Event name, description, location, and event type are required."
        });
    }

    if (newEvent.Distance <= 0)
    {
        return Results.BadRequest(new
        {
            message = "Distance must be greater than zero."
        });
    }

    var organiserId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    newEvent.Organiser = organiserId;

    db.Events.Add(newEvent);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/events/{newEvent.EventId}",
        newEvent);
})
.RequireAuthorization("OrganiserOrAdmin");




app.MapPut("/api/events/{id:int}", async (
    int id,
    Event updatedEvent,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var existingEvent = await db.Events.FindAsync(id);

    if (existingEvent is null)
        return Results.NotFound(new { message = "Event not found." });

    var isAdmin = user.IsInRole("Admin");
    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    if (!isAdmin && existingEvent.Organiser != userId)
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(updatedEvent.EventName) ||
        string.IsNullOrWhiteSpace(updatedEvent.Description) ||
        string.IsNullOrWhiteSpace(updatedEvent.Location) ||
        string.IsNullOrWhiteSpace(updatedEvent.EventType))
    {
        return Results.BadRequest(new
        {
            message = "Event name, description, location, and event type are required."
        });
    }

    if (updatedEvent.Distance <= 0)
        return Results.BadRequest(new
        {
            message = "Distance must be greater than zero."
        });

    existingEvent.EventName = updatedEvent.EventName.Trim();
    existingEvent.Description = updatedEvent.Description.Trim();
    existingEvent.EventDate = updatedEvent.EventDate;
    existingEvent.Location = updatedEvent.Location.Trim();
    existingEvent.Distance = updatedEvent.Distance;
    existingEvent.EventType = updatedEvent.EventType.Trim();
    existingEvent.BannerImage = updatedEvent.BannerImage;

    await db.SaveChangesAsync();

    return Results.Ok(existingEvent);
})
.RequireAuthorization("OrganiserOrAdmin");



app.MapDelete("/api/events/{id:int}", async (int id, RaceDayDbContext db) =>
{
    var raceEvent = await db.Events.FindAsync(id);

    if (raceEvent is null)
    {
        return Results.NotFound(new { message = "Event not found." });
    }

    db.Events.Remove(raceEvent);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization("OrganiserOrAdmin");


app.MapGet("/api/health/database", async (RaceDayDbContext db) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync();

        return canConnect
            ? Results.Ok(new { database = "Connected" })
            : Results.Problem("Could not connect to RaceDayDB.");
    }
    catch (Exception)
    {
        return Results.Problem(
            "Database connection failed. Check SQL Server and the connection string.");
    }
});


app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    RaceDayDbContext db,
    IPasswordHasher<User> passwordHasher) =>
{
    if (string.IsNullOrWhiteSpace(request.FirstName) ||
        string.IsNullOrWhiteSpace(request.LastName) ||
        string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new
        {
            message = "First name, last name, email, and password are required."
        });
    }

    if (request.Password.Length < 8)
    {
        return Results.BadRequest(new
        {
            message = "Password must be at least 8 characters long."
        });
    }

    var email = request.Email.Trim().ToLowerInvariant();

    var emailExists = await db.Users
        .AnyAsync(u => u.Email == email);

    if (emailExists)
    {
        return Results.Conflict(new
        {
            message = "An account with this email already exists."
        });
    }

    var user = new User
    {
        FirstName = request.FirstName.Trim(),
        LastName = request.LastName.Trim(),
        Email = email,
        RoleId = 3,
        Phone = request.Phone?.Trim(),
        ProfilePic = string.Empty
    };

    user.PasswordHash = passwordHasher.HashPassword(
        user,
        request.Password);

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/users/{user.UserId}",
        new AuthResponse
        {
            UserId = user.UserId,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = "Participant"
        });
});


app.MapPost("/api/auth/login", async (
    LoginRequest request,
    RaceDayDbContext db,
    IPasswordHasher<User> passwordHasher,
    IConfiguration configuration) =>
{
    if (string.IsNullOrWhiteSpace(request.Email) ||
        string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new
        {
            message = "Email and password are required."
        });
    }

    var email = request.Email.Trim().ToLowerInvariant();

    var user = await db.Users
        .FirstOrDefaultAsync(u => u.Email == email);

    if (user is null)
    {
        return Results.Unauthorized();
    }

    var passwordResult = passwordHasher.VerifyHashedPassword(
        user,
        user.PasswordHash,
        request.Password);

    if (passwordResult == PasswordVerificationResult.Failed)
    {
        return Results.Unauthorized();
    }

    var role = await db.Roles
        .AsNoTracking()
        .FirstOrDefaultAsync(r => r.RoleId == user.RoleId);

    if (role is null)
    {
        return Results.Problem(
            "The user's role could not be found.");
    }

    var jwtKey = configuration["Jwt:Key"];
    var jwtIssuer = configuration["Jwt:Issuer"];
    var jwtAudience = configuration["Jwt:Audience"];

    if (string.IsNullOrWhiteSpace(jwtKey) ||
        string.IsNullOrWhiteSpace(jwtIssuer) ||
        string.IsNullOrWhiteSpace(jwtAudience))
    {
        return Results.Problem(
            "JWT configuration is incomplete.");
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, role.RoleName),
        new Claim("role_id", user.RoleId.ToString())
    };

    var signingKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtKey));

    var credentials = new SigningCredentials(
        signingKey,
        SecurityAlgorithms.HmacSha256);

    var expires = DateTime.UtcNow.AddMinutes(
        configuration.GetValue<int>("Jwt:ExpiryMinutes", 60));

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: expires,
        signingCredentials: credentials);

    var tokenString = new JwtSecurityTokenHandler()
        .WriteToken(token);

    return Results.Ok(new AuthResponse
    {
        UserId = user.UserId,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Email = user.Email,
        Role = role.RoleName,
        Token = tokenString
    });
});


app.MapGet("/api/categories", async (RaceDayDbContext db) =>
{
    var categories = await db.Categories
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(categories);
})
.RequireAuthorization();

app.MapGet("/api/categories/{id:int}", async (int id, RaceDayDbContext db) =>
{
    var category = await db.Categories
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.CategoryId == id);

    return category is null
        ? Results.NotFound(new { message = "Category not found." })
        : Results.Ok(category);
})
.RequireAuthorization();

app.MapPost("/api/categories", async (Category newCategory, RaceDayDbContext db) =>
{
    var eventExists = await db.Events
        .AnyAsync(e => e.EventId == newCategory.EventId);

    if (!eventExists)
    {
        return Results.BadRequest(new { message = "Event not found." });
    }

    if (string.IsNullOrWhiteSpace(newCategory.CategoryName))
    {
        return Results.BadRequest(new { message = "Category name is required." });
    }

    db.Categories.Add(newCategory);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/categories/{newCategory.CategoryId}",
        newCategory);
})
.RequireAuthorization("OrganiserOrAdmin");

app.MapPut("/api/categories/{id:int}", async (int id, Category updatedCategory, RaceDayDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);

    if (category is null)
    {
        return Results.NotFound(new { message = "Category not found." });
    }

    var eventExists = await db.Events
        .AnyAsync(e => e.EventId == updatedCategory.EventId);

    if (!eventExists)
    {
        return Results.BadRequest(new { message = "Event not found." });
    }

    if (string.IsNullOrWhiteSpace(updatedCategory.CategoryName))
    {
        return Results.BadRequest(new { message = "Category name is required." });
    }

    category.EventId = updatedCategory.EventId;
    category.CategoryName = updatedCategory.CategoryName.Trim();
    category.Description = updatedCategory.Description;

    await db.SaveChangesAsync();

    return Results.Ok(category);
})
.RequireAuthorization("OrganiserOrAdmin");

app.MapDelete("/api/categories/{id:int}", async (int id, RaceDayDbContext db) =>
{
    var category = await db.Categories.FindAsync(id);

    if (category is null)
    {
        return Results.NotFound(new { message = "Category not found." });
    }

    db.Categories.Remove(category);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization("OrganiserOrAdmin");


app.MapGet("/api/enrolments", async (
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    if (user.IsInRole("Participant"))
    {
        var ownEnrolments = await db.Enrolments
            .AsNoTracking()
            .Where(e => e.Participant == userId)
            .ToListAsync();

        return Results.Ok(ownEnrolments);
    }

    var enrolments = await db.Enrolments
        .AsNoTracking()
        .ToListAsync();

    return Results.Ok(enrolments);
})
.RequireAuthorization();

app.MapGet("/api/enrolments/{id:int}", async (
    int id,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var enrolment = await db.Enrolments
        .AsNoTracking()
        .FirstOrDefaultAsync(e => e.EnrId == id);

    if (enrolment is null)
        return Results.NotFound(new { message = "Enrolment not found." });

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    if (enrolment.Participant != userId &&
        !user.IsInRole("Organiser") &&
        !user.IsInRole("Admin"))
    {
        return Results.Forbid();
    }

    return Results.Ok(enrolment);
})
.RequireAuthorization();


app.MapPost("/api/enrolments", async (
    Enrolment request,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Participant"))
        return Results.Forbid();

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var category = await db.Categories
        .FirstOrDefaultAsync(c =>
            c.CategoryId == request.CategoryId &&
            c.EventId == request.EventId);

    if (category is null)
        return Results.BadRequest(new
        {
            message = "Category not found or does not belong to this event."
        });

    var alreadyEnrolled = await db.Enrolments
        .AnyAsync(e => e.Participant == userId &&
                       e.EventId == request.EventId);

    if (alreadyEnrolled)
        return Results.Conflict(new
        {
            message = "Already enrolled in this event."
        });

    var enrolment = new Enrolment
    {
        Participant = userId,
        EventId = request.EventId,
        CategoryId = request.CategoryId,
        EnrDate = DateTime.UtcNow.Date,
        Status = "Pending"
    };

    db.Enrolments.Add(enrolment);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/enrolments/{enrolment.EnrId}",
        enrolment);
})
.RequireAuthorization();


app.MapPut("/api/enrolments/{id:int}", async (
    int id,
    Enrolment request,
    RaceDayDbContext db) =>
{
    var enrolment = await db.Enrolments.FindAsync(id);

    if (enrolment is null)
        return Results.NotFound(new { message = "Enrolment not found." });

    enrolment.Status = request.Status;

    await db.SaveChangesAsync();

    return Results.Ok(enrolment);
})
.RequireAuthorization("OrganiserOrAdmin");

app.MapDelete("/api/enrolments/{id:int}", async (
    int id,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var enrolment = await db.Enrolments.FindAsync(id);

    if (enrolment is null)
        return Results.NotFound(new { message = "Enrolment not found." });

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    if (enrolment.Participant != userId &&
        !user.IsInRole("Organiser") &&
        !user.IsInRole("Admin"))
    {
        return Results.Forbid();
    }

    db.Enrolments.Remove(enrolment);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization();


app.MapGet("/api/documents", async (
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    if (user.IsInRole("Admin") || user.IsInRole("Organiser"))
    {
        var allDocuments = await db.SupportingDocuments
            .AsNoTracking()
            .ToListAsync();

        return Results.Ok(allDocuments);
    }

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var documents = await db.SupportingDocuments
        .AsNoTracking()
        .Where(d => db.Enrolments.Any(e =>
            e.EnrId == d.EnrId && e.Participant == userId))
        .ToListAsync();

    return Results.Ok(documents);
})
.RequireAuthorization();

app.MapGet("/api/documents/{id:int}", async (
    int id,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var document = await db.SupportingDocuments
        .AsNoTracking()
        .FirstOrDefaultAsync(d => d.DocumentId == id);

    if (document is null)
        return Results.NotFound(new { message = "Document not found." });

    var isStaff = user.IsInRole("Admin") || user.IsInRole("Organiser");

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var isOwner = await db.Enrolments.AnyAsync(e =>
        e.EnrId == document.EnrId && e.Participant == userId);

    if (!isStaff && !isOwner)
        return Results.Forbid();

    return Results.Ok(document);
})
.RequireAuthorization();

app.MapPost("/api/documents", async (
    SupportingDocument request,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    if (!user.IsInRole("Participant"))
        return Results.Forbid();

    if (string.IsNullOrWhiteSpace(request.FileName) ||
        string.IsNullOrWhiteSpace(request.FilePath))
    {
        return Results.BadRequest(new
        {
            message = "File name and file path are required."
        });
    }

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var enrolment = await db.Enrolments.FirstOrDefaultAsync(e =>
        e.EnrId == request.EnrId && e.Participant == userId);

    if (enrolment is null)
    {
        return Results.BadRequest(new
        {
            message = "Enrolment not found for this participant."
        });
    }

    var document = new SupportingDocument
    {
        EnrId = request.EnrId,
        FileName = request.FileName.Trim(),
        FilePath = request.FilePath.Trim(),
        Status = "Pending"
    };

    db.SupportingDocuments.Add(document);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/api/documents/{document.DocumentId}",
        document);
})
.RequireAuthorization();

app.MapPut("/api/documents/{id:int}", async (
    int id,
    SupportingDocument request,
    RaceDayDbContext db) =>
{
    var document = await db.SupportingDocuments.FindAsync(id);

    if (document is null)
        return Results.NotFound(new { message = "Document not found." });

    if (string.IsNullOrWhiteSpace(request.Status))
    {
        return Results.BadRequest(new
        {
            message = "Document status is required."
        });
    }

    document.Status = request.Status.Trim();

    await db.SaveChangesAsync();

    return Results.Ok(document);
})
.RequireAuthorization("OrganiserOrAdmin");

app.MapDelete("/api/documents/{id:int}", async (
    int id,
    RaceDayDbContext db) =>
{
    var document = await db.SupportingDocuments.FindAsync(id);

    if (document is null)
        return Results.NotFound(new { message = "Document not found." });

    db.SupportingDocuments.Remove(document);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization("OrganiserOrAdmin");


app.MapGet("/api/results", async (
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    if (user.IsInRole("Organiser") || user.IsInRole("Admin"))
    {
        var allResults = await db.RaceResults
            .AsNoTracking()
            .ToListAsync();

        return Results.Ok(allResults);
    }

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var results = await db.RaceResults
        .AsNoTracking()
        .Where(r => r.Published &&
            db.Enrolments.Any(e =>
                e.EnrId == r.EnrId && e.Participant == userId))
        .ToListAsync();

    return Results.Ok(results);
})
.RequireAuthorization();

app.MapGet("/api/results/{id:int}", async (
    int id,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var result = await db.RaceResults
        .AsNoTracking()
        .FirstOrDefaultAsync(r => r.ResultId == id);

    if (result is null)
        return Results.NotFound(new { message = "Result not found." });

    var isStaff = user.IsInRole("Organiser") || user.IsInRole("Admin");

    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var isOwner = await db.Enrolments.AnyAsync(e =>
        e.EnrId == result.EnrId && e.Participant == userId);

    if (!isStaff && (!isOwner || !result.Published))
        return Results.Forbid();

    return Results.Ok(result);
})
.RequireAuthorization();

app.MapPost("/api/results", async (
    RaceResult request,
    RaceDayDbContext db) =>
{
    var enrolmentExists = await db.Enrolments
        .AnyAsync(e => e.EnrId == request.EnrId);

    if (!enrolmentExists)
        return Results.BadRequest(new { message = "Enrolment not found." });

    if (request.Position is <= 0)
        return Results.BadRequest(new { message = "Position must be positive." });

    if (request.FinishTime.HasValue &&
    request.FinishTime.Value < TimeSpan.Zero)
        return Results.BadRequest(new { message = "Finish time cannot be negative." });

    var result = new RaceResult
    {
        EnrId = request.EnrId,
        FinishTime = request.FinishTime,
        Position = request.Position,
        Published = request.Published
    };

    db.RaceResults.Add(result);
    await db.SaveChangesAsync();

    return Results.Created($"/api/results/{result.ResultId}", result);
})
.RequireAuthorization("OrganiserOrAdmin");

app.MapPut("/api/results/{id:int}", async (
    int id,
    RaceResult request,
    RaceDayDbContext db) =>
{
    var result = await db.RaceResults.FindAsync(id);

    if (result is null)
        return Results.NotFound(new { message = "Result not found." });

    if (request.Position is <= 0)
        return Results.BadRequest(new { message = "Position must be positive." });

    if (request.FinishTime.HasValue &&
    request.FinishTime.Value < TimeSpan.Zero)
        return Results.BadRequest(new { message = "Finish time cannot be negative." });

    result.FinishTime = request.FinishTime;
    result.Position = request.Position;
    result.Published = request.Published;

    await db.SaveChangesAsync();

    return Results.Ok(result);
})
.RequireAuthorization("OrganiserOrAdmin");

app.MapDelete("/api/results/{id:int}", async (
    int id,
    RaceDayDbContext db) =>
{
    var result = await db.RaceResults.FindAsync(id);

    if (result is null)
        return Results.NotFound(new { message = "Result not found." });

    db.RaceResults.Remove(result);
    await db.SaveChangesAsync();

    return Results.NoContent();
})
.RequireAuthorization("OrganiserOrAdmin");

app.MapGet("/api/users/me", async (
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var profile = await db.Users
        .AsNoTracking()
        .Where(u => u.UserId == userId)
        .Select(u => new
        {
            u.UserId,
            u.FirstName,
            u.LastName,
            u.Email,
            u.Phone,
            u.ProfilePic,
            u.RoleId
        })
        .FirstOrDefaultAsync();

    return profile is null
        ? Results.NotFound(new { message = "Profile not found." })
        : Results.Ok(profile);
})
.RequireAuthorization();

app.MapPut("/api/users/me", async (
    User request,
    RaceDayDbContext db,
    ClaimsPrincipal user) =>
{
    var userId = int.Parse(
        user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    var existingUser = await db.Users.FindAsync(userId);

    if (existingUser is null)
        return Results.NotFound(new { message = "Profile not found." });

    if (string.IsNullOrWhiteSpace(request.FirstName) ||
        string.IsNullOrWhiteSpace(request.LastName) ||
        string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest(new
        {
            message = "First name, last name, and email are required."
        });
    }

    var email = request.Email.Trim().ToLowerInvariant();

    var emailTaken = await db.Users.AnyAsync(u =>
        u.Email == email && u.UserId != userId);

    if (emailTaken)
        return Results.Conflict(new
        {
            message = "That email is already in use."
        });

    existingUser.FirstName = request.FirstName.Trim();
    existingUser.LastName = request.LastName.Trim();
    existingUser.Email = email;
    existingUser.Phone = request.Phone?.Trim();
    existingUser.ProfilePic = request.ProfilePic?.Trim();

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        existingUser.UserId,
        existingUser.FirstName,
        existingUser.LastName,
        existingUser.Email,
        existingUser.Phone,
        existingUser.ProfilePic,
        existingUser.RoleId
    });
})
.RequireAuthorization();

app.Run();

public partial class Program { }