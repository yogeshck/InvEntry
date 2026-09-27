using Microsoft.Data.SqlClient;

namespace InvEntry.Test;

[TestFixture]
[Category("SqlServerIntegration")]
public sealed class ProductTransactionSqlIntegrationTests
{
    private const string ConnectionString =
        "Server=.\\SQLEXPRESS;Database=tempdb;Integrated Security=True;TrustServerCertificate=True";

    [Test]
    public async Task FilteredUniqueIndex_AllowsHistoricalNullsAndDistinctLines_BlocksDuplicateLine()
    {
        string table = $"PT_STAGE2_{Guid.NewGuid():N}";
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();

        try
        {
            await Execute(connection, $"""
                CREATE TABLE dbo.[{table}]
                (
                    GKEY int IDENTITY PRIMARY KEY,
                    DOCUMENT_TYPE varchar(50) NULL,
                    TRANSACTION_TYPE varchar(50) NULL,
                    SOURCE_LINE_GKEY int NULL
                );
                CREATE UNIQUE INDEX UX_{table}
                    ON dbo.[{table}](DOCUMENT_TYPE, TRANSACTION_TYPE, SOURCE_LINE_GKEY)
                    WHERE SOURCE_LINE_GKEY IS NOT NULL
                      AND DOCUMENT_TYPE = 'GRN'
                      AND TRANSACTION_TYPE = 'Receipt';
                """);

            await Execute(connection, $"""
                INSERT dbo.[{table}] VALUES ('GRN','Receipt',NULL);
                INSERT dbo.[{table}] VALUES ('GRN','Receipt',NULL);
                INSERT dbo.[{table}] VALUES ('GRN','Receipt',501);
                INSERT dbo.[{table}] VALUES ('GRN','Receipt',502);
                """);

            var exception = Assert.ThrowsAsync<SqlException>(async () =>
                await Execute(connection,
                    $"INSERT dbo.[{table}] VALUES ('GRN','Receipt',501);"));

            Assert.That(exception!.Number, Is.AnyOf(2601, 2627));
        }
        finally
        {
            await Execute(connection, $"DROP TABLE IF EXISTS dbo.[{table}];");
        }
    }

    [Test]
    public async Task FilteredUniqueIndex_ConcurrentDuplicateRequests_OnlyOneCommits()
    {
        string table = $"PT_STAGE2_{Guid.NewGuid():N}";
        await using var setup = new SqlConnection(ConnectionString);
        await setup.OpenAsync();

        try
        {
            await Execute(setup, $"""
                CREATE TABLE dbo.[{table}]
                (
                    GKEY int IDENTITY PRIMARY KEY,
                    DOCUMENT_TYPE varchar(50) NULL,
                    TRANSACTION_TYPE varchar(50) NULL,
                    SOURCE_LINE_GKEY int NULL
                );
                CREATE UNIQUE INDEX UX_{table}
                    ON dbo.[{table}](DOCUMENT_TYPE, TRANSACTION_TYPE, SOURCE_LINE_GKEY)
                    WHERE SOURCE_LINE_GKEY IS NOT NULL
                      AND DOCUMENT_TYPE = 'GRN'
                      AND TRANSACTION_TYPE = 'Receipt';
                """);

            var results = await Task.WhenAll(
                TryInsert(table, 601),
                TryInsert(table, 601));

            Assert.Multiple(() =>
            {
                Assert.That(results.Count(x => x == 0), Is.EqualTo(1));
                Assert.That(results.Count(x => x is 2601 or 2627), Is.EqualTo(1));
            });
        }
        finally
        {
            await Execute(setup, $"DROP TABLE IF EXISTS dbo.[{table}];");
        }
    }

    private static async Task<int> TryInsert(string table, int sourceLineGkey)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        try
        {
            await Execute(connection,
                $"INSERT dbo.[{table}] VALUES ('GRN','Receipt',{sourceLineGkey});");
            return 0;
        }
        catch (SqlException ex)
        {
            return ex.Number;
        }
    }

    private static async Task Execute(SqlConnection connection, string commandText)
    {
        await using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync();
    }
}
