# GraphQL for .NET

[![License](https://img.shields.io/github/license/graphql-dotnet/graphql-dotnet)](LICENSE.md)
[![codecov](https://codecov.io/gh/graphql-dotnet/graphql-dotnet/branch/master/graph/badge.svg?token=iXZo1jZvFo)](https://codecov.io/gh/graphql-dotnet/graphql-dotnet)
[![Nuget](https://img.shields.io/nuget/dt/GraphQL)](https://www.nuget.org/packages/GraphQL)
[![Nuget](https://img.shields.io/nuget/v/GraphQL)](https://www.nuget.org/packages/GraphQL)
[![GitHub Release Date](https://img.shields.io/github/release-date/graphql-dotnet/graphql-dotnet?label=released)](https://github.com/graphql-dotnet/graphql-dotnet/releases)
[![GitHub commits since latest release (by date)](https://img.shields.io/github/commits-since/graphql-dotnet/graphql-dotnet/latest?label=new+commits)](https://github.com/graphql-dotnet/graphql-dotnet/commits/master)
![Size](https://img.shields.io/github/repo-size/graphql-dotnet/graphql-dotnet)
[![Gurubase](https://img.shields.io/badge/Gurubase-Ask%20GraphQL.NET%20Guru-006BFF)](https://gurubase.io/g/graphql-net)

[![GitHub contributors](https://img.shields.io/github/contributors/graphql-dotnet/graphql-dotnet)](https://github.com/graphql-dotnet/graphql-dotnet/graphs/contributors)
![Activity](https://img.shields.io/github/commit-activity/w/graphql-dotnet/graphql-dotnet)
![Activity](https://img.shields.io/github/commit-activity/m/graphql-dotnet/graphql-dotnet)
![Activity](https://img.shields.io/github/commit-activity/y/graphql-dotnet/graphql-dotnet)

| :heart: [Become a backer!](https://opencollective.com/graphql-net#backer) :heart: | [![Backers on Open Collective](https://opencollective.com/graphql-net/backers/badge.svg)](#backers) [![Sponsors on Open Collective](https://opencollective.com/graphql-net/sponsors/badge.svg)](#sponsors)  |
|-|-|

| :heavy_dollar_sign: [Get paid for contributing!](https://github.com/graphql-dotnet/graphql-dotnet/blob/master/BOUNTY.md) :heavy_dollar_sign: | [![GitHub issues by-label](https://img.shields.io/github/issues-raw/graphql-dotnet/graphql-dotnet/bounty?color=blue&label=open%20bounties)](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=is%3Aopen+is%3Aissue+label%3Abounty) [![GitHub closed issues by-label](https://img.shields.io/github/issues-closed-raw/graphql-dotnet/graphql-dotnet/bounty-paid?color=blue&label=paid%20bounties)](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=is%3Aclosed+is%3Aissue+label%3Abounty-paid)
|-|-|

This is an implementation of GraphQL, a query language and execution engine originally created
by Facebook in 2012, in .NET.

The [GraphQL specification](https://spec.graphql.org/) is now being developed and
maintained by the [GraphQL Foundation](https://foundation.graphql.org/), established in 2019 to
support the GraphQL ecosystem. You can also find the specification's source and discussions
on GitHub at [graphql/graphql-spec](https://github.com/graphql/graphql-spec).

This project uses a [lexer/parser](http://github.com/graphql-dotnet/parser) originally written
by [Marek Magdziak](https://github.com/mkmarek) and released with a MIT license. Thank you Marek!

Provides the following packages:

| Package                | Downloads                                                                                                                 | NuGet Latest                                                                                                             |
|------------------------|---------------------------------------------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------|
| GraphQL                | [![Nuget](https://img.shields.io/nuget/dt/GraphQL)](https://www.nuget.org/packages/GraphQL)                               | [![Nuget](https://img.shields.io/nuget/v/GraphQL)](https://www.nuget.org/packages/GraphQL)                               |
| GraphQL.SystemTextJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.SystemTextJson)](https://www.nuget.org/packages/GraphQL.SystemTextJson) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.SystemTextJson)](https://www.nuget.org/packages/GraphQL.SystemTextJson) |
| GraphQL.NewtonsoftJson | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.NewtonsoftJson) | [![Nuget](https://img.shields.io/nuget/v/GraphQL.NewtonsoftJson)](https://www.nuget.org/packages/GraphQL.NewtonsoftJson) |
| GraphQL.MemoryCache    | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.MemoryCache)](https://www.nuget.org/packages/GraphQL.MemoryCache)       | [![Nuget](https://img.shields.io/nuget/v/GraphQL.MemoryCache)](https://www.nuget.org/packages/GraphQL.MemoryCache)       |
| GraphQL.DataLoader     | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.DataLoader)](https://www.nuget.org/packages/GraphQL.DataLoader)         | [![Nuget](https://img.shields.io/nuget/v/GraphQL.DataLoader)](https://www.nuget.org/packages/GraphQL.DataLoader)         |
| GraphQL.MicrosoftDI    | [![Nuget](https://img.shields.io/nuget/dt/GraphQL.MicrosoftDI)](https://www.nuget.org/packages/GraphQL.MicrosoftDI)       | [![Nuget](https://img.shields.io/nuget/v/GraphQL.MicrosoftDI)](https://www.nuget.org/packages/GraphQL.MicrosoftDI)       |

You can get all preview versions from [GitHub Packages](https://github.com/orgs/graphql-dotnet/packages?repo_name=graphql-dotnet).
Note that GitHub requires authentication to consume the feed. See [here](https://docs.github.com/en/free-pro-team@latest/packages/publishing-and-managing-packages/about-github-packages#authenticating-to-github-packages).

## Documentation

1. http://graphql-dotnet.github.io - documentation site that is built from the [docs](/docs2/site/) folder in the `master` branch.
2. https://graphql.org/learn - learn about GraphQL, how it works, and how to use it.

## Debugging

All packages generated from this repository come with embedded pdb and support [Source Link](https://github.com/dotnet/sourcelink).
If you are having difficulty understanding how the code works or have encountered an error, then it is just enough to enable
Source Link in your IDE settings. Then you can debug GraphQL.NET source code as if it were part of your project.

## Installation

#### 1. GraphQL.NET engine

This is the main package, the heart of the repository in which you can find all the necessary classes
for GraphQL request processing.

```
> dotnet add package GraphQL
```

#### 2. Serialization

For serialized results, you'll need an `IGraphQLSerializer` implementation.
We provide several serializers (or you can bring your own).

```
> dotnet add package GraphQL.SystemTextJson
> dotnet add package GraphQL.NewtonsoftJson
```

> *Note: You can use `GraphQL.NewtonsoftJson` with .NET Core 3+, just be aware it lacks async writing 
> capabilities so writing to an ASP.NET Core 3.0 `HttpResponse.Body` will require you to set 
> `AllowSynchronousIO` to `true` as per [this announcement](https://github.com/aspnet/Announcements/issues/342);
> which isn't recommended.*

#### 3. Document Caching

The recommended way to setup caching layer (for caching of parsed GraphQL documents) is to
inherit from `IConfigureExecution` interface and register your class as its implementation.
We provide in-memory implementation on top of `Microsoft.Extensions.Caching.Memory` package.

```
> dotnet add package GraphQL.MemoryCache
```

For more information see [Document Caching](https://graphql-dotnet.github.io/docs/guides/document-caching).

#### 4. DataLoader

DataLoader is a generic utility to be used as part of your application's data fetching layer
to provide a simplified and consistent API over various remote data sources such as databases
or web services via batching and caching.

```
> dotnet add package GraphQL.DataLoader
```

For more information see [DataLoader](https://graphql-dotnet.github.io/docs/guides/dataloader).

> *Note: Prior to version 4, the contents of this package was part of the main GraphQL.NET package.*

#### 5. Subscriptions

`DocumentExecuter` can handle subscriptions as well as queries and mutations.
For more information see [Subscriptions](https://graphql-dotnet.github.io/docs/getting-started/subscriptions).

#### 6. Dependency Injection

To easily configure GraphQL.NET with the Microsoft dependency injection provider,
you can use the `GraphQL.MicrosoftDI` package. This package provides a `AddGraphQL`
extension method to register the necessary services. This package can also be used with
other dependency injection providers that support the `Microsoft.Extensions.DependencyInjection`
abstraction such as Autofac, Castle Windsor, and StructureMap.

```
> dotnet add package GraphQL.MicrosoftDI
```

You can then configure GraphQL.NET in your `Startup.cs` file like this:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddGraphQL(b => b
        .AddSchema<MySchema>()
        .AddSystemTextJson()
        .AddDataLoader()
    );
}
```

For more information see [Dependency Injection](https://graphql-dotnet.github.io/docs/getting-started/dependency-injection/).

## Examples

| Project / Repository        | Description                                                                                                            |
|-----------------------------|------------------------------------------------------------------------------------------------------------------------|
| [GraphQL.Net](https://github.com/graphql-dotnet/graphql-dotnet/tree/master/samples)                  | Sample projects focused on showcasing features of the core GraphQL library, an implementation of the GraphQL specification.           |
| [GraphQL.Net Server](https://github.com/graphql-dotnet/server/tree/master/samples)                   | Sample projects highlighting features of the server package, including utilities for integrating a GraphQL server with .NET Web APIs. |
| [Examples](https://github.com/graphql-dotnet/examples/tree/master/src)                               | Community-provided examples. These may not represent officially supported patterns but show how others use the library.               |
| [GraphQL.Net Client](https://github.com/graphql-dotnet/graphql-client/tree/master/examples)          | Example implementations for the GraphQL client library.                                                                               |

You can also try an example of GraphQL demo server inside this repo - [GraphQL.Harness](samples/GraphQL.Harness/GraphQL.Harness.csproj).
It supports the popular IDEs for managing GraphQL requests and exploring GraphQL schema:
- [Altair](https://github.com/imolorhe/altair)
- [Firecamp](https://firecamp.io/graphql/)
- [GraphiQL](https://github.com/graphql/graphiql)
- [GraphQL Playground](https://github.com/prisma-labs/graphql-playground)
- [Voyager](https://github.com/APIs-guru/graphql-voyager)

## Ahead-of-time compilation

GraphQL.NET supports ahead-of-time (AOT) compilation for execution of code-first schemas with .NET 7+. This allows
for use within iOS and Android apps, as well as other environments where such features as JIT compilation or
dynamic code generation are not available. It may be necessary to explicitly instruct the AOT compiler
to include the .NET types necessary for your schema to operate correctly. Of particular note, your query,
mutation and subscription types' constructors may be trimmed; register them in your DI engine to prevent this.
Also, `Field(x => x.MyField)` for enumeration values will require manually adding a mapping reference via
`RegisterTypeMapping<MyEnum, EnumerationGraphType<MyEnum>>()`. Please see the `GraphQL.AotCompilationSample` for a simple
demonstration of AOT compilation. Schema-first and type-first schemas have additional limtations and configuration requirements.
AOT compilation has not been tested with frameworks other than .NET 7+ on Windows and Linux (e.g. Xamarin).

## Training

* [API Development in .NET with GraphQL](https://www.lynda.com/NET-tutorials/API-Development-NET-GraphQL/664823-2.html) - [Glenn Block](https://twitter.com/gblock) demonstrates how to use the GraphQL.NET framework to build a fully functional GraphQL endpoint.
* [Building GraphQL APIs with ASP.NET Core](https://app.pluralsight.com/library/courses/building-graphql-apis-aspdotnet-core/table-of-contents) by [Roland Guijt](https://github.com/RolandGuijt)

## Upgrade Guides

You can see the changes in public APIs using [fuget.org](https://www.fuget.org/packages/GraphQL/8.2.1/lib/netstandard2.1/diff/7.9.0/).

* [7.x to 8.x](https://graphql-dotnet.github.io/docs/migrations/migration8)
* [5.x to 7.x](https://graphql-dotnet.github.io/docs/migrations/migration7)
* [4.x to 5.x](https://graphql-dotnet.github.io/docs/migrations/migration5)
* [3.x to 4.x](https://graphql-dotnet.github.io/docs/migrations/migration4)
* [2.4.x to 3.x](https://graphql-dotnet.github.io/docs/migrations/migration3)
* [0.17.x to 2.x](https://graphql-dotnet.github.io/docs/migrations/migration2)
* [0.11.0](https://graphql-dotnet.github.io/docs/migrations/v0_11_0)
* [0.8.0](https://graphql-dotnet.github.io/docs/migrations/v0_8_0)

## Basic Usage

Define your schema with a top level query object then execute that query.

Fully-featured examples can be found [here](https://github.com/graphql-dotnet/examples).

### Hello World

```csharp
using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using GraphQL.SystemTextJson; // First add PackageReference to GraphQL.SystemTextJson

var schema = Schema.For(@"
  type Query {
    hello: String
  }
");

var root = new { Hello = "Hello World!" };
var json = await schema.ExecuteAsync(_ =>
{
  _.Query = "{ hello }";
  _.Root = root;
});

Console.WriteLine(json);
```

### Schema First Approach

This example uses the [GraphQL schema language](https://graphql.org/learn/schema/#type-language).
See the [documentation](https://graphql-dotnet.github.io/docs/getting-started/introduction) for
more examples and information.

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class Query
{
  [GraphQLMetadata("droid")]
  public Droid GetDroid()
  {
    return new Droid { Id = "123", Name = "R2-D2" };
  }
}

var schema = Schema.For(@"
  type Droid {
    id: ID
    name: String
  }

  type Query {
    droid: Droid
  }
", _ => {
    _.Types.Include<Query>();
});

var json = await schema.ExecuteAsync(_ =>
{
  _.Query = "{ droid { id name } }";
});
```

### Parameters

```csharp
public class Droid
{
  public string Id { get; set; }
  public string Name { get; set; }
}

public class Query
{
  private List<Droid> _droids = new List<Droid>
  {
    new Droid { Id = "123", Name = "R2-D2" }
  };

  [GraphQLMetadata("droid")]
  public Droid GetDroid(string id)
  {
    return _droids.FirstOrDefault(x => x.Id == id);
  }
}

var schema = Schema.For(@"
  type Droid {
    id: ID
    name: String
  }

  type Query {
    droid(id: ID): Droid
  }
", _ => {
    _.Types.Include<Query>();
});

var json = await schema.ExecuteAsync(_ =>
{
  _.Query = $"{{ droid(id: \"123\") {{ id name }} }}";
});
```

## Roadmap

### Grammar / AST

- Grammar and AST for the GraphQL language should be compatible with the [October 2021 specification](https://spec.graphql.org/October2021/).

### Operation Execution

- [x] Scalars
- [x] Objects
- [x] Lists of objects/interfaces
- [x] Interfaces
- [x] Unions
- [x] Arguments
- [x] Variables
- [x] Fragments
- [x] Directives
  - [x] Include
  - [x] Skip
  - [x] Custom
- [x] Enumerations
- [x] Input Objects
- [x] Mutations
- [x] Subscriptions
- [x] Async execution

### Validation

- [x] Arguments of correct type
- [x] Default values of correct type
- [x] Fields on correct type
- [x] Fragments on composite types
- [x] Known argument names
- [x] Known directives
- [x] Known fragment names
- [x] Known type names
- [x] Lone anonymous operations
- [x] No fragment cycles
- [x] No undefined variables
- [x] No unused fragments
- [x] No unused variables
- [x] Overlapping fields can be merged
- [x] Possible fragment spreads
- [x] Provide non-null arguments
- [x] Scalar leafs
- [x] Unique argument names
- [x] Unique directives per location
- [x] Unique fragment names
- [x] Unique input field names
- [x] Unique operation names
- [x] Unique variable names
- [x] Variables are input types
- [x] Variables in allowed position
- [x] Single root field

### Schema Introspection

GraphQL.NET supports introspection schema from [October 2021 spec](https://spec.graphql.org/October2021/#sec-Schema-Introspection)
with some additional experimental introspection [extensions](https://graphql-dotnet.github.io/docs/getting-started/directives#directives-and-introspection).

## Publishing NuGet packages

The package publishing process is automated with [GitHub Actions](https://github.com/features/actions).

After your PR is merged into `master` or `develop`, preview packages are published to [GitHub Packages](https://github.com/orgs/graphql-dotnet/packages?repo_name=graphql-dotnet).

Stable versions of packages are published to NuGet when a [release](https://github.com/graphql-dotnet/graphql-dotnet/releases) is created.

## Contributors

This project exists thanks to all the people who contribute. 
<a href="https://github.com/graphql-dotnet/graphql-dotnet/graphs/contributors"><img src="https://opencollective.com/graphql-net/contributors.svg?width=890&button=false" /></a>

PRs are welcome! Looking for something to work on? The list of [open issues](https://github.com/graphql-dotnet/graphql-dotnet/issues)
is a great place to start. You can help the project simply respond to some of the [asked questions](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=is%3Aissue+is%3Aopen+label%3Aquestion).

The default branch is `master`. It is designed for non-breaking changes, that is to publish versions 7.x.x.
If you have a PR with some breaking changes, then please target it to the `develop` branch that tracks changes for v8.0.0.

## Backers

Thank you to all our backers! 🙏 [Become a backer](https://opencollective.com/graphql-net#backer).

Contributions are very much appreciated and are used to support the project primarily via [bounties](BOUNTY.md) paid directly to contributors to the project.
Please help us to express gratitude to those individuals who devote time and energy to contributing to this project by supporting their efforts in a tangible means.
A list of the outstanding issues to which we are sponsoring via bounties can be found [here](https://github.com/graphql-dotnet/graphql-dotnet/issues?q=is%3Aopen+is%3Aissue+label%3Abounty).

<a href="https://opencollective.com/graphql-net#backers" target="_blank"><img src="https://opencollective.com/graphql-net/backers.svg?width=890"></a>

## Sponsors

Support this project by becoming a sponsor. Your logo will show up here with a link to your website. [Become a sponsor](https://opencollective.com/graphql-net#sponsor).

<a href="https://opencollective.com/graphql-net/sponsor/0/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/0/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/1/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/1/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/2/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/2/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/3/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/3/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/4/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/4/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/5/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/5/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/6/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/6/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/7/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/7/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/8/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/8/avatar.svg"></a>
<a href="https://opencollective.com/graphql-net/sponsor/9/website" target="_blank"><img src="https://opencollective.com/graphql-net/sponsor/9/avatar.svg"></a>
