using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

[TestFixture]
public sealed class AuthenticationTests
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public async Task SetUp()
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "SafeVaultTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.UseSetting(WebHostDefaults.ContentRootKey, contentRoot));
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var users = _factory.Services.GetRequiredService<UserRepository>();
        await users.AddUserAsync("regular-user", "regular@example.com", AuthService.HashPassword("user-password"));
        await users.AddUserAsync("admin-user", "admin@example.com", AuthService.HashPassword("admin-password"), "admin");
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Test]
    public async Task InvalidLoginIsRejected()
    {
        var response = await LoginAsync("regular-user", "wrong-password");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task AnonymousUserCannotAccessAdminDashboard()
    {
        var response = await _client.GetAsync("/admin");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task RegularUserCannotAccessAdminDashboard()
    {
        var login = await LoginAsync("regular-user", "user-password");
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var response = await _client.GetAsync("/admin");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AdminUserCanAccessAdminDashboard()
    {
        var login = await LoginAsync("admin-user", "admin-password");
        Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var response = await _client.GetAsync("/admin");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    private async Task<HttpResponseMessage> LoginAsync(string username, string password)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Username"] = username,
            ["Password"] = password
        });
        return await _client.PostAsync("/login", content);
    }
}