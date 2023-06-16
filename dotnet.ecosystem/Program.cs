// See https://aka.ms/new-console-template for more information
using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
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
    public CsProjectProcessor()
    {
        Receive<Messages.ProcessProject>(msg =>
        {
            AnsiConsole.Markup($"Processing [yellow]{msg.File.FullName}[/]");
        });
    }
}
public class ProjFinderActor : ReceiveActor
{
    public ProjFinderActor()
    {
        var csProjProcessor = Context.ActorSelection("../csProjProcessor");
        Receive<Messages.FindProjects>(msg =>
        {
            AnsiConsole.WriteLine($"Checking {msg.Path} for files");
            var files = Directory.GetFiles(msg.Path, "*.csproj", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                AnsiConsole.WriteLine($"Found {file}");
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
        string ScanFolder = args[0]; // TODO: Real command parsing & syntax

        AnsiConsole.MarkupLine($"[red]R[/][green]G[/][blue]B[/] - Scan Folder is [yellow]{ScanFolder}[/]");

        var system = ActorSystem.Create("dotnet-ecosystem");

        var finder = system.ActorOf<ProjFinderActor>("projFinder");
        var processor = system.ActorOf<CsProjectProcessor>("csProjProcessor");

        finder.Tell(new Messages.FindProjects(ScanFolder));

        Console.ReadLine();

        return await Task.FromResult<int>(0);
    }
}