FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
RUN dotnet dev-certs https -ep /https/aspnetapp.pfx -p "fllkwefiwejf23049uwlekjf.sEFWEFGR98^&$"
COPY ["FileHub.Presentation/FileHub.Presentation.csproj", "FileHub.Presentation/"]
COPY ["FileHub.Core/FileHub.Core.csproj", "FileHub.Core/"]
COPY ["FileHub.Infrastructure/FileHub.Infrastructure.csproj", "FileHub.Infrastructure/"]
RUN dotnet restore "FileHub.Presentation/FileHub.Presentation.csproj"
COPY . .
WORKDIR "/src/FileHub.Presentation"
RUN dotnet build "FileHub.Presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FileHub.Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /https/* /https/
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FileHub.Presentation.dll"]
