FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Directory.Build.props", "../"]
COPY ["Directory.Packages.props", "../"]
COPY ["PaymentService/src/PaymentService.Domain/PaymentService.Domain.csproj", "PaymentService/src/PaymentService.Domain/"]
COPY ["PaymentService/src/PaymentService.Application/PaymentService.Application.csproj", "PaymentService/src/PaymentService.Application/"]
COPY ["PaymentService/src/PaymentService.Infrastructure/PaymentService.Infrastructure.csproj", "PaymentService/src/PaymentService.Infrastructure/"]
COPY ["PaymentService/src/PaymentService.Presentation/PaymentService.Presentation.csproj", "PaymentService/src/PaymentService.Presentation/"]

RUN dotnet restore "PaymentService/src/PaymentService.Presentation/PaymentService.Presentation.csproj"

COPY . .

WORKDIR "/src/PaymentService/src/PaymentService.Presentation"
RUN dotnet build "PaymentService.Presentation.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentService.Presentation.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.Presentation.dll"]
