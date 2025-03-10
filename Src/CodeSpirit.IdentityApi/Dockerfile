# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Src/CodeSpirit.IdentityApi/CodeSpirit.IdentityApi.csproj", "Src/CodeSpirit.IdentityApi/"]
COPY ["Src/CodeSpirit.ConfigCenter.Client/CodeSpirit.ConfigCenter.Client.csproj", "Src/CodeSpirit.ConfigCenter.Client/"]
COPY ["Src/CodeSpirit.Core/CodeSpirit.Core.csproj", "Src/CodeSpirit.Core/"]
COPY ["Src/CodeSpirit.ServiceDefaults/CodeSpirit.ServiceDefaults.csproj", "Src/CodeSpirit.ServiceDefaults/"]
COPY ["Src/CodeSpirit.Shared/CodeSpirit.Shared.csproj", "Src/CodeSpirit.Shared/"]
COPY ["Src/Components/CodeSpirit.Aggregator/CodeSpirit.Aggregator.csproj", "Src/Components/CodeSpirit.Aggregator/"]
COPY ["Src/Components/CodeSpirit.Amis/CodeSpirit.Amis.csproj", "Src/Components/CodeSpirit.Amis/"]
COPY ["Src/Components/CodeSpirit.Authorization/CodeSpirit.Authorization.csproj", "Src/Components/CodeSpirit.Authorization/"]
COPY ["Src/Components/CodeSpirit.Navigation/CodeSpirit.Navigation.csproj", "Src/Components/CodeSpirit.Navigation/"]
COPY ["Src/Components/CodeSpirit.Charts/CodeSpirit.Charts.csproj", "Src/Components/CodeSpirit.Charts/"]
RUN dotnet restore "./Src/CodeSpirit.IdentityApi/CodeSpirit.IdentityApi.csproj"
COPY . .
WORKDIR "/src/Src/CodeSpirit.IdentityApi"
RUN dotnet build "./CodeSpirit.IdentityApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CodeSpirit.IdentityApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeSpirit.IdentityApi.dll"]