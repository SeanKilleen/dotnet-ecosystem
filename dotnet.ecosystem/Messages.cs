// See https://aka.ms/new-console-template for more information

namespace dotnet.ecosystem;

public class Messages
{
    public record FindProjects(string Path);
    public record ProcessProject(FileInfo File);
    public record AddProjectToGraph(string Directory, string ProjectFileName);
    public record SpecifyTargets(string Directory, string ProjectFileName, IReadOnlyList<string> Targets);
    public record SpecifyPackages(string Directory, string ProjectFileName, IReadOnlyList<PackageReference> Packages);
}
