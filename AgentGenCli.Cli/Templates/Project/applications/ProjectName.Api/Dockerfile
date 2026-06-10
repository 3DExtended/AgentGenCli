FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["applications/ProjectName.Api/ProjectName.Api.csproj", "applications/ProjectName.Api/"]
COPY ["common/ProjectName.Common/ProjectName.Common.csproj", "common/ProjectName.Common/"]
COPY ["common/ProjectName.Cqrs/ProjectName.Cqrs.csproj", "common/ProjectName.Cqrs/"]
COPY ["common/ProjectName.Cqrs.EfCore/ProjectName.Cqrs.EfCore.csproj", "common/ProjectName.Cqrs.EfCore/"]

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
