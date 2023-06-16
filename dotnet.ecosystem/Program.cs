// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using Akka.Event;
using Microsoft.Build.Construction;
using Neo4j.Driver;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dotnet.ecosystem;

public class Messages
{
    public record FindProjects(string Path);
    public record ProcessProject(FileInfo File);
    public record AddProjectToGraph(string Directory, string ProjectFileName);
    public record SpecifyTargets(string Directory, string ProjectFileName, IReadOnlyList<string> Targets);
}

public class GraphActor : ReceiveActor
{
    ILoggingAdapter _log = Context.GetLogger();
    // TODO: Obviously fix this
    IDriver _driver = GraphDatabase.Driver(
    "bolt://localhost",
    AuthTokens.Basic(
        "neo4j",
        "admin"
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
    }
}
public class CsProjectProcessor : ReceiveActor
{
    ILoggingAdapter _log = Context.GetLogger();
    ActorSelection _graphActor = Context.ActorSelection("../graphActor");
    public CsProjectProcessor()
    {

        Receive<Messages.ProcessProject>(msg =>
        {
            _log.Info("Processing {FilePath}", msg.File.FullName);

            if (!msg.File.Exists)
            {
                _log.Warning("File {FilePath} did not exist", msg.File.FullName);
            }

            _graphActor.Tell(new Messages.AddProjectToGraph(msg.File.DirectoryName, msg.File.Name));

            // TODO: Own actor etc.
            var projectFile = ProjectRootElement.Open(msg.File.FullName);

            List<string> targetFrameworks = new();

            var targetProperties = projectFile.Properties.Where(p => p.Name == "TargetFramework" || p.Name == "TargetFrameworks");
            foreach (var targetProperty in targetProperties)
            {
                targetFrameworks.AddRange(targetProperty.Value.Split(';'));
            }

            _graphActor.Tell(new Messages.SpecifyTargets(msg.File.DirectoryName, msg.File.Name, targetFrameworks));


        });
    }
}
public class ProjFinderActor : ReceiveActor
{
    ILoggingAdapter _log = Context.GetLogger();
    public ProjFinderActor()
    {
        var csProjProcessor = Context.ActorSelection("../csProjProcessor");
        Receive<Messages.FindProjects>(msg =>
        {
            _log.Info("Checking {FolderPath} for .csproj files", msg.Path);
            var files = Directory.GetFiles(msg.Path, "*.csproj", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                csProjProcessor.Tell(new Messages.ProcessProject(fileInfo));
            }
        });
    }

}
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var logger = new LoggerConfiguration()
        .WriteTo.ColoredConsole()
        .MinimumLevel.Information()
        .CreateLogger();

        Serilog.Log.Logger = logger;

        string ScanFolder = args[0]; // TODO: Real command parsing & syntax

        AnsiConsole.MarkupLine($"[red]R[/][green]G[/][blue]B[/] - Scan Folder is [yellow]{ScanFolder}[/]");

        var system = ActorSystem.Create("dotnet-ecosystem", "akka { loglevel=INFO,  loggers=[\"Akka.Logger.Serilog.SerilogLogger, Akka.Logger.Serilog\"]}");

        var finder = system.ActorOf<ProjFinderActor>("projFinder");
        var processor = system.ActorOf<CsProjectProcessor>("csProjProcessor");
        var graphActor = system.ActorOf<GraphActor>("graphActor");

        finder.Tell(new Messages.FindProjects(ScanFolder));

        Console.ReadLine();

        return await Task.FromResult<int>(0);
    }
}