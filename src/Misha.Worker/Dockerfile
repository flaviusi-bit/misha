FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Misha.Worker/Misha.Worker.csproj", "src/Misha.Worker/"]
COPY ["src/Misha.Application/Misha.Application.csproj", "src/Misha.Application/"]
COPY ["src/Misha.Domain/Misha.Domain.csproj", "src/Misha.Domain/"]
COPY ["src/Misha.Infrastructure/Misha.Infrastructure.csproj", "src/Misha.Infrastructure/"]
RUN dotnet restore "src/Misha.Worker/Misha.Worker.csproj"
COPY . .
WORKDIR "/src/src/Misha.Worker"
RUN dotnet publish "Misha.Worker.csproj" -c Release -o /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Misha.Worker.dll"]
