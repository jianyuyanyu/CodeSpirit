-- =============================================
-- CodeSpirit SQL Server 数据库交互式删除脚本
-- 需要用户确认的安全删除脚本
-- =============================================

USE master;
GO

PRINT '========================================';
PRINT N'CodeSpirit 数据库删除脚本';
PRINT '========================================';
PRINT '';
PRINT N'此脚本将删除以下数据库：';
PRINT '  • codespirit-identity';
PRINT '  • codespirit-exam';
PRINT '  • codespirit-messaging';
PRINT '  • codespirit-config';
PRINT '  • codespirit-settings';
PRINT '  • codespirit-file';
PRINT '  • codespirit-survey';
PRINT '';
PRINT N'⚠️  警告：此操作不可逆转！';
PRINT N'⚠️  所有数据将被永久删除！';
PRINT N'⚠️  请确保已备份重要数据！';
PRINT '';

-- 显示当前存在的CodeSpirit数据库
PRINT N'当前存在的CodeSpirit数据库：';
IF EXISTS (SELECT 1 FROM sys.databases WHERE name LIKE 'codespirit-%')
BEGIN
    SELECT 
        name AS N'数据库名称',
        create_date AS N'创建时间',
        CASE 
            WHEN state_desc = 'ONLINE' THEN N'在线'
            WHEN state_desc = 'OFFLINE' THEN N'离线'
            ELSE state_desc
        END AS N'状态'
    FROM sys.databases 
    WHERE name LIKE 'codespirit-%'
    ORDER BY name;
END
ELSE
BEGIN
    PRINT N'  没有找到CodeSpirit数据库';
END

PRINT '';
PRINT N'如果要继续删除，请执行以下步骤：';
PRINT N'1. 确认已备份所有重要数据';
PRINT N'2. 确认没有应用程序正在使用这些数据库';
PRINT N'3. 将下面的确认代码取消注释并执行';
PRINT '';
PRINT N'-- 取消注释下面的代码以执行删除操作';
PRINT '-- EXEC sp_executesql N''EXEC master.dbo.sp_delete_codespirit_databases'';';

GO

-- 创建删除存储过程
CREATE OR ALTER PROCEDURE sp_delete_codespirit_databases
AS
BEGIN
    SET NOCOUNT ON;
    
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
    DECLARE @deletedCount INT = 0;
    DECLARE @skippedCount INT = 0;
    
    PRINT N'开始执行删除操作...';
    PRINT '';
    
    -- 游标遍历删除数据库
    DECLARE db_cursor CURSOR FOR 
    SELECT DatabaseName FROM @databases;
    
    OPEN db_cursor;
    FETCH NEXT FROM db_cursor INTO @dbName;
    
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF EXISTS (SELECT name FROM sys.databases WHERE name = @dbName)
        BEGIN
            PRINT N'正在删除数据库: ' + @dbName;
            
            -- 强制断开所有连接
            SET @sql = N'ALTER DATABASE [' + @dbName + N'] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;';
            
            BEGIN TRY
                EXEC sp_executesql @sql;
                
                -- 删除数据库
                SET @sql = N'DROP DATABASE [' + @dbName + N'];';
                EXEC sp_executesql @sql;
                
                PRINT N'  ✓ 删除成功';
                SET @deletedCount = @deletedCount + 1;
                
            END TRY
            BEGIN CATCH
                PRINT N'  ✗ 删除失败: ' + ERROR_MESSAGE();
                
                -- 尝试恢复为多用户模式
                BEGIN TRY
                    SET @sql = N'ALTER DATABASE [' + @dbName + N'] SET MULTI_USER;';
                    EXEC sp_executesql @sql;
                END TRY
                BEGIN CATCH
                    -- 忽略恢复失败的错误
                END CATCH
            END CATCH
        END
        ELSE
        BEGIN
            PRINT N'数据库 ' + @dbName + N' 不存在，跳过';
            SET @skippedCount = @skippedCount + 1;
        END
        
        FETCH NEXT FROM db_cursor INTO @dbName;
    END
    
    CLOSE db_cursor;
    DEALLOCATE db_cursor;
    
    PRINT '';
    PRINT N'删除操作完成！';
    PRINT N'成功删除: ' + CAST(@deletedCount AS NVARCHAR(10)) + N' 个数据库';
    PRINT N'跳过删除: ' + CAST(@skippedCount AS NVARCHAR(10)) + N' 个数据库';
    
    -- 验证结果
    IF EXISTS (SELECT 1 FROM sys.databases WHERE name LIKE 'codespirit-%')
    BEGIN
        PRINT '';
        PRINT N'以下数据库仍然存在：';
        SELECT name AS N'剩余数据库' FROM sys.databases WHERE name LIKE 'codespirit-%';
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT N'✓ 所有CodeSpirit数据库已成功删除';
    END
END
GO

PRINT '';
PRINT N'存储过程 sp_delete_codespirit_databases 已创建';
PRINT '';
PRINT N'要执行删除操作，请取消注释并执行以下命令：';
PRINT '-- EXEC sp_delete_codespirit_databases;';
PRINT '';
PRINT N'执行后可以删除存储过程：';
PRINT '-- DROP PROCEDURE sp_delete_codespirit_databases;';

GO
