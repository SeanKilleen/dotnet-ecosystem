using System.Xml.Linq;
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

            List<PackageReference> packageReferences = new();

            var extractedPackageRefs = await ExtractPackageFromXMLFile(msg.File.FullName, "PackageReference", "Include", "Version");
            packageReferences.AddRange(extractedPackageRefs);

            var packagesConfigPath = Path.Combine(msg.File.DirectoryName, "packages.config");
            if (File.Exists(packagesConfigPath))
            {
                _log.Info("Found packages.config for {ProjectName}; processing.", msg.File.Name);

                var extractedPackages = await ExtractPackageFromXMLFile(packagesConfigPath, "package", "id", "version");
                packageReferences.AddRange(extractedPackages);
            }


            _log.Info("Found {PackageReferenceCount} package references for {ProjectName}", packageReferences.Count, msg.File.Name);

            _graphActor.Tell(new Messages.SpecifyPackages(msg.File.DirectoryName, msg.File.Name, packageReferences));

            var appConfigPath = Path.Combine(msg.File.DirectoryName, "app.config");
            if (File.Exists(appConfigPath))
            {
                _log.Info("Found app.config for {ProjectName}; processing.", msg.File.Name);

            }

        });
    }

    private async Task<List<PackageReference>> ExtractPackageFromXMLFile(string path, string elementName, string idAttributeName,
        string versionAttributeName)
    {
        List<PackageReference> result = new();

        var xmlText = await File.ReadAllTextAsync(path);
        var xDoc = XDocument.Parse(xmlText);

        var packages = xDoc.Descendants().Where(x => string.Equals(x.Name.LocalName, elementName, StringComparison.InvariantCultureIgnoreCase));

        foreach (var package in packages)
        {
            var name = package.AttributeCaseInsensitive(idAttributeName);
            var version = package.AttributeCaseInsensitive(versionAttributeName);

            if (name == null)
            {
                _log.Warning("Name was null when retrieving a packages.config reference as part of {ProjectPath}", path);
            }

            if (version == null)
            {
                _log.Warning("Version was null when retrieving a packages.config reference as part of {ProjectPath}", path);
            }

            if (name != null && version != null)
            {
                result.Add(new PackageReference(name.Value, version.Value));
            }
        }

        return result;
    }
}

public record PackageReference(string Name, string Version);
