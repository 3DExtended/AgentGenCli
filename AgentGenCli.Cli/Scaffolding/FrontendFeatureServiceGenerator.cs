using System.Text;

namespace AgentGenCli.Cli.Scaffolding;

internal static class FrontendFeatureServiceGenerator
{
    public static string Generate(ProjectContext context, FeatureNameInfo feature, bool withApi)
    {
        var package = NameFormatting.ToSnakeCase(context.ProjectName);
        var folder = feature.FolderName;
        var pascal = feature.PascalName;

        if (!withApi)
        {
            return GeneratePlaceholderService(package, pascal, $"{pascal} feature scaffolded. Implement API integration here.");
        }

        var controllerPath = Path.Combine(
            context.Root,
            "applications",
            $"{context.ProjectName}.Api",
            "Controllers",
            $"{pascal}Controller.cs"
        );

        if (!File.Exists(controllerPath))
        {
            return GeneratePlaceholderService(
                package,
                pascal,
                $"{pascal} backend API not found. Scaffold backend feature with --withApi first."
            );
        }

        var controllerContent = File.ReadAllText(controllerPath);
        var capabilities = ControllerCrudDetector.Detect(controllerContent);
        var prefix = SwaggerRouteNaming.ToMethodPrefix(folder);

        var imports = new StringBuilder();
        imports.AppendLine($"import 'package:{package}/core/api/api_connector.dart';");

        if (capabilities.HasCreate || capabilities.HasUpdate)
        {
            imports.AppendLine($"import 'package:{package}/generated/swaggen/swagger.models.swagger.dart';");
        }

        var methods = new StringBuilder();

        if (capabilities.HasHandle)
        {
            methods.AppendLine(
                """
                  Future<dynamic> handle() async {
                    final client = await _apiConnector.getClient();
                    final response = await client.{{PREFIX}}HandleGet();
                    if (!response.isSuccessful) {
                      throw Exception('{{PASCAL}} handle request failed (${response.statusCode})');
                    }

                    return response.body;
                  }

                """
                    .Replace("{{PREFIX}}", prefix, StringComparison.Ordinal)
                    .Replace("{{PASCAL}}", pascal, StringComparison.Ordinal)
            );
        }

        if (capabilities.HasCreate)
        {
            methods.AppendLine(
                """
                  Future<dynamic> create(Create{{PASCAL}}Query query) async {
                    final client = await _apiConnector.getClient();
                    final response = await client.{{PREFIX}}Post(body: query);
                    if (!response.isSuccessful) {
                      throw Exception('{{PASCAL}} create request failed (${response.statusCode})');
                    }

                    return response.body;
                  }

                """
                    .Replace("{{PREFIX}}", prefix, StringComparison.Ordinal)
                    .Replace("{{PASCAL}}", pascal, StringComparison.Ordinal)
            );
        }

        if (capabilities.HasGetById)
        {
            methods.AppendLine(
                """
                  Future<dynamic> getById(String id) async {
                    final client = await _apiConnector.getClient();
                    final response = await client.{{PREFIX}}IdGet(id: id);
                    if (!response.isSuccessful) {
                      throw Exception('{{PASCAL}} get request failed (${response.statusCode})');
                    }

                    return response.body;
                  }

                """
                    .Replace("{{PREFIX}}", prefix, StringComparison.Ordinal)
                    .Replace("{{PASCAL}}", pascal, StringComparison.Ordinal)
            );
        }

        if (capabilities.HasList)
        {
            methods.AppendLine(
                """
                  Future<dynamic> list() async {
                    final client = await _apiConnector.getClient();
                    final response = await client.{{PREFIX}}Get();
                    if (!response.isSuccessful) {
                      throw Exception('{{PASCAL}} list request failed (${response.statusCode})');
                    }

                    return response.body;
                  }

                """
                    .Replace("{{PREFIX}}", prefix, StringComparison.Ordinal)
                    .Replace("{{PASCAL}}", pascal, StringComparison.Ordinal)
            );
        }

        if (capabilities.HasUpdate)
        {
            methods.AppendLine(
                """
                  Future<dynamic> update(String id, Update{{PASCAL}}Query query) async {
                    final client = await _apiConnector.getClient();
                    final response = await client.{{PREFIX}}IdPut(id: id, body: query);
                    if (!response.isSuccessful) {
                      throw Exception('{{PASCAL}} update request failed (${response.statusCode})');
                    }

                    return response.body;
                  }

                """
                    .Replace("{{PREFIX}}", prefix, StringComparison.Ordinal)
                    .Replace("{{PASCAL}}", pascal, StringComparison.Ordinal)
            );
        }

        if (capabilities.HasDelete)
        {
            methods.AppendLine(
                """
                  Future<void> delete(String id) async {
                    final client = await _apiConnector.getClient();
                    final response = await client.{{PREFIX}}IdDelete(id: id);
                    if (!response.isSuccessful) {
                      throw Exception('{{PASCAL}} delete request failed (${response.statusCode})');
                    }
                  }

                """
                    .Replace("{{PREFIX}}", prefix, StringComparison.Ordinal)
                    .Replace("{{PASCAL}}", pascal, StringComparison.Ordinal)
            );
        }

        var placeholderMethod = capabilities.HasHandle
            ? """
                  Future<String> getPlaceholderMessage() async {
                    final result = await handle();
                    return result?.toString() ?? '{{PASCAL}} API response received';
                  }

              """.Replace("{{PASCAL}}", pascal, StringComparison.Ordinal)
            : """
                  Future<String> getPlaceholderMessage() async {
                    return '{{PASCAL}} API wired. Implement UI for CRUD operations.';
                  }

              """.Replace("{{PASCAL}}", pascal, StringComparison.Ordinal);

        return $$"""
            {{imports.ToString().TrimEnd()}}

            class {{pascal}}Service {
              {{pascal}}Service(this._apiConnector);

              final ApiConnector _apiConnector;

            {{methods}}{{placeholderMethod}}}
            """;
    }

    private static string GeneratePlaceholderService(string package, string pascal, string message)
    {
        return $$"""
            import 'package:{{package}}/core/api/api_connector.dart';

            class {{pascal}}Service {
              {{pascal}}Service(this._apiConnector);

              final ApiConnector _apiConnector;

              // TODO: Replace with feature-specific API calls.
              Future<String> getPlaceholderMessage() async {
                return '{{message}}';
              }
            }
            """;
    }
}
