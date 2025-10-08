-- =============================================
-- CodeSpirit 简化版数据库创建脚本
-- 直接在 SSMS 或 sqlcmd 中执行
-- =============================================

USE master;
GO

-- 创建登录用户
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'codespirit')
BEGIN
    CREATE LOGIN codespirit WITH PASSWORD = '123456abcD..';
    PRINT N'登录用户 codespirit 创建成功';
END
GO

-- 创建 codespirit-identity 数据库
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-identity')
BEGIN
    CREATE DATABASE [codespirit-identity];
    PRINT N'数据库 codespirit-identity 创建成功';
END
GO

USE [codespirit-identity];
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'codespirit')
BEGIN
    CREATE USER codespirit FOR LOGIN codespirit;
    ALTER ROLE db_owner ADD MEMBER codespirit;
    PRINT N'用户 codespirit 在 codespirit-identity 中获得 owner 权限';
END
GO

-- 创建 codespirit-exam 数据库
USE master;
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-exam')
BEGIN
    CREATE DATABASE [codespirit-exam];
    PRINT N'数据库 codespirit-exam 创建成功';
END
GO

USE [codespirit-exam];
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'codespirit')
BEGIN
    CREATE USER codespirit FOR LOGIN codespirit;
    ALTER ROLE db_owner ADD MEMBER codespirit;
    PRINT N'用户 codespirit 在 codespirit-exam 中获得 owner 权限';
END
GO

-- 创建 codespirit-messaging 数据库
USE master;
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-messaging')
BEGIN
    CREATE DATABASE [codespirit-messaging];
    PRINT N'数据库 codespirit-messaging 创建成功';
END
GO

USE [codespirit-messaging];
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'codespirit')
BEGIN
    CREATE USER codespirit FOR LOGIN codespirit;
    ALTER ROLE db_owner ADD MEMBER codespirit;
    PRINT N'用户 codespirit 在 codespirit-messaging 中获得 owner 权限';
END
GO

-- 创建 codespirit-config 数据库
USE master;
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-config')
BEGIN
    CREATE DATABASE [codespirit-config];
    PRINT N'数据库 codespirit-config 创建成功';
END
GO

USE [codespirit-config];
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'codespirit')
BEGIN
    CREATE USER codespirit FOR LOGIN codespirit;
    ALTER ROLE db_owner ADD MEMBER codespirit;
    PRINT N'用户 codespirit 在 codespirit-config 中获得 owner 权限';
END
GO

-- 创建 codespirit-settings 数据库
USE master;
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-settings')
BEGIN
    CREATE DATABASE [codespirit-settings];
    PRINT N'数据库 codespirit-settings 创建成功';
END
GO

USE [codespirit-settings];
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'codespirit')
BEGIN
    CREATE USER codespirit FOR LOGIN codespirit;
    ALTER ROLE db_owner ADD MEMBER codespirit;
    PRINT N'用户 codespirit 在 codespirit-settings 中获得 owner 权限';
END
GO

-- 创建 codespirit-file 数据库
USE master;
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-file')
BEGIN
    CREATE DATABASE [codespirit-file];
    PRINT N'数据库 codespirit-file 创建成功';
END
GO

USE [codespirit-file];
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'codespirit')
BEGIN
    CREATE USER codespirit FOR LOGIN codespirit;
    ALTER ROLE db_owner ADD MEMBER codespirit;
    PRINT N'用户 codespirit 在 codespirit-file 中获得 owner 权限';
END
GO

-- 创建 codespirit-survey 数据库
USE master;
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-survey')
BEGIN
    CREATE DATABASE [codespirit-survey];
    PRINT N'数据库 codespirit-survey 创建成功';
END
GO

USE [codespirit-survey];
IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = 'codespirit')
BEGIN
    CREATE USER codespirit FOR LOGIN codespirit;
    ALTER ROLE db_owner ADD MEMBER codespirit;
    PRINT N'用户 codespirit 在 codespirit-survey 中获得 owner 权限';
END
GO

-- 验证结果
USE master;
PRINT '';
PRINT N'=== 数据库创建完成 ===';
SELECT name as N'已创建的数据库' FROM sys.databases 
WHERE name LIKE 'codespirit-%' 
ORDER BY name;
GO
