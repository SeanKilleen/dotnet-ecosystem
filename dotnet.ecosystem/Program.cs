// See https://aka.ms/new-console-template for more information
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp<EcosystemCommand>();

return app.Run(args);

internal sealed class EcosystemCommand : Command<EcosystemCommand.Settings>
{

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        AnsiConsole.MarkupLine($"[red]R[/][green]G[/][blue]B[/] - Scan Folder is [yellow]{settings.ScanFolder}[/]");
        return 0;
    }

    public sealed class Settings : CommandSettings
    {
        [CommandArgument(0, "<scanFolder>")]
        public string ScanFolder { get; set; }
    }
}