namespace AgentGenCli.Cli.Scaffolding;

internal static class FeatureApiGenerator
{
    public static string GenerateController(
        ProjectContext context,
        FeatureNameInfo feature,
        string crudLetters
    )
    {
        var project = context.ProjectName;
        var name = feature.PascalName;
        var route = feature.FolderName;

        var methods = new List<string>
        {
            $$"""
                [HttpGet("handle")]
                public async Task<IActionResult> Handle(CancellationToken cancellationToken)
                {
                    var result = await _queryProcessor.RunQueryAsync(new Handle{{name}}Query(), cancellationToken);
                    return ToActionResult(result);
                }
            """,
        };

        if (crudLetters.Contains('C', StringComparison.Ordinal))
        {
            methods.Add(
                $$"""
                    [HttpPost]
                    public async Task<IActionResult> Create(
                        [FromBody] Create{{name}}Query query,
                        CancellationToken cancellationToken
                    )
                    {
                        var result = await _queryProcessor.RunQueryAsync(query, cancellationToken);
                        return ToActionResult(result);
                    }
                """
            );
        }

        if (crudLetters.Contains('R', StringComparison.Ordinal))
        {
            methods.Add(
                $$"""
                    [HttpGet("{id:guid}")]
                    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
                    {
                        var result = await _queryProcessor.RunQueryAsync(
                            new Get{{name}}Query { Id = id },
                            cancellationToken
                        );
                        return ToActionResult(result);
                    }

                    [HttpGet]
                    public async Task<IActionResult> List(CancellationToken cancellationToken)
                    {
                        var result = await _queryProcessor.RunQueryAsync(new List{{name}}Query(), cancellationToken);
                        return ToActionResult(result);
                    }
                """
            );
        }

        if (crudLetters.Contains('U', StringComparison.Ordinal))
        {
            methods.Add(
                $$"""
                    [HttpPut("{id:guid}")]
                    public async Task<IActionResult> Update(
                        Guid id,
                        [FromBody] Update{{name}}Query query,
                        CancellationToken cancellationToken
                    )
                    {
                        query.Id = id;
                        var result = await _queryProcessor.RunQueryAsync(query, cancellationToken);
                        return ToActionResult(result);
                    }
                """
            );
        }

        if (crudLetters.Contains('D', StringComparison.Ordinal))
        {
            methods.Add(
                $$"""
                    [HttpDelete("{id:guid}")]
                    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
                    {
                        var result = await _queryProcessor.RunQueryAsync(
                            new Delete{{name}}Query { Id = id },
                            cancellationToken
                        );

                        if (result.IsNone)
                        {
                            return NotFound();
                        }

                        return NoContent();
                    }
                """
            );
        }

        return $$"""
            using {{project}}.Features.{{name}}.Contracts;

            using Microsoft.AspNetCore.Mvc;

            using Prodot.Patterns.Cqrs;

            namespace {{project}}.Api.Controllers;

            [ApiController]
            [Route("{{route}}")]
            public class {{name}}Controller : ControllerBase
            {
                private readonly IQueryProcessor _queryProcessor;

                public {{name}}Controller(IQueryProcessor queryProcessor)
                {
                    _queryProcessor = queryProcessor;
                }

            {{string.Join("\n\n", methods)}}

                private IActionResult ToActionResult<T>(Option<T> result)
                {
                    if (result.IsNone)
                    {
                        return NotFound();
                    }

                    return Ok(result.Get());
                }
            }
            """;
    }

    public static string GenerateControllerTests(ProjectContext context, FeatureNameInfo feature)
    {
        var project = context.ProjectName;
        var name = feature.PascalName;
        var route = feature.FolderName;

        return $$"""
            using System.Net.Http.Json;

            using {{project}}.Features.{{name}}.Contracts;

            using Microsoft.AspNetCore.Hosting;
            using Microsoft.AspNetCore.Mvc.Testing;

            namespace {{project}}.Api.Tests.Controllers;

            public class {{name}}ControllerTests : IClassFixture<WebApplicationFactory<ApiEntryPoint>>
            {
                private readonly WebApplicationFactory<ApiEntryPoint> _factory;

                public {{name}}ControllerTests(WebApplicationFactory<ApiEntryPoint> factory)
                {
                    _factory = factory;
                }

                [Fact]
                public async Task Handle_returns_placeholder_dto()
                {
                    var client = _factory
                        .WithWebHostBuilder(builder => builder.UseEnvironment("E2ETest"))
                        .CreateClient();

                    var response = await client.GetAsync("/{{route}}/handle");
                    response.EnsureSuccessStatusCode();

                    var dto = await response.Content.ReadFromJsonAsync<{{name}}Dto>();
                    Assert.NotNull(dto);
                    Assert.Contains("Replace this query", dto.Message, StringComparison.Ordinal);
                }
            }
            """;
    }
}
