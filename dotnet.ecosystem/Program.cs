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
public class Program
{
    public static async Task<int> Main(string[] args)
    {
        var logger = new LoggerConfiguration()
        .WriteTo.ColoredConsole()
        .WriteTo.Seq("http://localhost:5341")
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