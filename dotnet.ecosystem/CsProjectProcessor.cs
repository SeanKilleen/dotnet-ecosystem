// See https://aka.ms/new-console-template for more information

using System.Xml.Linq;
using Akka.Actor;
using Akka.Event;
using Microsoft.Build.Construction;
using Neo4j.Driver;

namespace dotnet.ecosystem;

public class CsProjectProcessor : ReceiveActor
{
    ILoggingAdapter _log = Context.GetLogger();
    ActorSelection _graphActor = Context.ActorSelection("../graphActor");

    public CsProjectProcessor()
    {

        ReceiveAsync<Messages.ProcessProject>(async msg =>
        {
            _log.Info("Processing {FilePath}", msg.File.FullName);

            if (!msg.File.Exists)
            {
                _log.Warning("File {FilePath} did not exist", msg.File.FullName);
            }

            _graphActor.Tell(new Messages.AddProjectToGraph(msg.File.DirectoryName, msg.File.Name));

            // TODO: Split into actors etc.
            List<string> targetFrameworks = new();
            var projectFile = ProjectRootElement.Open(msg.File.FullName);

            var targetProperties = projectFile.Properties.Where(p => p.Name == "TargetFramework" || p.Name == "TargetFrameworks");
            foreach (var targetProperty in targetProperties)
            {
                targetFrameworks.AddRange(targetProperty.Value.Split(';'));
            }

            _graphActor.Tell(new Messages.SpecifyTargets(msg.File.DirectoryName, msg.File.Name, targetFrameworks));

            _graphActor.Tell(new Messages.SpecifySdk(msg.File.DirectoryName, msg.File.Name, projectFile.Sdk));

            var xmlText = await File.ReadAllTextAsync(msg.File.FullName);
            var xDoc = XDocument.Parse(xmlText);

            var packageRefElements = xDoc.Descendants("PackageReference");

            List<PackageReference> packageReferences = new();
            foreach (var packageRefElement in packageRefElements)
            {
                var name = packageRefElement.AttributeCaseInsensitive("Include");
                var version = packageRefElement.AttributeCaseInsensitive("Version");

                if (name == null)
                {
                    _log.Warning("Name was null when retrieving a PackageReference as part of {ProjectName}", msg.File.Name);
                }

                if (version == null)
                {
                    _log.Warning("Version was null when retrieving a PackageReference as part of {ProjectName}", msg.File.Name);
                }

                if (name != null && version != null)
                {
                    packageReferences.Add(new PackageReference(name.Value, version.Value));
                }
            }

            var packagesConfigPath = Path.Combine(msg.File.DirectoryName, "packages.config");
            if (File.Exists(packagesConfigPath))
            {
                _log.Info("Found packages.config for {ProjectName}; processing.", msg.File.Name);

                var packagesConfigXmlText = await File.ReadAllTextAsync(packagesConfigPath);
                var packagesConfigXDoc = XDocument.Parse(packagesConfigXmlText);

                var packages = packagesConfigXDoc.Descendants().Where(x => string.Equals(x.Name.LocalName, "package", StringComparison.InvariantCultureIgnoreCase));

                foreach (var package in packages)
                {
                    var name = package.AttributeCaseInsensitive("id");
                    var version = package.AttributeCaseInsensitive("version");

                    if (name == null)
                    {
                        _log.Warning("Name was null when retrieving a packages.config reference as part of {ProjectName}", msg.File.Name);
                    }

                    if (version == null)
                    {
                        _log.Warning("Version was null when retrieving a packages.config reference as part of {ProjectName}", msg.File.Name);
                    }

                    if (name != null && version != null)
                    {
                        packageReferences.Add(new PackageReference(name.Value, version.Value));
                    }
                }
            }


            _log.Info("Found {PackageReferenceCount} package references for {ProjectName}", packageReferences.Count, msg.File.Name);

            _graphActor.Tell(new Messages.SpecifyPackages(msg.File.DirectoryName, msg.File.Name, packageReferences));

        });
    }
}

public record PackageReference(string Name, string Version);
