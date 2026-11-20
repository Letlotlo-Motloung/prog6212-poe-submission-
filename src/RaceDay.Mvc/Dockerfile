# Use the official .NET 10 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy the solution and project files first (for better caching)
COPY ["RaceDay.sln", "."]
COPY ["src/RaceDay.Api/RaceDay.Api.csproj", "src/RaceDay.Api/"]
COPY ["src/RaceDay.Mvc/RaceDay.Mvc.csproj", "src/RaceDay.Mvc/"]

# Restore dependencies
RUN dotnet restore "src/RaceDay.Mvc/RaceDay.Mvc.csproj"

# Copy the rest of the files and build
COPY . .
WORKDIR "/src/src/RaceDay.Mvc"
RUN dotnet build "RaceDay.Mvc.csproj" -c Release -o /app/build

# Publish the app
FROM build AS publish
RUN dotnet publish "RaceDay.Mvc.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RaceDay.Mvc.dll"]
