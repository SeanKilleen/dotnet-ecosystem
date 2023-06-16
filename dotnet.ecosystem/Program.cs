// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using Akka.Event;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace dotnet.ecosystem;

public class Messages
{
    public record FindProjects(string Path);
    public record ProcessProject(FileInfo File);
}

public class CsProjectProcessor : ReceiveActor
{
    ILoggingAdapter _log = Context.GetLogger();
    public CsProjectProcessor()
    {

        Receive<Messages.ProcessProject>(msg =>
        {
            _log.Info("Processing {FilePath}", msg.File.FullName);

            if (!msg.File.Exists)
            {
                _log.Warning("File {FilePath} did not exist", msg.File.FullName);
            }
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

        finder.Tell(new Messages.FindProjects(ScanFolder));

        Console.ReadLine();

        return await Task.FromResult<int>(0);
    }
}