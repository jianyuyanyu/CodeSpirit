# CodeSpirit SQL Server 数据库创建脚本

本目录包含用于创建CodeSpirit项目所需SQL Server数据库的脚本集合。

## 📋 脚本概览

### 🎯 主要脚本

#### 数据库创建脚本

| 脚本文件 | 描述 | 推荐场景 |
|---------|------|----------|
| `create-sqlserver-databases.ps1` | PowerShell自动化脚本 | ⭐ 推荐：自动化部署 |
| `create-sqlserver-databases.sql` | 完整版SQL脚本 | 高级用户，需要自定义 |
| `create-databases-simple.sql` | 简化版SQL脚本 | 快速部署，SSMS执行 |

#### 数据库删除脚本 🗑️

| 脚本文件 | 描述 | 安全级别 | 推荐场景 |
|---------|------|----------|----------|
| `drop-databases-interactive.sql` | 交互式删除脚本 | 🛡️ 最安全 | ⭐ 推荐：生产环境清理 |
| `drop-databases.sql` | 完整版删除脚本 | ⚠️ 中等 | 开发环境批量清理 |
| `drop-databases-quick.sql` | 快速删除脚本 | ⚡ 直接执行 | 测试环境快速重置 |

## 🚀 快速开始

### 创建数据库

#### 方法一：PowerShell脚本（推荐）

```powershell
# 进入脚本目录
cd Scripts\database-setup

# 使用SQL Server身份验证
.\create-sqlserver-databases.ps1 -ServerInstance "10.0.1.15" -AdminUser "sa"

# 使用Windows身份验证
.\create-sqlserver-databases.ps1 -ServerInstance "10.0.1.15" -UseWindowsAuth
```

#### 方法二：SSMS执行简化脚本

1. 打开SQL Server Management Studio
2. 连接到服务器 `10.0.1.15`
3. 打开 `create-databases-simple.sql`
4. 执行脚本

#### 方法三：命令行执行

```cmd
sqlcmd -S 10.0.1.15 -U sa -P YourPassword -i create-databases-simple.sql
```

### 删除数据库 🗑️

#### 安全删除（推荐）

```sql
-- 在SSMS中执行交互式删除脚本
-- 1. 打开 drop-databases-interactive.sql
-- 2. 执行脚本查看要删除的数据库
-- 3. 按提示取消注释确认代码
-- 4. 执行删除操作
```

#### 快速删除（测试环境）

```sql
-- 直接在SSMS中执行
:r drop-databases-quick.sql

-- 或使用sqlcmd
sqlcmd -S 10.0.1.15 -U sa -P YourPassword -i drop-databases-quick.sql
```

## 📊 创建的数据库

脚本将创建以下数据库，并为 `codespirit` 用户分配owner权限：

| 数据库名 | 用途 | 对应API服务 |
|---------|------|-------------|
| `codespirit-identity` | 身份认证系统 | CodeSpirit.IdentityApi |
| `codespirit-exam` | 考试系统 | CodeSpirit.ExamApi |
| `codespirit-messaging` | 消息服务 | CodeSpirit.MessagingApi |
| `codespirit-config` | 配置中心 | CodeSpirit.ConfigCenter |
| `codespirit-settings` | 设置管理 | CodeSpirit.Settings |
| `codespirit-file` | 文件存储 | CodeSpirit.FileStorageApi |
| `codespirit-survey` | 问卷调查 | CodeSpirit.SurveyApi |

## 🔐 用户和权限

### 创建的用户
- **登录名**: `codespirit`
- **密码**: `123456abcD..`
- **权限**: 每个数据库的 `db_owner` 角色

### 连接字符串格式
```
Server=10.0.1.x;Database={数据库名};User ID=codespirit;Password=123456abcD..;TrustServerCertificate=True;
```

## ⚙️ PowerShell脚本参数

### create-sqlserver-databases.ps1

| 参数 | 类型 | 默认值 | 描述 |
|------|------|--------|------|
| `-ServerInstance` | String | `10.0.1.x` | SQL Server实例地址 |
| `-AdminUser` | String | `sa` | 管理员用户名 |
| `-AdminPassword` | String | - | 管理员密码（可选，会提示输入） |
| `-UseWindowsAuth` | Switch | `false` | 使用Windows身份验证 |

### 使用示例

```powershell
# 基本用法
.\create-sqlserver-databases.ps1

# 指定服务器和用户
.\create-sqlserver-databases.ps1 -ServerInstance "192.168.1.100" -AdminUser "admin"

# 使用Windows身份验证
.\create-sqlserver-databases.ps1 -UseWindowsAuth

# 指定所有参数
.\create-sqlserver-databases.ps1 -ServerInstance "10.0.1.x" -AdminUser "sa" -AdminPassword "MyPassword"
```

## 🛠️ 脚本特性

### ✅ 幂等性
- 可以重复执行，不会重复创建已存在的数据库或用户
- 自动检测并跳过已存在的资源

### 📝 详细日志
- 提供创建过程的详细反馈
- 显示每个步骤的执行状态
- 最后提供验证结果摘要

### 🔍 自动验证
- 执行后自动验证所有数据库是否创建成功
- 检查用户权限是否正确分配
- 提供连接字符串示例

### ⚡ 错误处理
- 完善的异常处理机制
- 提供详细的错误信息和解决建议
- 自动安装必要的PowerShell模块

## 🗑️ 数据库删除详细说明

### 删除脚本对比

#### 1. drop-databases-interactive.sql（最安全）
- **特点**: 需要用户手动确认才执行删除
- **安全性**: 🛡️ 最高，防止误操作
- **适用场景**: 生产环境、重要数据清理
- **使用方式**: 
  1. 执行脚本查看要删除的数据库列表
  2. 手动取消注释确认代码
  3. 再次执行完成删除

#### 2. drop-databases.sql（功能完整）
- **特点**: 自动处理连接断开，提供详细日志
- **安全性**: ⚠️ 中等，执行后立即删除
- **适用场景**: 开发环境批量清理
- **功能**: 
  - 自动断开活动连接
  - 详细的执行日志
  - 删除结果验证
  - 可选的用户删除提示

#### 3. drop-databases-quick.sql（最快速）
- **特点**: 直接删除，无额外功能
- **安全性**: ⚡ 立即执行，无确认
- **适用场景**: 测试环境快速重置
- **优势**: 代码简洁，执行快速

### 删除操作注意事项

⚠️ **重要警告**
- 删除操作不可逆转
- 所有数据将永久丢失
- 请在删除前备份重要数据
- 确保没有应用程序正在使用这些数据库

### 删除前检查清单

- [ ] 确认要删除的数据库列表
- [ ] 备份重要数据
- [ ] 停止相关应用程序
- [ ] 确认没有其他用户连接到数据库
- [ ] 选择合适的删除脚本

### 删除后验证

```sql
-- 检查是否还有CodeSpirit数据库
SELECT name FROM sys.databases WHERE name LIKE 'codespirit-%';

-- 检查codespirit用户是否还拥有其他数据库
SELECT d.name 
FROM sys.databases d
INNER JOIN sys.database_principals dp ON d.database_id = DB_ID(d.name)
WHERE dp.name = 'codespirit' AND d.name NOT IN ('master', 'tempdb', 'model', 'msdb');
```

## 🔧 故障排除

### 1. 连接失败
**可能原因：**
- SQL Server服务未启动
- 防火墙阻止连接
- 用户名或密码错误
- 网络连接问题

**解决方案：**
```powershell
# 检查SQL Server服务状态
Get-Service -Name "MSSQLSERVER"

# 测试网络连接
Test-NetConnection -ComputerName "10.0.1.x" -Port 1433
```

### 2. 权限不足
**错误信息：** `CREATE DATABASE permission denied`

**解决方案：**
- 确保使用的管理员账户具有 `sysadmin` 权限
- 检查SQL Server配置是否允许SQL Server身份验证

### 3. PowerShell模块问题
**错误信息：** `SqlServer module not found`

**解决方案：**
```powershell
# 手动安装SqlServer模块
Install-Module -Name SqlServer -Force -AllowClobber -Scope CurrentUser
```

### 4. 数据库文件路径问题
如果默认数据库文件路径不正确，请修改SQL脚本中的路径：

```sql
-- 修改为实际的SQL Server数据文件路径
FILENAME = 'C:\YourPath\Data\' + @dbName + N'.mdf'
```

## 📋 执行前检查清单

- [ ] SQL Server服务正在运行
- [ ] 网络连接正常（可访问10.0.1.x:1433）
- [ ] 管理员账户具有足够权限
- [ ] PowerShell执行策略允许运行脚本
- [ ] 磁盘空间充足（每个数据库初始100MB）

## 🔄 执行后验证

执行脚本后，可以通过以下方式验证：

```sql
-- 检查创建的数据库
SELECT name, database_id, create_date 
FROM sys.databases 
WHERE name LIKE 'codespirit-%'
ORDER BY name;

-- 检查用户权限
USE [codespirit-identity];
SELECT 
    dp.name AS principal_name,
    dp.type_desc AS principal_type,
    r.name AS role_name
FROM sys.database_role_members rm
JOIN sys.database_principals dp ON rm.member_principal_id = dp.principal_id
JOIN sys.database_principals r ON rm.role_principal_id = r.principal_id
WHERE dp.name = 'codespirit';
```

## 📞 技术支持

如果遇到问题，请检查：

1. **日志输出** - 脚本会提供详细的执行日志
2. **SQL Server日志** - 查看SQL Server错误日志
3. **Windows事件日志** - 检查系统和应用程序日志
4. **网络连接** - 确保可以访问目标SQL Server实例

---

**注意**: 这些脚本设计用于开发和测试环境。在生产环境中使用前，请根据实际需求调整数据库配置和安全设置。
