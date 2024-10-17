using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Load configuration from secrets file if it exists
builder.Configuration.AddJsonFile("appsettings.Secrets.json", optional: true, reloadOnChange: true);

// Get JWT configuration from appsettings
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

if (string.IsNullOrEmpty(jwtKey) || string.IsNullOrEmpty(jwtIssuer) || string.IsNullOrEmpty(jwtAudience))
{
    throw new InvalidOperationException("JWT Key, Issuer, or Audience is missing in the configuration.");
}

// Add Authentication and Authorization services
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("Token validated successfully.");
                return Task.CompletedTask;
            }
        };
    });


// Add Authorization service
builder.Services.AddAuthorization();

// Add DB context (just to make sure we also have DB context set)
builder.Services.AddDbContext<UserContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));


var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/register", async (UserContext db, UserDto userDto) =>
{
    var userExists = await db.Users.AnyAsync(u => u.Username == userDto.Username || u.Email == userDto.Email);
    if (userExists)
        return Results.BadRequest("User already exists.");

    var user = new User
    {
        Username = userDto.Username,
        Email = userDto.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
        FirstName = userDto.FirstName,
        LastName = userDto.LastName,
        PhoneNumber = userDto.PhoneNumber
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok("User registered successfully.");
});


app.MapPost("/login", async (UserContext db, UserDto userDto) =>
{
    var user = await db.Users.SingleOrDefaultAsync(u => u.Username == userDto.Username);
    if (user == null || !BCrypt.Net.BCrypt.Verify(userDto.Password, user.PasswordHash))
        return Results.BadRequest("Invalid username or password.");

    var token = GenerateJwtToken(user);
    return Results.Ok(new { token });
});

app.MapPost("/announce", [Authorize] async (UserContext db, ClaimsPrincipal userPrincipal, [FromBody] AnnouncementRequest request) =>
{
    // Extract the username from ClaimTypes.NameIdentifier
    var username = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    if (string.IsNullOrEmpty(username))
    {
        Console.WriteLine("Username could not be extracted from JWT.");
        return Results.Unauthorized();
    }

    // Find the user in the database
    var user = await db.Users.SingleOrDefaultAsync(u => u.Username == username);
    if (user == null)
    {
        return Results.NotFound("User not found.");
    }

    // Validate the role input
    if (request.Role != "driver" && request.Role != "passenger")
    {
        return Results.BadRequest("Invalid role. Must be 'driver' or 'passenger'.");
    }

    // Check if the user has already made an announcement today
    var today = DateTime.UtcNow.Date;
    var existingAnnouncement = await db.Announcements
        .FirstOrDefaultAsync(a => a.UserId == user.Id && a.Date == today);

    if (existingAnnouncement != null)
    {
        return Results.BadRequest("You have already made an announcement today.");
    }

    // Create a new announcement for the current day
    var announcement = new Announcement
    {
        UserId = user.Id,
        Role = request.Role,
        Date = today // Store the announcement for today's date
    };

    db.Announcements.Add(announcement);
    await db.SaveChangesAsync();

    return Results.Ok($"User announced as {request.Role} for today.");
});


// Get all announcements for today (only accessible to authorized users)
app.MapGet("/announcements/today", [Authorize] async (UserContext db) =>
{
    var today = DateTime.UtcNow.Date; // Get today's date
    var announcementsForToday = await db.Announcements
        .Where(a => a.Date == today)  // Filter announcements for today's date
        .Include(a => a.User)  // Include related user information
        .ToListAsync();

    if (!announcementsForToday.Any())
    {
        return Results.NotFound("No announcements for today.");
    }

    // Optionally, return only relevant fields instead of the entire Announcement object
    var response = announcementsForToday.Select(a => new 
    {
        Username = a.User.Username,
        Role = a.Role,
        Date = a.Date
    });

    return Results.Ok(response);
});



// Just to check if the current token is valid
app.MapGet("/check", [Authorize] () => "Token is valid!");

app.Run();

// JWT token generation
string GenerateJwtToken(User user)
{
    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.Username),  // Set the Name claim
        new Claim(JwtRegisteredClaimNames.Sub, user.Username),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.Now.AddMinutes(30),
        signingCredentials: creds
    );

    return new JwtSecurityTokenHandler().WriteToken(token);
}

