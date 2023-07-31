#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Promotion.ReliefMoney.Service/Tgs.Promotion.ReliefMoney.Service.csproj", "Tgs.Promotion.ReliefMoney.Service/"]
COPY ["Tgs.Promotion.ReliefMoney.Core/Tgs.Promotion.ReliefMoney.Core.csproj", "Tgs.Promotion.ReliefMoney.Core/"]
COPY ["Tgs.Promotion.ReliefMoney.Repositories/Tgs.Promotion.ReliefMoney.Repositories.csproj", "Tgs.Promotion.ReliefMoney.Repositories/"]
RUN dotnet restore "Tgs.Promotion.ReliefMoney.Service/Tgs.Promotion.ReliefMoney.Service.csproj"
COPY . .
WORKDIR "/src/Tgs.Promotion.ReliefMoney.Service"
RUN dotnet build "Tgs.Promotion.ReliefMoney.Service.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Promotion.ReliefMoney.Service.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Promotion.ReliefMoney.Service.dll"]