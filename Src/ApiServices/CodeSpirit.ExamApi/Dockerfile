# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
# 安装中文字体
# 思源黑体（完整支持）
# 安装中文字体（仅思源黑体）
RUN apt-get update && apt-get install -y \
    fonts-noto-cjk \
    fontconfig \
    && fc-cache -f -v \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# 验证字体安装（可选，调试时使用）
RUN echo "已安装的中文字体：" && fc-list :lang=zh | head -5

USER $APP_UID
WORKDIR /app
ENV TZ=Asia/Shanghai
EXPOSE 8080
EXPOSE 8081


# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Src/CodeSpirit.ExamApi/CodeSpirit.ExamApi.csproj", "Src/CodeSpirit.ExamApi/"]
COPY ["Src/CodeSpirit.ServiceDefaults/CodeSpirit.ServiceDefaults.csproj", "Src/CodeSpirit.ServiceDefaults/"]
COPY ["Src/CodeSpirit.Core/CodeSpirit.Core.csproj", "Src/CodeSpirit.Core/"]
COPY ["Src/CodeSpirit.Shared/CodeSpirit.Shared.csproj", "Src/CodeSpirit.Shared/"]
COPY ["Src/Components/CodeSpirit.Aggregator/CodeSpirit.Aggregator.csproj", "Src/Components/CodeSpirit.Aggregator/"]
COPY ["Src/Components/CodeSpirit.Amis/CodeSpirit.Amis.csproj", "Src/Components/CodeSpirit.Amis/"]
COPY ["Src/Components/CodeSpirit.Authorization/CodeSpirit.Authorization.csproj", "Src/Components/CodeSpirit.Authorization/"]
COPY ["Src/Components/CodeSpirit.Navigation/CodeSpirit.Navigation.csproj", "Src/Components/CodeSpirit.Navigation/"]
COPY ["Src/Components/CodeSpirit.Charts/CodeSpirit.Charts.csproj", "Src/Components/CodeSpirit.Charts/"]
RUN dotnet restore "./Src/CodeSpirit.ExamApi/CodeSpirit.ExamApi.csproj"
COPY . .
WORKDIR "/src/Src/CodeSpirit.ExamApi"
RUN dotnet build "./CodeSpirit.ExamApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

# 此阶段用于发布要复制到最终阶段的服务项目
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./CodeSpirit.ExamApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# 此阶段在生产中使用，或在常规模式下从 VS 运行时使用(在不使用调试配置时为默认值)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeSpirit.ExamApi.dll"]