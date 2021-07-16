#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:3.1 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:3.1 AS build
WORKDIR /src
COPY ["src/MergeBot/MergeBot.csproj", "src/MergeBot/"]
RUN dotnet restore "src/MergeBot/MergeBot.csproj"
COPY . .
WORKDIR "/src/src/MergeBot"
RUN dotnet build "MergeBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MergeBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MergeBot.dll"]