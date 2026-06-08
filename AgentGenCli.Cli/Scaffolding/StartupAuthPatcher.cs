namespace AgentGenCli.Cli.Scaffolding;

internal static class StartupAuthPatcher
{
    private const string MiddlewareMarker = "// agentGenCli:auth-middleware";
    private const string ServicesMarker = "// agentGenCli:auth-services";

    public static void Apply(ProjectContext context)
    {
        var path = Path.Combine(context.Root, "applications", $"{context.ProjectName}.Api", "Startup.cs");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Startup.cs not found at '{path}'.");
        }

        var content = File.ReadAllText(path);
        if (content.Contains(MiddlewareMarker, StringComparison.Ordinal))
        {
            return;
        }

        if (!content.Contains("using Microsoft.AspNetCore.Authorization;", StringComparison.Ordinal))
        {
            content = content.Replace(
                "using Microsoft.OpenApi.Models;",
                "using Microsoft.AspNetCore.Authorization;\r\nusing Microsoft.AspNetCore.Authentication.JwtBearer;\r\nusing Microsoft.IdentityModel.Tokens;\r\nusing Microsoft.OpenApi.Models;\r\nusing System.Text;",
                StringComparison.Ordinal
            );
        }

        var authUsings =
            $"using {context.ProjectName}.Common.Auth;\r\nusing {context.ProjectName}.Common.Options;\r\nusing {context.ProjectName}.Common.Services;\r\nusing {context.ProjectName}.Api.Services;\r\n";

        if (!content.Contains($"{context.ProjectName}.Common.Auth", StringComparison.Ordinal))
        {
            content = content.Replace(
                $"using {context.ProjectName}.Common;",
                $"using {context.ProjectName}.Common;\r\n{authUsings}",
                StringComparison.Ordinal
            );
        }

        const string swaggerClose = "        });";
        const string servicesInsert =
            """

                services.AddHttpContextAccessor();
                services.AddMemoryCache();
                services.AddSingleton<ISystemClock, SystemClock>();
                var jwtOptions = Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
                services.AddSingleton(jwtOptions);
                services.AddSingleton(Configuration.GetSection("Encryption").Get<EncryptionOptions>() ?? new EncryptionOptions());
                services.AddSingleton(Configuration.GetSection("Google").Get<GoogleAuthOptions>() ?? new GoogleAuthOptions());
                services.AddSingleton(Configuration.GetSection("Apple").Get<AppleAuthOptions>() ?? new AppleAuthOptions());
                services.AddSingleton<IJWTTokenGenerator, JWTTokenGenerator>();
                services.AddSingleton<IEmailProtectionService, EmailProtectionService>();
                services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
                services.AddScoped<IUserContext, UserContextProvider>();
                services.AddAuthentication(options =>
                {
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidIssuer = jwtOptions.Issuer ?? "api",
                        ValidAudience = jwtOptions.Audience ?? "api",
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(
                                string.IsNullOrEmpty(jwtOptions.Key)
                                    ? string.Join(string.Empty, Enumerable.Repeat("dev-key", 32))
                                    : jwtOptions.Key
                            )
                        ),
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                    };
                });
                services.AddAuthorization(options =>
                {
                    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build();
                });

            """;

        var markerInsert = servicesInsert + "                " + ServicesMarker + "\r\n";
        var lastSwagger = content.LastIndexOf(swaggerClose, StringComparison.Ordinal);
        if (lastSwagger < 0)
        {
            throw new InvalidOperationException("Could not locate Swagger configuration in Startup.cs.");
        }

        content = content.Insert(lastSwagger + swaggerClose.Length, markerInsert);

        const string routingAnchor = "        app.UseRouting();";
        var middlewareInsert =
            "\r\n                app.UseAuthentication();\r\n                app.UseAuthorization();\r\n                "
            + MiddlewareMarker
            + "\r\n";

        content = content.Replace(routingAnchor, routingAnchor + middlewareInsert, StringComparison.Ordinal);
        File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }
}
