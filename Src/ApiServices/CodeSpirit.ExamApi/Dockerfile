# 请参阅 https://aka.ms/customizecontainer 以了解如何自定义调试容器，以及 Visual Studio 如何使用此 Dockerfile 生成映像以更快地进行调试。

# 此阶段用于在快速模式(默认为调试配置)下从 VS 运行时
FROM ccr.ccs.tencentyun.com/magicodes/aspnetcore-runtime:9.0withfonts AS base
USER root
# 安装Chrome依赖
RUN apt-get update && apt-get install -y \
    chromium \
    libgbm1 \
    libnss3 \
    libxss1 \
    libasound2 \
    libatk1.0-0 \
    libatk-bridge2.0-0 \
    libcups2 \
    libdrm2 \
    libxkbcommon0 \
    libxcomposite1 \
    libxdamage1 \
    libxfixes3 \
    libxrandr2 \
    libgbm1 \
    libpango-1.0-0 \
    libcairo2 \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# 设置环境变量
ENV PUPPETEER_EXECUTABLE_PATH=/usr/bin/chromium
ENV PDF_GENERATION_MAX_CONCURRENT=5
ENV PDF_GENERATION_POOL_SIZE=3

USER $APP_UID
WORKDIR /app
ENV TZ=Asia/Shanghai
EXPOSE 8080
EXPOSE 8081


# 此阶段用于生成服务项目
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
COPY ["Src/Components/CodeSpirit.PdfGeneration/CodeSpirit.PdfGeneration.csproj", "Src/Components/CodeSpirit.PdfGeneration/"]
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