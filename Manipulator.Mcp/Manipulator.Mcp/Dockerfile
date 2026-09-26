FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Manipulator.Mcp/Manipulator.Mcp/Manipulator.Mcp.csproj", "Manipulator.Mcp/Manipulator.Mcp/"]
RUN dotnet restore "Manipulator.Mcp/Manipulator.Mcp/Manipulator.Mcp.csproj"
COPY . .
WORKDIR "/src/Manipulator.Mcp/Manipulator.Mcp"
RUN dotnet build "./Manipulator.Mcp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Manipulator.Mcp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Manipulator.Mcp.dll"]
