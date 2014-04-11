DependencyGrapher
=================

Automatic architecture diagrams for .NET applications.

Output file format is [DOT](http://en.wikipedia.org/wiki/DOT_(graph_description_language)). Use [Graphviz](http://www.graphviz.org/) to convert it to image.

## Usage

    Usage: DependencyGrapher [OPTIONS]+

    Options:
      -h, --help                 show this message and exit
      -i, --input=VALUE          input root assembly
      -o, --output=VALUE         output dot diagram file
          --hideTransitive       hide transitive dependencies (default: false)
          --include-assembly=VALUE
                                 Regex for including referenced assemblies (default: all)
          --exclude-assembly=VALUE
                                 Regex for excluding referenced assemblies (default: System)
          --include-type=VALUE   Regex for including types (default: none)
          --exclude-type=VALUE   Regex for excluding types (default: all)

## Samples

[Orchard CMS](http://www.orchardproject.net/):

    DependencyGrapher.exe ^
        --hideTransitive ^
        -include-assembly="^Orchard.*" ^
        -include-type="^.*Service$" ^
        -exclude-type="^I[A-Z].*" ^
        -i=Orchard.Tests.Modules.dll

[DDD\CQRS Sample](http://cqrssample.codeplex.com/) - modules with entities:

    DependencyGrapher.exe ^
        --hideTransitive ^
        -include-assembly="^CQRS.*" ^
        -include-type="^Entity$" ^
        -i=CQRS.Web.dll

[DDD\CQRS Sample](http://cqrssample.codeplex.com/) - modules with events:

    DependencyGrapher.exe ^
        --hideTransitive ^
        -include-assembly="^CQRS.*" ^
        -include-type="^IDomainEvent$" ^
        -i=CQRS.Web.dll

[NorthwindStarterKit](http://nsk.codeplex.com/) - modules and aggregate roots:

    DependencyGrapher.exe ^
        --hideTransitive ^
        -include-assembly="^Nsk.*" ^
        -include-type="^IAggregateRoot$" ^
        -i=Nsk.Web.OnlineStore.dll

[EventStore](https://github.com/EventStore/EventStore):

    DependencyGrapher.exe ^
        --hideTransitive ^
        -include-assembly="^EventStore.*" ^
        -i=EventStore.SingleNode.exe

