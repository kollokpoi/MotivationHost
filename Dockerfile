#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
RUN apt-get update && apt-get install -y libgdiplus
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Motivation/Motivation.csproj", "Motivation/"]
RUN dotnet restore "Motivation/Motivation.csproj"
COPY . .
WORKDIR "/src/Motivation"
RUN dotnet build "Motivation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Motivation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 80
EXPOSE 443
ENTRYPOINT ["dotnet", "Motivation.dll"]
