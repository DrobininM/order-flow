FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "../"]
COPY ["Directory.Packages.props", "../"]
COPY ["OrderService/src/OrderService.Domain/OrderService.Domain.csproj", "OrderService/src/OrderService.Domain/"]
COPY ["OrderService/src/OrderService.Application/OrderService.Application.csproj", "OrderService/src/OrderService.Application/"]
COPY ["OrderService/src/OrderService.Infrastructure/OrderService.Infrastructure.csproj", "OrderService/src/OrderService.Infrastructure/"]
COPY ["OrderService/src/OrderService.Presentation/OrderService.Presentation.csproj", "OrderService/src/OrderService.Presentation/"]

RUN dotnet restore "OrderService/src/OrderService.Presentation/OrderService.Presentation.csproj"

COPY . .

WORKDIR "/src/OrderService/src/OrderService.Presentation"
RUN dotnet build "OrderService.Presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "OrderService.Presentation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "OrderService.Presentation.dll"]
