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
COPY ["ApiServices/CodeSpirit.SurveyApi/CodeSpirit.SurveyApi.csproj", "ApiServices/CodeSpirit.SurveyApi/"]
COPY ["CodeSpirit.ServiceDefaults/CodeSpirit.ServiceDefaults.csproj", "CodeSpirit.ServiceDefaults/"]
COPY ["CodeSpirit.Core/CodeSpirit.Core.csproj", "CodeSpirit.Core/"]
COPY ["CodeSpirit.Shared/CodeSpirit.Shared.csproj", "CodeSpirit.Shared/"]
COPY ["Components/CodeSpirit.Aggregator/CodeSpirit.Aggregator.csproj", "Components/CodeSpirit.Aggregator/"]
COPY ["Components/CodeSpirit.Amis/CodeSpirit.Amis.csproj", "Components/CodeSpirit.Amis/"]
COPY ["Components/CodeSpirit.Authorization/CodeSpirit.Authorization.csproj", "Components/CodeSpirit.Authorization/"]
COPY ["Components/CodeSpirit.LLM/CodeSpirit.LLM.csproj", "Components/CodeSpirit.LLM/"]
COPY ["Components/CodeSpirit.Navigation/CodeSpirit.Navigation.csproj", "Components/CodeSpirit.Navigation/"]
COPY ["Components/CodeSpirit.MultiTenant/CodeSpirit.MultiTenant.csproj", "Components/CodeSpirit.MultiTenant/"]
COPY ["Components/CodeSpirit.Settings/CodeSpirit.Settings.csproj", "Components/CodeSpirit.Settings/"]
COPY ["Components/CodeSpirit.Charts/CodeSpirit.Charts.csproj", "Components/CodeSpirit.Charts/"]
RUN dotnet restore "./ApiServices/CodeSpirit.SurveyApi/CodeSpirit.SurveyApi.csproj"
COPY . .
WORKDIR "/src/ApiServices/CodeSpirit.SurveyApi"
RUN dotnet build "./CodeSpirit.SurveyApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CodeSpirit.SurveyApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeSpirit.SurveyApi.dll"]