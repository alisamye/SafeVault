using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/forbidden";
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
var databasePath = Path.Combine(builder.Environment.ContentRootPath, "safevault.db");
builder.Services.AddSingleton(new UserRepository($"Data Source={databasePath}"));
builder.Services.AddSingleton<AuthService>();

var app = builder.Build();
var repository = app.Services.GetRequiredService<UserRepository>();
await repository.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/submit", async ([FromForm] UserForm form, UserRepository users) =>
{
    var validation = InputValidator.Validate(form.Username, form.Email);
    if (!validation.IsValid)
    {
        return Results.BadRequest(new { errors = validation.Errors });
    }

    await users.AddAsync(form.Username.Trim(), form.Email.Trim());
    return Results.Ok(new { message = "User saved." });
}).DisableAntiforgery();

app.MapPost("/login", async ([FromForm] LoginForm form, AuthService auth, HttpContext context) =>
{
    var user = await auth.AuthenticateAsync(form.Username, form.Password);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.Username),
        new Claim(ClaimTypes.Role, user.Role)
    };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Ok(new { message = "Logged in." });
}).DisableAntiforgery();

app.MapPost("/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Logged out." });
});

app.MapGet("/admin", [Authorize(Policy = "AdminOnly")] () => Results.Ok(new { message = "Admin dashboard." }));
app.MapGet("/forbidden", () => Results.StatusCode(StatusCodes.Status403Forbidden));

app.Run();

public partial class Program;

public sealed record UserForm(string Username, string Email);
public sealed record LoginForm(string Username, string Password);
