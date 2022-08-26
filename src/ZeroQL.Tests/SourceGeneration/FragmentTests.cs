using FluentAssertions;
using Xunit;
using ZeroQL.Core;
using ZeroQL.TestApp.Models;
using ZeroQL.Tests.Core;
using ZeroQL.Tests.Data;

namespace ZeroQL.Tests.SourceGeneration;

public class FragmentTests : IntegrationTest
{
    [Fact]
    public async Task CanCreateClassInstance()
    {
        var csharpQuery = "static q => q.Me(o => new UserModel(o.FirstName, o.LastName, o.Role(o => o.Name)))";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task CanApplyFragmentWithBody()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleNameBody())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task CanApplyFragmentWithExpression()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleNameExpression())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }
    
    [Fact]
    public async Task CanApplyFragmentWithInitializers()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithRoleNameInitializers())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }
    
    [Fact]
    public async Task CanApplyFragmentWithArgument()
    {
        var csharpQuery = "static (i, q) => q.GetUserById(i.Id)";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, "new { Id = 1 }, " + csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }
    
    [Fact]
    public async Task CanApplyFragmentWithConstantArgument()
    {
        var csharpQuery = "static q => q.GetUserById(1)";
        var graphqlQuery = @"query { user(id: 1) { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }

    [Fact]
    public async Task CanLoadFragmentFromDifferentProject()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserFromDifferentAssembly())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }
    
    [Fact]
    public async Task CanLoadFragmentFromDifferentProjectWitharguments()
    {
        var csharpQuery = "new { Id = 1 }, static (i, q) => q.AsUserFromDifferentAssembly(i.Id)";
        var graphqlQuery = @"query ($id: Int!) { user(id: $id) { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }    
    
    [Fact]
    public async Task FragmentsWithPartialKeywordIsExtended()
    {
        var csharpQuery = "static q => q.Me(o => o.ExposedFragmentUserWithRole())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }
    
    [Fact]
    public async Task FragmentsWithPartialKeywordWithoutNamespaceIsExtended()
    {
        var csharpQuery = "static q => q.Me(o => o.AsUserWithoutNamespace())";
        var graphqlQuery = @"query { me { firstName lastName role { name }  } }";

        var project = await TestProject.Project
            .ReplacePartOfDocumentAsync("Program.cs", (TestProject.ME_QUERY, csharpQuery));

        var response = (GraphQLResult<UserModel>)await project.Validate(graphqlQuery);

        response.Data.FirstName.Should().Be("Jon");
        response.Data.LastName.Should().Be("Smith");
        response.Data.Role.Should().Be("Admin");
    }
}