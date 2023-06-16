// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Event;
using Neo4j.Driver;

namespace dotnet.ecosystem;

public class GraphActor : ReceiveActor
{
    ILoggingAdapter _log = Context.GetLogger();
    // TODO: Obviously fix this
    IDriver _driver = GraphDatabase.Driver(
    "bolt://localhost",
    AuthTokens.Basic(
        "neo4j",
        "admin123"
    )
);
    public GraphActor()
    {
        ReceiveAsync<Messages.AddProjectToGraph>(async msg =>
        {
            _log.Info("Adding {ProjectFileName} to graph", msg.ProjectFileName);

            await using var session = _driver.AsyncSession();

            await session.ExecuteWriteAsync(async tr =>
            {
                var cursor = await tr.RunAsync(@"
                    MERGE (p:Project { path: $path, name: $name })
                    return p", new { path = msg.Directory, name = msg.ProjectFileName });
            });
        });

        ReceiveAsync<Messages.SpecifyTargets>(async msg =>
        {
            await using var session = _driver.AsyncSession();

            foreach (var target in msg.Targets)
            {
                await session.ExecuteWriteAsync(async tr =>
                {
                    var cursor = await tr.RunAsync(@"
                    MATCH (p:Project { path: $path, name: $name })
                    MERGE(t:Target { name: $target })
                    MERGE (p)-[:TARGETS]->(t)
                    return p", new { path = msg.Directory, name = msg.ProjectFileName, target = target });
                });
            }
        });

        ReceiveAsync<Messages.SpecifySdk>(async msg =>
        {
            if (string.IsNullOrWhiteSpace(msg.SDK)) { return; }
            await using var session = _driver.AsyncSession();
            await session.ExecuteWriteAsync(async tr =>
            {
                var cursor = await tr.RunAsync(@"
                    MATCH (p:Project { path: $path, name: $name })
                    MERGE (s:SDK {name: $sdk})
                    MERGE (p)-[:HAS_SDK]->(s)
                    return p", new { path = msg.Directory, name = msg.ProjectFileName, sdk = msg.SDK });
            });
        });

        ReceiveAsync<Messages.SpecifyPackages>(async msg =>
        {
            await using var session = _driver.AsyncSession();

            foreach (var package in msg.Packages)
            {
                await session.ExecuteWriteAsync(async tr =>
                {
                    var cursor = await tr.RunAsync(@"
                    MATCH (p:Project { path: $path, name: $name })
                    MERGE(n:NugetPackage { name: $packageName })
                    MERGE (p)-[:USES { version: $packageVersion }]->(n)
                    return p", new { path = msg.Directory, name = msg.ProjectFileName, packageName = package.Name, packageVersion = package.Version });
                });
            }
        });

        ReceiveAsync<Messages.SpecifyAppSettings>(async msg =>
        {
            await using var session = _driver.AsyncSession();

            foreach (var setting in msg.Settings)
            {
                await session.ExecuteWriteAsync(async tr =>
                {
                    var cursor = await tr.RunAsync(@"
                    MATCH (p:Project { path: $path, name: $name })
                    MERGE(s:AppSetting { name: $settingName })
                    MERGE (p)-[:HAS_SETTING { value: $settingValue }]->(s)
                    return p", new { path = msg.Directory, name = msg.ProjectFileName, settingName = setting.Name, settingValue = setting.Value });
                });
            }
        });

        ReceiveAsync<Messages.SpecifyConnectionStrings>(async msg =>
        {
            await using var session = _driver.AsyncSession();

            foreach (var connectionString in msg.ConnectionStrings)
            {
                await session.ExecuteWriteAsync(async tr =>
                {
                    var cursor = await tr.RunAsync(@"
                    MATCH (p:Project { path: $path, name: $name })
                    MERGE(c:ConnectionString { name: $connectionStringName })
                    MERGE (p)-[:HAS_CONNECTION_STRING { value: $connectionStringValue, provider: $connectionStringProvider }]->(c)
                    return p", new { path = msg.Directory, name = msg.ProjectFileName, connectionStringName = connectionString.Name, connectionStringValue = connectionString.Value, connectionStringProvider = connectionString.Provider ?? string.Empty });
                });
            }
        });

    }
}
