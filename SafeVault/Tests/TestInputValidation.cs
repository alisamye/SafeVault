using NUnit.Framework;
using Microsoft.Data.Sqlite;

[TestFixture]
public class TestInputValidation
{
	[Test]
	public void RejectsSqlInjectionPayloadInUsername()
	{
		var result = InputValidator.Validate("' OR 1=1 --", "user@example.com");

		Assert.That(result.IsValid, Is.False);
	}

	[Test]
	public void RejectsXssPayloadInUsername()
	{
		var result = InputValidator.Validate("<script>alert(1)</script>", "user@example.com");

		Assert.That(result.IsValid, Is.False);
	}

	[Test]
	public async Task InsertsPayloadAsDataUsingParameters()
	{
		await using var connection = new SqliteConnection("Data Source=SafeVaultTest;Mode=Memory;Cache=Shared");
		await connection.OpenAsync();
		var repository = new UserRepository(connection.ConnectionString);
		await repository.InitializeAsync();

		await repository.AddAsync("alice'; DROP TABLE Users; --", "alice@example.com");

		await using var command = connection.CreateCommand();
		command.CommandText = "SELECT COUNT(*) FROM Users;";
		Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo(1L));
	}
}