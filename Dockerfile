# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["SistemaTriagem.Api.csproj", "./"]
RUN dotnet restore "SistemaTriagem.Api.csproj"

COPY . .
RUN dotnet publish "SistemaTriagem.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render/Railway injetam a variável PORT; fallback para 8080
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
EXPOSE 8080

ENTRYPOINT ["dotnet", "SistemaTriagem.Api.dll"]
