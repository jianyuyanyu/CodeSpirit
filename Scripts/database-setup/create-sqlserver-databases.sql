-- =============================================
-- CodeSpirit SQL Server 数据库创建脚本
-- 创建用户、数据库并分配权限
-- =============================================

USE master;
GO

-- 检查并创建登录用户 codespirit
IF NOT EXISTS (SELECT name FROM sys.server_principals WHERE name = 'codespirit')
BEGIN
    CREATE LOGIN codespirit WITH PASSWORD = '123456abcD..', 
        DEFAULT_DATABASE = master,
        CHECK_EXPIRATION = OFF,
        CHECK_POLICY = OFF;
    PRINT N'登录用户 codespirit 创建成功';
END
ELSE
BEGIN
    PRINT N'登录用户 codespirit 已存在';
END
GO

-- 创建数据库和用户的通用脚本模板
DECLARE @databases TABLE (
    DatabaseName NVARCHAR(128)
);

-- 插入需要创建的数据库列表
INSERT INTO @databases VALUES 
    ('codespirit-identity'),
    ('codespirit-exam'),
    ('codespirit-messaging'),
    ('codespirit-config'),
    ('codespirit-settings'),
    ('codespirit-file'),
    ('codespirit-survey');

DECLARE @dbName NVARCHAR(128);
DECLARE @sql NVARCHAR(MAX);

-- 游标遍历创建数据库
DECLARE db_cursor CURSOR FOR 
SELECT DatabaseName FROM @databases;

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 检查数据库是否存在
    IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = @dbName)
    BEGIN
        -- 创建数据库
        SET @sql = N'CREATE DATABASE [' + @dbName + N'] 
        COLLATE Chinese_PRC_CI_AS
        ON (
            NAME = ''' + @dbName + N''',
            FILENAME = ''C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\' + @dbName + N'.mdf'',
            SIZE = 100MB,
            MAXSIZE = UNLIMITED,
            FILEGROWTH = 10MB
        )
        LOG ON (
            NAME = ''' + @dbName + N'_Log'',
            FILENAME = ''C:\Program Files\Microsoft SQL Server\MSSQL16.MSSQLSERVER\MSSQL\DATA\' + @dbName + N'_Log.ldf'',
            SIZE = 10MB,
            MAXSIZE = UNLIMITED,
            FILEGROWTH = 10%
        );';
        
        EXEC sp_executesql @sql;
        PRINT N'数据库 ' + @dbName + N' 创建成功';
        
        -- 切换到新创建的数据库并创建用户
        SET @sql = N'USE [' + @dbName + N'];
        
        -- 创建数据库用户
        IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = ''codespirit'')
        BEGIN
            CREATE USER codespirit FOR LOGIN codespirit;
            PRINT N'用户 codespirit 在数据库 ' + @dbName + N' 中创建成功';
        END
        
        -- 分配 db_owner 权限
        ALTER ROLE db_owner ADD MEMBER codespirit;
        PRINT N'用户 codespirit 已获得数据库 ' + @dbName + N' 的 owner 权限';';
        
        EXEC sp_executesql @sql;
    END
    ELSE
    BEGIN
        PRINT N'数据库 ' + @dbName + N' 已存在，跳过创建';
        
        -- 即使数据库存在，也要确保用户和权限正确设置
        SET @sql = N'USE [' + @dbName + N'];
        
        -- 创建数据库用户（如果不存在）
        IF NOT EXISTS (SELECT name FROM sys.database_principals WHERE name = ''codespirit'')
        BEGIN
            CREATE USER codespirit FOR LOGIN codespirit;
            PRINT N'用户 codespirit 在数据库 ' + @dbName + N' 中创建成功';
        END
        
        -- 确保用户拥有 db_owner 权限
        IF NOT IS_ROLEMEMBER(''db_owner'', ''codespirit'') = 1
        BEGIN
            ALTER ROLE db_owner ADD MEMBER codespirit;
            PRINT N'用户 codespirit 已获得数据库 ' + @dbName + N' 的 owner 权限';
        END
        ELSE
        BEGIN
            PRINT N'用户 codespirit 在数据库 ' + @dbName + N' 中已拥有 owner 权限';
        END';
        
        EXEC sp_executesql @sql;
    END
    
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;

-- 验证创建结果
PRINT '';
PRINT N'=== 创建结果验证 ===';

-- 验证登录用户
IF EXISTS (SELECT name FROM sys.server_principals WHERE name = 'codespirit')
    PRINT N'✓ 登录用户 codespirit 存在';
ELSE
    PRINT N'✗ 登录用户 codespirit 不存在';

-- 验证数据库
DECLARE verify_cursor CURSOR FOR 
SELECT DatabaseName FROM @databases;

OPEN verify_cursor;
FETCH NEXT FROM verify_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS (SELECT name FROM sys.databases WHERE name = @dbName)
        PRINT N'✓ 数据库 ' + @dbName + N' 存在';
    ELSE
        PRINT N'✗ 数据库 ' + @dbName + N' 不存在';
    
    FETCH NEXT FROM verify_cursor INTO @dbName;
END

CLOSE verify_cursor;
DEALLOCATE verify_cursor;

PRINT '';
PRINT N'数据库创建脚本执行完成！';
PRINT N'请使用以下连接字符串测试连接：';
PRINT 'Server=10.0.1.x;Database={数据库名};User ID=codespirit;Password=123456abcD..;TrustServerCertificate=True;';

GO
