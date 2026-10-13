using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

var connectionString =
    builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("Connection string Postgres is not configured");

var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder
    .Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = signingKey,
        };
    });

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddIdentity<User, IdentityRole>().AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

await SeedAsync(app);

app.Run();

static async Task SeedAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    foreach (var role in new[] { "Librarian", "Member" })
    {
        if (await roles.RoleExistsAsync(role))
            continue;

        var created = await roles.CreateAsync(new IdentityRole(role));
        if (!created.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not create the {role} role: {Describe(created)}"
            );
        }
    }

    var librarian = app.Configuration.GetSection("Librarian");
    var email = librarian["Email"];
    var password = librarian["Password"];
    var firstName = librarian["FirstName"];
    var lastName = librarian["LastName"];

    if (
        string.IsNullOrWhiteSpace(email)
        || string.IsNullOrWhiteSpace(password)
        || string.IsNullOrWhiteSpace(firstName)
        || string.IsNullOrWhiteSpace(lastName)
    )
    {
        throw new InvalidOperationException(
            "Set Librarian__Email, Librarian__Password, Librarian__FirstName, and Librarian__LastName."
        );
    }

    if (await users.FindByEmailAsync(email) is not null)
        return;

    var user = new User
    {
        UserName = email,
        Email = email,
        FirstName = firstName,
        LastName = lastName,
        EmailConfirmed = true,
        CreatedAt = DateTime.UtcNow,
    };

    var result = await users.CreateAsync(user, password);
    if (!result.Succeeded)
    {
        throw new InvalidOperationException($"Could not seed the librarian: {Describe(result)}");
    }

    var assigned = await users.AddToRoleAsync(user, "Librarian");
    if (!assigned.Succeeded)
    {
        throw new InvalidOperationException(
            $"Could not assign the Librarian role: {Describe(assigned)}"
        );
    }
}

static string Describe(IdentityResult result) =>
    string.Join(", ", result.Errors.Select(error => error.Description));
