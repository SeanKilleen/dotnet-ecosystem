// See https://aka.ms/new-console-template for more information

using Akka.Actor;
using Akka.Event;
using Microsoft.Build.Construction;

namespace dotnet.ecosystem;

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

            var packageReferences = projectFile.ItemDefinitions.Where(x => x.ElementName == "PackageReference").ToList();

            _log.Info("Found {PackageReferenceCount} package references for {ProjectName}", packageReferences.Count, msg.File.Name);


        });
    }
}
