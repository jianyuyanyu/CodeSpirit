using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.SurveyApi.Migrations;

/// <inheritdoc />
public partial class PopulatePublicAccessCodes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 为现有问卷生成访问码的SQL脚本
        migrationBuilder.Sql(@"
            -- 为现有问卷生成访问码
            DECLARE @SurveyId INT;
            DECLARE @AccessCode NVARCHAR(16);
            DECLARE @Counter INT = 0;
            DECLARE @TotalUpdated INT = 0;

            -- 游标遍历所有没有访问码的问卷
            DECLARE survey_cursor CURSOR FOR
            SELECT Id FROM Surveys WHERE PublicAccessCode IS NULL OR PublicAccessCode = '';

            OPEN survey_cursor;
            FETCH NEXT FROM survey_cursor INTO @SurveyId;

            WHILE @@FETCH_STATUS = 0
            BEGIN
                -- 生成唯一的8位访问码
                SET @Counter = 0;
                WHILE @Counter < 50 -- 最多尝试50次
                BEGIN
                    -- 生成8位随机字符串（小写字母和数字）
                    SET @AccessCode = '';
                    DECLARE @i INT = 0;
                    WHILE @i < 8
                    BEGIN
                        DECLARE @CharIndex INT = ABS(CHECKSUM(NEWID())) % 36;
                        IF @CharIndex < 26
                            SET @AccessCode = @AccessCode + CHAR(97 + @CharIndex); -- a-z
                        ELSE
                            SET @AccessCode = @AccessCode + CHAR(48 + @CharIndex - 26); -- 0-9
                        SET @i = @i + 1;
                    END;

                    -- 检查访问码是否已存在
                    IF NOT EXISTS (SELECT 1 FROM Surveys WHERE PublicAccessCode = @AccessCode)
                    BEGIN
                        -- 访问码唯一，更新问卷
                        UPDATE Surveys SET PublicAccessCode = @AccessCode WHERE Id = @SurveyId;
                        SET @TotalUpdated = @TotalUpdated + 1;
                        BREAK;
                    END;

                    SET @Counter = @Counter + 1;
                END;

                FETCH NEXT FROM survey_cursor INTO @SurveyId;
            END;

            CLOSE survey_cursor;
            DEALLOCATE survey_cursor;

            PRINT 'AccessCode generation completed, updated ' + CAST(@TotalUpdated AS NVARCHAR(10)) + ' surveys';
        ");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 回滚时清空访问码
        migrationBuilder.Sql("UPDATE Surveys SET PublicAccessCode = NULL;");
    }
}
