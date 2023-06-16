# dotnet-ecosystem

Quickly express the ecosystem of your .NET projects as a Neo4j graph for ease of querying insights.

The goal is for this to publish this as a dotnet global tool, but for now it's a simple console app.

## Getting Started

* You should have a neo4j database running. You can do this via a docker container: `docker run --name neo4j -e NEO4J_AUTH=neo4j/admin123 -p 7474:7474 -p 7687:7687 neo4j:latest`
  * NOTE: As of now, we have the neo4j auth value hard-coded as `neo4j/admin123` in the code, which we'll be working to change soon.

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

### List of versions of a given Nuget Package 

```cypher
MATCH (p:Project)-[r:USES]->(n:NugetPackage)
where n.name = "FluentAssertions"
return p.name, r.version
```

### List of Packages With a Given Target

```cypher
MATCH (p:Project)-[r:TARGETS]->(t:Target)
where t.name = "net6.0"
return p.name
```

### Targets for a Given Project

```cypher
MATCH (p:Project)-[r:TARGETS]->(t:Target)
WHERE p.name = "Converter.Tests.csproj"
return t.name
```

### Count of Project by SDK

```cypher
MATCH (p:Project)-[r:HAS_SDK]->(s:SDK)
Return s.name, count(*)
```