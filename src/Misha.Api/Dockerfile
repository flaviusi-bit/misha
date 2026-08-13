FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Misha.Api/Misha.Api.csproj", "src/Misha.Api/"]
COPY ["src/Misha.Application/Misha.Application.csproj", "src/Misha.Application/"]
COPY ["src/Misha.Domain/Misha.Domain.csproj", "src/Misha.Domain/"]
COPY ["src/Misha.Infrastructure/Misha.Infrastructure.csproj", "src/Misha.Infrastructure/"]
RUN dotnet restore "src/Misha.Api/Misha.Api.csproj"
COPY . .
WORKDIR "/src/src/Misha.Api"
RUN dotnet publish "Misha.Api.csproj" -c Release -o /app/publish

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Misha.Api.dll"]
