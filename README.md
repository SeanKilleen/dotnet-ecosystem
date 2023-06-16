# dotnet-ecosystem

Quickly express the ecosystem of your .NET projects as a Neo4j graph for ease of querying insights.

The goal is for this to publish this as a dotnet global tool, but for now it's a simple console app.

## Getting Started

* You should have a neo4j database running. You can do this via a docker container: `docker run -e NEO4J_AUTH=neo4j/admin -p 7474:7474 -p 7687:7687 neo4j:latest`
  * NOTE: As of now, we have the neo4j auth value hard-coded as `neo4j/admin` in the code, which we'll be working to change soon.

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

## Queries to Run

Check back here soon for a list of queries.
