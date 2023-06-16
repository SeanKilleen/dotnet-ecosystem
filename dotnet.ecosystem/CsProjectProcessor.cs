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

                var appSettings = await ExtractAppSettingsFromAppConfig(appConfigPath);
                _log.Info("Extracted {SettingsCount} app settings from app.config for {ProjectName}", appSettings.Count, msg.File.Name);
                _graphActor.Tell(new Messages.SpecifyAppSettings(msg.File.DirectoryName, msg.File.Name, appSettings));

                var appConnStrings = await ExtractConnectionStringsFromAppConfig(appConfigPath);
                _log.Info("Extracted {ConnectionStringsCount} connection strings from app.config for {ProjectName}", appConnStrings.Count, msg.File.Name);
                _graphActor.Tell(new Messages.SpecifyConnectionStrings(msg.File.DirectoryName, msg.File.Name, appConnStrings));
            }

            var webConfigPath = Path.Combine(msg.File.DirectoryName, "web.config");
            if (File.Exists(webConfigPath))
            {
                _log.Info("Found web.config for {ProjectName}; processing.", msg.File.Name);

                var webSettings = await ExtractAppSettingsFromAppConfig(webConfigPath);
                _log.Info("Extracted {SettingsCount} app settings from web.config for {ProjectName}", webSettings.Count, msg.File.Name);
                _graphActor.Tell(new Messages.SpecifyAppSettings(msg.File.DirectoryName, msg.File.Name, webSettings));

                var webConnStrings = await ExtractConnectionStringsFromAppConfig(webConfigPath);
                _log.Info("Extracted {ConnectionStringsCount} connection strings from web.config for {ProjectName}", webConnStrings.Count, msg.File.Name);
                _graphActor.Tell(new Messages.SpecifyConnectionStrings(msg.File.DirectoryName, msg.File.Name, webConnStrings));

            }

        });
    }

    private async Task<List<ConnectionString>> ExtractConnectionStringsFromAppConfig(string path)
    {
        List<ConnectionString> result = new();

        var xmlText = await File.ReadAllTextAsync(path);
        var xDoc = XDocument.Parse(xmlText);

        var connectionStringsElements = xDoc.Descendants("connectionStrings").FirstOrDefault();
        if (connectionStringsElements is null)
        {
            _log.Info("connectionStrings was empty for {ProjectPath}; returning empty result list.", path);
            return result;
        }

        var addedConnectionStrings = connectionStringsElements.Descendants().Where(x => string.Equals(x.Name.LocalName, "add", StringComparison.InvariantCultureIgnoreCase));

        foreach (var connectionStringElement in addedConnectionStrings)
        {
            var name = connectionStringElement.AttributeCaseInsensitive("name");
            var connectionString = connectionStringElement.AttributeCaseInsensitive("connectionString");
            var provider = connectionStringElement.AttributeCaseInsensitive("provider");

            if (name == null)
            {
                _log.Warning("Key was null when retrieving an app.config appSetting as part of {ProjectPath}", path);
            }

            if (connectionString == null)
            {
                _log.Warning("Value was null when retrieving an app.config appSetting as part of {ProjectPath}", path);
            }

            if (name != null && connectionString != null)
            {
                result.Add(new ConnectionString(name.Value, connectionString.Value, provider?.Value));
            }
        }

        return result;

    }
    private async Task<List<AppSetting>> ExtractAppSettingsFromAppConfig(string path)
    {
        List<AppSetting> result = new();

        var xmlText = await File.ReadAllTextAsync(path);
        var xDoc = XDocument.Parse(xmlText);

        var appSettingsElements = xDoc.Descendants("appSettings").FirstOrDefault();
        if (appSettingsElements is null)
        {
            _log.Info("appSettings was empty for {ProjectPath}; returning empty result list.", path);
            return result;
        }

        var addedAppSettings = appSettingsElements.Descendants().Where(x => string.Equals(x.Name.LocalName, "add", StringComparison.InvariantCultureIgnoreCase));

        foreach (var settingElement in addedAppSettings)
        {
            var key = settingElement.AttributeCaseInsensitive("key");
            var value = settingElement.AttributeCaseInsensitive("value");

            if (key == null)
            {
                _log.Warning("Key was null when retrieving an app.config appSetting as part of {ProjectPath}", path);
            }

            if (value == null)
            {
                _log.Warning("Value was null when retrieving an app.config appSetting as part of {ProjectPath}", path);
            }

            if (key != null && value != null)
            {
                result.Add(new AppSetting(key.Value, value.Value));
            }
        }

        return result;

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
public record AppSetting(string Name, string Value);
public record ConnectionString(string Name, string Value, string? Provider);