# dotnet-ecosystem

Quickly express the ecosystem of your .NET projects as a Neo4j graph.

The goal is for this to publish this as a dotnet global tool, but for now it's a simple console app.

## Roadmap

* [X] Detect csproj files
* [X] Surface target frameworks of csproj files
* [ ] Capture project references from csproj
* [ ] Capture nuget packages and versions
* [ ] Capture config settings & values
  * [ ] app.confg, web.config, etc.
  * [ ] appSettings.*.json
* [ ] Capture DB connection string names & values
  * [ ] app.confg, web.config, etc.
  * [ ] appSettings.*.json
* [ ] Capture remote/WCF service references/endpoint names & values
  * [ ] app.confg, web.config, etc.
  * [ ] appSettings.*.json
