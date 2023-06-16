// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Event;

namespace dotnet.ecosystem;

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
