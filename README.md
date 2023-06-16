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
* [X] Capture nuget packages and versions
  * [X] csproj file PackageReference
  * [X] packages.config files
* [ ] Capture config settings & values
  * [ ] app.confg
  * [ ] web.config
  * [ ] appSettings.*.json
* [ ] Capture DB connection string names & values
  * [ ] app.confg
  * [ ] web.config
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

### Count of Versions of a Nuget Package

```cypher
MATCH (p:Project)-[r:USES]->(n:NugetPackage)
where n.name = "FluentAssertions"
return r.version, count(*)
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
WHERE p.name = "MyProject.csproj"
return t.name
```

### Count of Project by SDK

```cypher
MATCH (p:Project)-[r:HAS_SDK]->(s:SDK)
Return s.name, count(*)
```

### Anomalies

Projects that don't have any associated nuget packages (to double-check the tool)

```cypher
MATCH (p:Project) WHERE NOT (p)-[:USES]->()
return p.name
```

Projects with no extracted targets:

```cypher
MATCH (p:Project) WHERE NOT (p)-[:TARGETS]->()
return p.name
```

### Packages by Usage
```cypher
MATCH (p:Project)-[r:USES]->(n:NugetPackage)
return n.name, count(*)
order by count(*) desc
```

### Packages and Versions Used by a Project

```cypher
MATCH (p:Project)-[r:USES]->(n:NugetPackage)
WHERE p.name = "MyProject.csproj"
return n.name, r.version
order by n.name
```