#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/SIO.EventNotifier/SIO.EventNotifier.csproj", "src/SIO.EventNotifier/"]
COPY ["src/SIO.Migrations/SIO.Migrations.csproj", "src/SIO.Migrations/"]
COPY ["src/SIO.Domain/SIO.Domain.csproj", "src/SIO.Domain/"]
RUN dotnet restore "src/SIO.EventNotifier/SIO.EventNotifier.csproj"
COPY . .
WORKDIR "/src/src/SIO.EventNotifier"
RUN dotnet build "SIO.EventNotifier.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SIO.EventNotifier.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SIO.EventNotifier.dll"]