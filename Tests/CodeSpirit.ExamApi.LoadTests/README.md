# CodeSpirit 考试客户端 NBomber 负载测试

基于 NBomber 框架的 CodeSpirit 考试客户端负载测试项目，专门测试考试系统客户端接口的性能表现。

## 🎯 测试流程

本测试严格按照以下业务流程进行：

1. **客户端登录** (`/api/auth/client/login`) - 获取JWT Token
2. **获取考生个人信息** (`/api/exam/client/profile`) - 验证用户身份
3. **获取可用考试** (`/api/exam/client/available`) - 查看可参加的考试
4. **获取考试基本信息** (`/api/exam/client/{id}/basic`) - 获取具体考试详情

## 🚀 快速开始

### 前置要求

- .NET 9.0 SDK
- CodeSpirit 考试系统运行环境
- 测试用户数据（通过Excel导入到系统中）

### 1. 准备测试用户数据

编辑 `testusers.json` 文件，添加测试用户：

```json
[
  "student001",
  "student002", 
  "student003",
  "student004",
  "student005"
]
```

> 密码将自动基于用户名后6位生成（如 student001 → ent001123!）

### 2. 配置测试参数

编辑 `appsettings.json` 文件：

```json
{
  "TestConfiguration": {
    "BaseUrl": "https://localhost:7120",
    "IdentityApiUrl": "https://localhost:7120/identity",
    "ExamApiUrl": "https://localhost:7120/exam",
    "TenantId": "default",
    "ClientType": "exam",
    "TestUsersFile": "testusers.json"
  },
  "LoadTestScenarios": {
    "WarmUp": {
      "Duration": "00:02:00",
      "Rate": 10
    },
    "NormalLoad": {
      "Duration": "00:10:00", 
      "Rate": 40
    },
    "PeakLoad": {
      "Duration": "00:05:00",
      "Rate": 100
    },
    "StressTest": {
      "Duration": "00:03:00",
      "Rate": 200
    }
  }
}
```

#### 负载场景说明

- **WarmUp**: 预热场景，2分钟，10 RPS
- **NormalLoad**: 正常负载，10分钟，40 RPS  
- **PeakLoad**: 峰值负载，5分钟，100 RPS
- **StressTest**: 压力测试，3分钟，200 RPS

### 3. 启动 CodeSpirit 服务

**⚠️ 重要：在运行负载测试前，必须先启动 CodeSpirit 服务！**

```bash
# 在项目根目录启动协调程序
cd ../../Src/CodeSpirit.AppHost
dotnet run
```

等待服务启动完成，确认以下服务正常运行：
- 身份认证API (IdentityApi)
- 考试系统API (ExamApi)

### 4. 运行测试

#### 方式一：使用 PowerShell 脚本（推荐）

```powershell
# 运行预热场景
.\run-load-tests.ps1 -Scenario WarmUp

# 运行正常负载场景
.\run-load-tests.ps1 -Scenario NormalLoad

# 运行峰值负载场景  
.\run-load-tests.ps1 -Scenario PeakLoad

# 运行压力测试场景
.\run-load-tests.ps1 -Scenario StressTest

# 指定特定的测试场景
.\run-load-tests.ps1 -Scenario NormalLoad -TestScenario mixed-operations
```

#### 方式二：使用环境变量

```bash
# 设置负载场景环境变量
export LOAD_SCENARIO=PeakLoad  # Linux/Mac
# 或
$env:LOAD_SCENARIO="PeakLoad"  # Windows PowerShell

# 运行测试
dotnet run

# 运行特定测试场景
dotnet run -- full-exam-flow
dotnet run -- mixed-operations
dotnet run -- get-profile
```

#### 方式三：直接运行（使用默认配置）

```bash
# 返回测试项目目录
cd ../../Tests/CodeSpirit.ExamApi.LoadTests

# 安装依赖并构建项目
dotnet restore
dotnet build

# 运行默认测试（NormalLoad + full-exam-flow）
dotnet run

# 运行特定测试场景
dotnet run -- get-exam-light-info
dotnet run -- start-exam
dotnet run -- record-screen-switch
dotnet run -- mixed-operations
```

## 📊 测试场景

### 1. 完整考试流程 (full-exam-flow) ⭐
**默认场景**，包含所有接口的完整流程：
- 客户端登录获取Token
- 获取考生个人信息
- 获取可用考试列表
- 获取考试轻量信息
- 获取考试基本信息
- 开始考试
- 记录切屏事件

**负载模式**：
- 预热：3 用户/秒，持续 2 分钟
- 正常：10 用户/秒，持续 10 分钟
- 峰值：20 用户/秒，持续 5 分钟
- 降低：5 用户/秒，持续 3 分钟

### 2. 原始完整流程 (complete-flow)
原始的4步流程测试：
- 客户端登录获取Token
- 获取考生个人信息
- 获取可用考试列表
- 获取第一个可用考试的基本信息

### 2. 客户端登录测试 (client-login)
专门测试客户端登录接口的性能：
- 高并发登录压力测试
- Token获取和缓存机制测试

**负载模式**：
- 正常：10 用户/秒，持续 5 分钟
- 高并发：30 用户/秒，持续 3 分钟
- 恢复：5 用户/秒，持续 2 分钟

### 3. 获取考生个人信息测试 (get-profile)
测试个人信息接口的响应性能：
- 认证Token验证
- 个人信息查询性能

**负载模式**：
- 持续：15 用户/秒，持续 8 分钟

### 4. 获取可用考试测试 (get-available-exams)
测试考试列表查询性能：
- 考试数据查询优化
- 权限验证性能

**负载模式**：
- 持续：25 用户/秒，持续 6 分钟

### 5. 获取考试基本信息测试 (get-exam-basic-info)
测试考试详情查询性能：
- 考试详情和题目加载
- 数据量较大的响应处理

**负载模式**：
- 持续：20 用户/秒，持续 7 分钟

### 6. 获取考试轻量信息测试 (get-exam-light-info)
测试考试轻量信息接口性能：
- 倒计时页面数据加载
- 轻量级数据响应

**负载模式**：
- 持续：15 用户/秒，持续 6 分钟

### 7. 开始考试测试 (start-exam)
测试开始考试接口性能：
- 考试记录创建
- 高并发开始考试

**负载模式**：
- 持续：10 用户/秒，持续 8 分钟

### 8. 记录切屏事件测试 (record-screen-switch)
测试切屏监控接口性能：
- 切屏事件记录
- 监控数据写入

**负载模式**：
- 持续：20 用户/秒，持续 5 分钟

### 9. 混合操作测试 (mixed-operations)
模拟真实用户的随机操作行为：
- 随机调用所有接口
- 真实使用场景模拟

**负载模式**：
- 持续：30 用户/秒，持续 12 分钟

## 🔧 配置说明

### 测试用户配置

测试用户通过 `testusers.json` 文件配置，采用简化的数组格式：

- 直接使用用户名字符串数组
- 密码自动基于用户名后6位生成（格式：后6位 + "123!"）
- 例如：`student001` → 密码为 `ent001123!`

> 这种设计简化了用户数据管理，适合批量生成测试用户。

### 负载测试配置

在 `appsettings.json` 中的 `LoadTestScenarios` 部分可以调整负载参数：

```json
{
  "LoadTestScenarios": {
    "WarmUp": {
      "Duration": "00:02:00",
      "Rate": 5
    },
    "NormalLoad": {
      "Duration": "00:10:00", 
      "Rate": 20
    },
    "PeakLoad": {
      "Duration": "00:05:00",
      "Rate": 50
    }
  }
}
```

## 📈 性能监控和自定义指标

### 监控系统架构

本测试项目集成了完整的性能监控系统，包括：

1. **系统性能监控** - 实时收集系统资源使用情况
2. **自定义业务指标** - 记录业务相关的关键指标
3. **NBomber集成** - 与负载测试框架深度集成

### 关键性能指标 (KPI)

- **响应时间**：P95 < 2秒，P99 < 5秒
- **吞吐量**：支持 50+ RPS
- **成功率**：> 99%
- **并发用户**：支持 100+ 并发

### 系统性能监控

自动收集以下系统指标：
- **CPU 使用率** - 实时CPU使用百分比
- **内存使用** - 托管内存和工作集大小
- **线程数量** - 活动线程统计
- **垃圾回收** - GC各代回收次数
- **句柄数量** - 系统句柄使用情况

### 自定义业务指标

#### API调用指标
- `api_calls_total` - API调用总数（按接口、状态码分组）
- `api_response_time_ms` - API响应时间分布
- `api_calls_success` - 成功调用计数
- `api_calls_failed` - 失败调用计数

#### 认证指标
- `auth_attempts_total` - 认证尝试总数
- `auth_success_total` - 认证成功总数
- `auth_failed_total` - 认证失败总数
- `auth_response_time_ms` - 认证响应时间

#### 考试业务指标
- `exam_operations_total` - 考试操作总数
- `exam_operation_response_time_ms` - 考试操作响应时间
- `user_actions_total` - 用户行为统计
- `errors_total` - 错误统计（按类型分组）

#### 用户行为追踪
- `user_login_success` - 用户登录成功事件
- `exam_selected` - 考试选择事件
- `exam_started` - 考试开始事件
- `screen_switch_detected` - 切屏检测事件
- `full_exam_flow_completed` - 完整流程完成事件

## 📋 测试报告

测试完成后自动生成多种格式的报告：

### NBomber 标准报告
- **HTML报告** - 可视化测试结果和统计图表
- **CSV数据** - 原始测试数据，便于进一步分析
- **Markdown摘要** - 测试结果概览

### 性能监控报告
- **performance_metrics.json** - 详细的系统性能数据
- **性能统计摘要** - CPU、内存、线程使用统计

### 自定义指标报告
- **custom_metrics.json** - 完整的业务指标数据
- **指标统计分析** - 各类指标的统计信息

### 综合报告
- **comprehensive_report.md** - 整合所有监控数据的综合分析报告

## 🛠️ 自定义场景

### 添加新的测试场景

1. 在 `Scenarios/ExamClientScenarios.cs` 中添加新方法
2. 在 `Program.cs` 的 `CreateTestScenariosAsync` 方法中注册新场景
3. 使用 `--scenario=新场景名` 运行

### 修改负载模式

在场景方法中调整 `WithLoadSimulations` 参数：

```csharp
.WithLoadSimulations(
    Simulation.InjectPerSec(rate: 10, during: TimeSpan.FromMinutes(5)),
    Simulation.KeepConstant(copies: 20, during: TimeSpan.FromMinutes(10))
)
```

## 🚨 注意事项

### 测试环境要求

1. **系统运行状态**：确保 CodeSpirit 考试系统正常运行
2. **用户数据准备**：测试用户需要预先在系统中创建（通过Excel导入）
3. **网络连接**：确保测试机器能正常访问目标API
4. **权限配置**：测试用户需要有考生权限

### 性能基准建议

1. **从小规模开始**：先用5-10用户测试，建立基准
2. **逐步增加负载**：观察系统在不同负载下的表现
3. **监控系统资源**：关注CPU、内存、数据库性能
4. **分析错误模式**：重点关注失败请求的原因

### 故障排除

#### 常见问题

1. **认证失败**
   - 检查用户名密码是否正确
   - 确认租户ID配置
   - 验证用户是否存在于系统中

2. **连接超时**
   - 检查API地址配置
   - 确认网络连接
   - 调整HTTP超时时间

3. **权限错误**
   - 确认用户具有考生角色
   - 检查租户权限配置
   - 验证JWT Token有效性

#### 日志查看

- **应用日志**：`logs/loadtest-*.txt`
- **NBomber日志**：`reports/nbomber-log.txt`
- **控制台输出**：实时查看测试进度

## 🔧 故障排除

### 常见问题

#### 1. 登录失败 - 404 NotFound

**症状**：所有登录请求返回404错误
```
登录请求失败: username, 状态码: NotFound, 响应:
```

**解决方案**：
1. **检查服务状态**：确保 CodeSpirit.AppHost 正在运行
   ```bash
   cd ../../Src/CodeSpirit.AppHost
   dotnet run
   ```

2. **验证服务地址**：检查 `appsettings.json` 中的URL配置
   - 确认 `IdentityApiUrl` 和 `ExamApiUrl` 与实际服务地址匹配
   - 默认配置：`https://localhost:7120/identity` 和 `https://localhost:7120/exam`

3. **测试API连通性**：
   ```bash
   # 测试身份认证API
   curl -k https://localhost:7120/identity/api/auth/client/login
   
   # 测试考试API  
   curl -k https://localhost:7120/exam/api/client/profile
   ```

#### 2. 用户认证失败

**症状**：登录请求成功但认证失败

**解决方案**：
1. **检查测试用户**：确保 `testusers.json` 中的用户在系统中存在
2. **验证密码规则**：确认密码生成规则与系统要求匹配  
3. **检查租户配置**：验证 `TenantId` 配置正确

#### 3. 连接超时

**症状**：请求超时或连接被拒绝

**解决方案**：
1. **检查防火墙**：确保端口7120未被阻止
2. **验证HTTPS证书**：如果使用HTTPS，确保证书有效
3. **调整超时设置**：在代码中增加HTTP客户端超时时间

### 配置验证

运行测试前，请验证以下配置：

```json
{
  "TestConfiguration": {
    "IdentityApiUrl": "https://localhost:7120/identity",  // 身份认证API地址
    "ExamApiUrl": "https://localhost:7120/exam",          // 考试API地址  
    "TenantId": "default",                                // 租户ID
    "ClientType": "exam"                                  // 客户端类型
  }
}
```

### 📊 日志分析

项目提供了详细的执行日志，帮助您诊断问题和分析性能：

#### 日志级别和内容

1. **Information级别**：
   - 流程开始和完成信息
   - 成功的API调用统计
   - 关键业务事件

2. **Debug级别**：
   - 详细的执行步骤
   - API请求URL和参数
   - 每个步骤的耗时统计

3. **Warning级别**：
   - 认证失败
   - API调用失败（非异常）
   - 业务逻辑警告

4. **Error级别**：
   - 异常信息和堆栈跟踪
   - 系统级错误

#### 日志格式示例

```
[18:01:50 INF] [full_exam_flow] 开始执行完整考试流程，用户: student001
[18:01:50 DBG] [full_exam_flow] 步骤1: 用户登录，用户: student001
[18:01:50 DBG] [full_exam_flow] 步骤1成功: 用户登录完成，用户: student001, 耗时: 45.2ms
[18:01:50 DBG] [full_exam_flow] 步骤2: 获取用户档案，用户: student001
[18:01:50 INF] [full_exam_flow] 完整考试流程执行成功，用户: student001, 考试ID: 42, 总耗时: 234.7ms
```

#### 日志文件位置

- **应用日志**：`logs/loadtest-*.txt` - 详细的执行日志
- **NBomber日志**：`reports/nbomber-log-*.txt` - 框架级日志
- **控制台输出**：实时显示关键信息和错误

#### 日志配置

在 `appsettings.json` 中调整日志级别：

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "NBomber": "Information",
      "CodeSpirit.ExamApi.LoadTests": "Debug"
    }
  }
}
```

## 📞 技术支持

如遇到问题，请检查：

1. 系统日志文件
2. 测试报告中的错误统计
3. API响应状态码和错误信息
4. 系统资源使用情况

## 📄 许可证

本项目采用 MIT 许可证。
