FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Manipulator.Runner/Manipulator.Runner.csproj", "Manipulator.Runner/"]
COPY ["Manipulator.Mcp/Manipulator.Mcp/Manipulator.Mcp.csproj", "Manipulator.Mcp/Manipulator.Mcp/"]
COPY ["Manipulator.Core/Manipulator.Core.csproj", "Manipulator.Core/"]
RUN dotnet restore "Manipulator.Runner/Manipulator.Runner.csproj"
COPY . .
WORKDIR "/src/Manipulator.Runner"
RUN dotnet build "./Manipulator.Runner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Manipulator.Runner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Manipulator.Runner.dll"]
