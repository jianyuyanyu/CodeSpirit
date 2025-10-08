-- =============================================
-- CodeSpirit 快速数据库删除脚本
-- 简单直接的删除脚本，无需确认
-- =============================================

USE master;
GO

PRINT N'CodeSpirit 快速数据库删除脚本';
PRINT '==============================';

-- 删除 codespirit-identity
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-identity')
BEGIN
    ALTER DATABASE [codespirit-identity] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [codespirit-identity];
    PRINT N'✓ codespirit-identity 已删除';
END
ELSE
    PRINT N'- codespirit-identity 不存在';

-- 删除 codespirit-exam
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-exam')
BEGIN
    ALTER DATABASE [codespirit-exam] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [codespirit-exam];
    PRINT N'✓ codespirit-exam 已删除';
END
ELSE
    PRINT N'- codespirit-exam 不存在';

-- 删除 codespirit-messaging
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-messaging')
BEGIN
    ALTER DATABASE [codespirit-messaging] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [codespirit-messaging];
    PRINT N'✓ codespirit-messaging 已删除';
END
ELSE
    PRINT N'- codespirit-messaging 不存在';

-- 删除 codespirit-config
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-config')
BEGIN
    ALTER DATABASE [codespirit-config] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [codespirit-config];
    PRINT N'✓ codespirit-config 已删除';
END
ELSE
    PRINT N'- codespirit-config 不存在';

-- 删除 codespirit-settings
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-settings')
BEGIN
    ALTER DATABASE [codespirit-settings] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [codespirit-settings];
    PRINT N'✓ codespirit-settings 已删除';
END
ELSE
    PRINT N'- codespirit-settings 不存在';

-- 删除 codespirit-file
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-file')
BEGIN
    ALTER DATABASE [codespirit-file] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [codespirit-file];
    PRINT N'✓ codespirit-file 已删除';
END
ELSE
    PRINT N'- codespirit-file 不存在';

-- 删除 codespirit-survey
IF EXISTS (SELECT name FROM sys.databases WHERE name = 'codespirit-survey')
BEGIN
    ALTER DATABASE [codespirit-survey] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [codespirit-survey];
    PRINT N'✓ codespirit-survey 已删除';
END
ELSE
    PRINT N'- codespirit-survey 不存在';

PRINT '';
PRINT N'删除操作完成！';

-- 验证删除结果
IF EXISTS (SELECT 1 FROM sys.databases WHERE name LIKE 'codespirit-%')
BEGIN
    PRINT '';
    PRINT N'以下数据库仍然存在：';
    SELECT name AS N'剩余数据库' FROM sys.databases WHERE name LIKE 'codespirit-%';
END
ELSE
BEGIN
    PRINT N'✓ 所有CodeSpirit数据库已成功删除';
END

GO
