FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["applications/ProjectName.Api/ProjectName.Api.csproj", "applications/ProjectName.Api/"]
COPY ["common/ProjectName.Common/ProjectName.Common.csproj", "common/ProjectName.Common/"]
COPY ["extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.csproj", "extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs/"]
COPY ["extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.Abstractions/Prodot.Patterns.Cqrs.Abstractions.csproj", "extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.Abstractions/"]
COPY ["extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.EfCore/Prodot.Patterns.Cqrs.EfCore.csproj", "extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.EfCore/"]
COPY ["extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.EfCore.Abstractions/Prodot.Patterns.Cqrs.EfCore.Abstractions.csproj", "extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.EfCore.Abstractions/"]
COPY ["extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.MicrosoftExtensionsDependencyInjection/Prodot.Patterns.Cqrs.MicrosoftExtensionsDependencyInjection.csproj", "extern/Prodot.Patterns.Cqrs/Prodot.Patterns.Cqrs.MicrosoftExtensionsDependencyInjection/"]

RUN dotnet restore "applications/ProjectName.Api/ProjectName.Api.csproj"

COPY . .
WORKDIR "/src/applications/ProjectName.Api"
RUN dotnet build "ProjectName.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ProjectName.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ProjectName.Api.dll"]
