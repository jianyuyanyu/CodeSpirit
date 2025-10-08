-- =============================================
-- CodeSpirit SQL Server 数据库删除脚本
-- 批量删除所有CodeSpirit相关数据库
-- =============================================

USE master;
GO

PRINT N'开始执行CodeSpirit数据库删除脚本...';
PRINT N'警告：此操作将永久删除所有数据，请确保已备份重要数据！';
PRINT '';

-- 声明变量
DECLARE @databases TABLE (
    DatabaseName NVARCHAR(128)
);

-- 插入需要删除的数据库列表
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
DECLARE @activeConnections INT;

-- 游标遍历删除数据库
DECLARE db_cursor CURSOR FOR 
SELECT DatabaseName FROM @databases;

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @dbName;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- 检查数据库是否存在
    IF EXISTS (SELECT name FROM sys.databases WHERE name = @dbName)
    BEGIN
        PRINT N'正在处理数据库: ' + @dbName;
        
        -- 检查是否有活动连接
        SELECT @activeConnections = COUNT(*)
        FROM sys.dm_exec_sessions 
        WHERE database_id = DB_ID(@dbName) AND session_id != @@SPID;
        
        IF @activeConnections > 0
        BEGIN
            PRINT N'  发现 ' + CAST(@activeConnections AS NVARCHAR(10)) + N' 个活动连接，正在强制断开...';
            
            -- 设置数据库为单用户模式，强制断开连接
            SET @sql = N'ALTER DATABASE [' + @dbName + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
            EXEC sp_executesql @sql;
            
            PRINT N'  活动连接已断开';
        END
        
        -- 删除数据库
        SET @sql = N'DROP DATABASE [' + @dbName + N'];';
        
        BEGIN TRY
            EXEC sp_executesql @sql;
            PRINT N'  ✓ 数据库 ' + @dbName + N' 删除成功';
        END TRY
        BEGIN CATCH
            PRINT N'  ✗ 删除数据库 ' + @dbName + N' 失败: ' + ERROR_MESSAGE();
        END CATCH
    END
    ELSE
    BEGIN
        PRINT N'  - 数据库 ' + @dbName + N' 不存在，跳过删除';
    END
    
    FETCH NEXT FROM db_cursor INTO @dbName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;

-- 可选：删除codespirit登录用户
PRINT '';
PRINT N'检查是否需要删除登录用户 codespirit...';

IF EXISTS (SELECT name FROM sys.server_principals WHERE name = 'codespirit')
BEGIN
    -- 检查用户是否还拥有其他数据库
    DECLARE @userDatabases INT;
    SELECT @userDatabases = COUNT(*)
    FROM sys.databases d
    INNER JOIN sys.database_principals dp ON d.database_id = DB_ID(d.name)
    WHERE dp.name = 'codespirit' AND d.name NOT IN (
        'master', 'tempdb', 'model', 'msdb'
    );
    
    IF @userDatabases = 0
    BEGIN
        PRINT N'用户 codespirit 不再拥有任何用户数据库';
        PRINT N'是否要删除登录用户 codespirit？';
        PRINT N'如需删除，请手动执行: DROP LOGIN codespirit;';
        PRINT N'注意：删除登录用户前请确保没有其他应用程序在使用此账户';
    END
    ELSE
    BEGIN
        PRINT N'用户 codespirit 仍拥有 ' + CAST(@userDatabases AS NVARCHAR(10)) + N' 个数据库，保留登录用户';
    END
END
ELSE
BEGIN
    PRINT N'登录用户 codespirit 不存在';
END

-- 验证删除结果
PRINT '';
PRINT N'=== 删除结果验证 ===';

-- 检查剩余的CodeSpirit数据库
DECLARE @remainingDbs TABLE (DatabaseName NVARCHAR(128));
INSERT INTO @remainingDbs
SELECT name FROM sys.databases 
WHERE name LIKE 'codespirit-%';

IF EXISTS (SELECT 1 FROM @remainingDbs)
BEGIN
    PRINT N'以下CodeSpirit数据库仍然存在：';
    DECLARE verify_cursor CURSOR FOR 
    SELECT DatabaseName FROM @remainingDbs;
    
    OPEN verify_cursor;
    FETCH NEXT FROM verify_cursor INTO @dbName;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        PRINT '  - ' + @dbName;
        FETCH NEXT FROM verify_cursor INTO @dbName;
    END
    
    CLOSE verify_cursor;
    DEALLOCATE verify_cursor;
END
ELSE
BEGIN
    PRINT N'✓ 所有CodeSpirit数据库已成功删除';
END

-- 显示当前所有数据库
PRINT '';
PRINT N'当前服务器上的所有数据库：';
SELECT name AS N'数据库名称', 
       create_date AS N'创建时间',
       CASE 
           WHEN name IN ('master', 'tempdb', 'model', 'msdb') THEN N'系统数据库'
           ELSE N'用户数据库'
       END AS N'类型'
FROM sys.databases 
ORDER BY 
    CASE WHEN name IN ('master', 'tempdb', 'model', 'msdb') THEN 1 ELSE 2 END,
    name;

PRINT '';
PRINT N'数据库删除脚本执行完成！';

GO
