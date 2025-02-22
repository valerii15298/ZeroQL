# ZeroQL | [![GitHub](https://img.shields.io/github/license/byme8/ZeroQL?color=blue&style=flat-square)](https://github.com/byme8/ZeroQL/blob/main/LICENCE) [![Nuget](https://img.shields.io/nuget/v/zeroql?color=blue&style=flat-square)](https://www.nuget.org/packages/ZeroQL) [![.NET](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml/badge.svg)](https://github.com/byme8/ZeroQL/actions/workflows/dotnet.yml)

The ZeroQL is a high-performance C#-friendly GraphQL client. It supports Linq-like syntax. It doesn't require Reflection.Emit or expressions. As a result, the runtime provides performance very close to a raw HTTP call.

There is a more detailed list of what the ZeroQL can do at the moment:
- [x] Bootstrap schema.graphql file from graphql endpoint
- [x] Bootstrap client from schema.graphql file
- [x] Support for queries and mutations
    - [x] ["Lambda" like syntax](#graphql-lambda-syntax)
    - [x] ["Request" like syntax](#graphql-request-syntax)
- [ ] Support for subscriptions
- [x] [Support for fragments](#fragments)
- [x] [Support for interfaces](#interfaces)
- [x] [Support for unions](#unions)
- [x] [Support for file uploads](#file-upload)
- [x] [Support for persisted queries](#persisted-queries)
- [ ] Support for @defer
- [ ] Support for @stream

The articles:
- [ZeroQL - C# friendly graphql client](https://dev.to/byme8/zeroql-c-friendly-graphql-4134)
- [ZeroQL - C# GraphQL client adds fragments support](https://dev.to/byme8/zeroql-c-graphql-client-adds-fragments-support-1lcf)
- [ZeroQL V2 - C# GraphQL client](https://dev.to/byme8/zeroql-v2-c-graphql-client-1o8d)
- [ZeroQL V3 - C# GraphQL client](https://dev.to/byme8/zeroql-v3-c-friendly-graphql-client-4b8n)

# How to setup

The initial setup:
``` bash
# create console app
dotnet new console -o QLClient
# go to project folder 
cd QLClient
# create manifest file to track nuget tools
dotnet new tool-manifest 
# add ZeroQL.CLI nuget tool
dotnet tool install ZeroQL.CLI
# add ZeroQL nuget package
dotnet add package ZeroQL 
# fetch graphql schema from server(creates schema.graphql file)
dotnet zeroql schema pull http://localhost:10000/graphql
# to bootstrap schema.graphql file from graphql schema
dotnet zeroql generate --schema ./schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs
```

It is possible to add next target to csproj to keep generated client in sync with schema.graphql:
``` xml
<Target Name="GenerateQLClient" BeforeTargets="BeforeCompile">
    <Exec Command="dotnet zeroql generate --schema .\schema.graphql --namespace TestServer.Client --client-name TestServerGraphQLClient --output Generated/GraphQL.g.cs" />
</Target>
```
As a result, the graphql client will be generated on every build.

## Config

There is a way to simplify the CLI command. The command `` dotnet zeroql config init `` creates the `` zeroql.josn ``. It may look like that:
``` json
{
  "$schema": "https://raw.githubusercontent.com/byme8/ZeroQL/main/schema.verified.json",
  "graphql": "./schema.graphql",
  "namespace": "ZeroQL.Client",
  "clientName": "ZeroQLClient",
  "output": "./Generated/GraphQL.g.cs"
}
```

Then we can use it like that:
``` bash
dotnet zeroql generate -c ./zeroql.json
```

# How to use

Let's suppose that schema.graphql file contains the following:
``` graphql
schema {
  query: Queries
  mutation: Mutation
}

type Queries {
  me: User!
  user(id: Int!): User
}

type Mutation {
  addUser(firstName: String!, lastName: String!): User!
  addUserProfileImage(userId: Int! file: Upload!): Int!
}

type User {
  id: Int!
  firstName: String!
  lastName: String!
  role: Role!
}

type Role {
  id: Int!
  name: String!
}
```

and we want to execute the query like that:
``` graphql
query { me { id firstName lastName } }
```

## GraphQL lambda syntax

Here how we can achieve it with ZeroQL "lambda" syntax:
``` csharp
var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:10000/graphql");

var client = new TestServerGraphQLClient(httpClient);

var response = await client.Query(static o => o.Me(o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query { me { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```

You can pass arguments if needed:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query(variables, static (i, o) => o.User(i.Id, o => new { o.Id, o.FirstName, o.LastName }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query ($id: Int!) { user(id: $id) { id firstName lastName } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}"); // 1: Jon Smith
```

You can fetch attached fields:
``` csharp
var variables = new { Id = 1 };
var response = await client.Query(
    variables,
    static (i, o) => o
        .User(i.Id,
            o => new
            {
                o.Id,
                o.FirstName,
                o.LastName,
                Role = o.Role(role => role.Name)
            }));

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query GetUserWithRole($id: Int!) { user(id: $id) { id firstName lastName role { name }  } }
Console.WriteLine($"{response.Data.Id}: {response.Data.FirstName} {response.Data.LastName}, Role: {response.Data.Role}"); // 1: Jon Smith, Role: Admin
```

## GraphQL request syntax

In more complex queries, the "lambda" syntax may look verbose, and extracting requests into a separate entity would be nice. Now it is possible to do it via the "request" syntax. Here is an example:
``` csharp

// define a request
public record GetUserQuery(int Id) : GraphQL<Queries, UserModel?>
{
    public override UserModel? Execute(Queries query) 
        => query.User(Id, o => new UserModel(o.Id, o.FirstName, o.LastName));
}

// execute a request
var response = await client.Execute(new GetUserQuery(variables.FriendId));

Console.WriteLine(response.Query); // query GetUserQuery($id: Int!) { user(id: $id) { id firstName lastName } }
Console.WriteLine(response.Data); // UserModel { Id = 2, FirstName = Ben, LastName = Smith }

```

You need to create a record from the base record `` GraphQL<TOperationType, TResult> ``. Where the `` TOperationType `` is a root query type(`` Query ``, `` Mutation ``) that associated with the `` GraphQLClient<TQuery, TMutataion> `` instance.

# Fragments
It is possible to define and reuse fragments:
``` csharp
public static class UserFragments
{
    [GraphQLFragment]
    public static UserDetails ToUserDetails(this User user)
    {
        return new UserDetails
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}

var variables = new { Id = 1 };
var response = await client.Query(
    variables,
    static (i, q) => 
        new 
        { 
            Me = q.Me(o => o.ToUserDetails()),
            User = q.User(i.Id, o => o.ToUserDetails())
        });


Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: query ($id: Int!) { me { id firstName lastName } user(id: $id) { id firstName lastName } }
Console.WriteLine($"{response.Data.Me.Id}: {response.Data.Me.FirstName} {response.Data.Me.LastName}"); // 1: Jon Smith
Console.WriteLine($"{response.Data.User.Id}: {response.Data.User.FirstName} {response.Data.User.LastName}"); // 1: Jon Smith
```

The fragment should be marked with the `` [GraphQLFragment] `` attribute, and it should be an extension method. If the fragment is defined in another assembly, it should be a partial method. The last requirement is necessary because source generators don't have access to source code from another assembly. So, a workaround will be to define fragments as a partial method and generate additional metadata.

# Interfaces
For example, we have the following schema:
``` graphql
schema {
  query: Query
}

interface IFigure {
  perimeter: Float!
}

type Circle implements IFigure {
  center: Point!
  radius: Float!
  perimeter: Float!
}

type Point implements IFigure {
  x: Float!
  y: Float!
  perimeter: Float!
}

type Square implements IFigure {
  topLeft: Point!
  bottomRight: Point!
  perimeter: Float!
}

type Query {
  figures: [IFigure!]!
}
```

To get figures we can use next request:
``` csharp
 var response = await qlClient.Query(static q => q
    .Figures(o => new
    {
        o.Perimeter,
        Circle = o.On<Circle>().Select(oo => new
        {
            oo.Radius,
            Center = oo.Center(ooo => new { ooo.X, ooo.Y })
        }),
        Square = o.On<Square>().Select(oo => new
        {
            TopLeft = oo.TopLeft(ooo => new { ooo.X, ooo.Y }),
            BottomRight = oo.BottomRight(ooo => new { ooo.X, ooo.Y })
        })
    }));

Console.WriteLine(JsonSerializer.Serialize(response)); 
// {
//   "Query": "query { figures { perimeter ... on Circle { radius center { x y }  } ... on Square { topLeft { x y }  topLeft { x y }  } __typename } }",
//   "Data": [
//     {
//       "Perimeter": 6.2831854820251465,
//       "Circle": {
//         "Radius": 1,
//         "Center": {
//           "X": 1,
//           "Y": 1
//         }
//       }
//     },
//     {
//       "Perimeter": 40,
//       "Square": {
//         "TopLeft": {
//           "X": 1,
//           "Y": 1
//         },
//         "BottomRight": {
//           "X": 11,
//           "Y": 11
//         }
//       }
//     }
//   ]
// }
```

# Unions
Let's suppose that we have the following schema:
``` graphql
schema {
  query: Query
}

type TextContent {
  text: String!
}

type ImageContent {
  imageUrl: String!
  height: Int!
}

union PostContent = TextContent | ImageContent

type Query {
  posts: [PostContent!]!
}
```
Here how we can get posts:
``` csharp
 var response = await qlClient.Query(static q => q
        .Posts(
            o => new
            {
                Image = o.On<ImageContent>().Select(oo => new
                {
                    oo.ImageUrl,
                    oo.Height
                }),
                Text = o.On<TextContent>().Select(oo => new
                {
                    oo.Text
                }),
            }));

Console.WriteLine(JsonSerializer.Serialize(response));
// {
//   "Query": "query { posts { ... on ImageContent { imageUrl height } ... on TextContent { text } __typename } }",
//   "Data": [
//     {
//       "Image": {
//         "ImageUrl": "http://example.com/image.png",
//         "Height": 1920
//       }
//     },
//     {
//       "Text": {
//         "Text": "Hello World!"
//       }
//     }
//   ]
// }
```

# File upload

The ZeroQL supports file uploading via the "official" GraphQL way - the Upload type. 
There is an example:
``` csharp
public record AddAvatar(int Id, Upload File) : GraphQL<Mutation, int>
{
    public override int Execute(Mutation mutation)
        => mutation.AddUserProfileImage(Id, File);
}
```

> There is one nuance associated with the `` Upload `` type. Pay attention to how you pass the `` Upload `` instance.
For example, if it is a anonymous type `` new { File = new Upload("image.png", new MemoryStream()) } `` that means that reflection is going to be utilized to get the value. As a result, the Reflection.Emit is involved, which can be an issue in AOT scenarios. So, the "request" syntax would be better for such a case.

# Persisted queries

To use persisted queries, we need to pass `` PersistedQueryPipeline `` inside GraphQLClient:
``` csharp
var client = new TestServerGraphQLClient(httpClient, new PersistedQueryPipeline()); 
var response = await client.Execute(new GetUserQuery(1)); 

Console.WriteLine($"GraphQL: {response.Query}"); // GraphQL: 8cc1ee42eecdac2a8590486826856c041b04981a2c55d5cc560c338e1f6f0285:query GetUserQuery($id: Int!) { user(id: $id) { id firstName lastName } }
Console.WriteLine(response.Data); // UserModel { Id = 1, FirstName = Jon, LastName = Smith }
```

Now the client will follow "automatic persisted queries" pipeline. Description is here [here](https://chillicream.com/docs/hotchocolate/performance/automatic-persisted-queries).

When we need the "persisted queries" pipeline, described [here](https://chillicream.com/docs/hotchocolate/performance/persisted-queries), then change the client initialization like that:
``` csharp
var client = new TestServerGraphQLClient(httpClient, new PersistedQueryPipeline(tryToAddPersistedQueryOnFail: false));
```
and export defined queries from the assembly:
``` bash
 dotnet zeroql queries extract -a .\bin\Debug\net6.0\TestProject.dll -c TestServer.Client.TestServerGraphQLClient -o ./queries
```

The `` queries `` folder will contain the set of the "hashed" GraphQL files that you need for your GraphQL server setup:
``` bash
8cc1ee42eecdac2a8590486826856c041b04981a2c55d5cc560c338e1f6f0285.graphql # query GetUserQuery($id: Int!) { user(id: $id) { id firstName lastName } }
21cc96eaf0c0db2b5f980c8ec8b5aba2e40eb24f370cfc0cd7e4825509742ae2.graphql # mutation AddAvatar($id: Int!, $file: Upload!) { addUserProfileImage(userId: $id, file: $file)}
```


# Benchmarks

The complete benchmark source code you can find [here](https://github.com/byme8/ZeroQL/blob/main/src/Benchmarks/ZeroQL.Benchmark/Program.cs).

The short version looks like this:
``` csharp
[Benchmark]
public async Task<string> Raw()
{
    var rawQuery = @"{ ""query"": ""query { me { firstName }}"" }";
    var response = await httpClient.PostAsync("", new StringContent(rawQuery, Encoding.UTF8, "application/json"));
    var responseJson = await response.Content.ReadAsStreamAsync();
    var qlResponse = JsonSerializer.Deserialize<JsonObject>(responseJson, options);

    return qlResponse["data"]["me"]["firstName"].GetValue<string>();
}

[Benchmark]
public async Task<string> StrawberryShake()
{
    var firstname = await strawberryShake.Me.ExecuteAsync();
    return firstname.Data.Me.FirstName;
}

[Benchmark]
public async Task<string> ZeroQL()
{
    var firstname = await zeroQLClient.Query(static q => q.Me(o => o.FirstName));

    return firstname.Data;
}
```

Here results:
``` ini
BenchmarkDotNet=v0.13.2, OS=macOS 13.2.1 (22D68) [Darwin 22.3.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK=7.0.200
  [Host]     : .NET 7.0.3 (7.0.323.6910), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 7.0.3 (7.0.323.6910), Arm64 RyuJIT AdvSIMD
```
|              Method |     Mean |   Error |  StdDev |   Gen0 | Allocated |
|-------------------- |---------:|--------:|--------:|-------:|----------:|
|                 Raw | 172.2 μs | 1.49 μs | 1.40 μs | 0.7324 |   4.96 KB |
|     StrawberryShake | 175.0 μs | 1.18 μs | 1.05 μs | 1.4648 |   9.32 KB |
|        ZeroQLLambda | 174.2 μs | 1.26 μs | 1.17 μs | 0.7324 |    5.5 KB |
|       ZeroQLRequest | 174.8 μs | 1.68 μs | 1.49 μs | 0.7324 |   5.88 KB |
|  ZeroQLLambdaUpload | 208.5 μs | 2.06 μs | 1.83 μs | 1.4648 |  10.34 KB |
| ZeroQLRequestUpload | 208.9 μs | 3.02 μs | 2.83 μs | 1.7090 |  10.43 KB |

As you can see, the ``Raw`` method is the fastest.
The ``ZeroQL`` method is a bit faster than the ``StrawberryShake`` method. 
But in absolute terms, all of them are pretty much the same.

So, with the `` ZeroQL `` you can forget about the graphql and just use the Linq-like interface. 
It will have little effect on performace.
