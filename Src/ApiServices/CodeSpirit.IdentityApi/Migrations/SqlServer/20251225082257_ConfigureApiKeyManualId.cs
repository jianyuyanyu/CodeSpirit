using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeSpirit.IdentityApi.Migrations.SqlServer
{
    /// <inheritdoc />
    public partial class ConfigureApiKeyManualId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 检查表是否存在并移除 IDENTITY
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ApiKeys')
                BEGIN
                    -- 如果表存在且 Id 列是 IDENTITY
                    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApiKeys') AND name = 'Id' AND is_identity = 1)
                    BEGIN
                        -- 使用动态 SQL 来重建列
                        DECLARE @sql NVARCHAR(MAX);
                        
                        -- 1. 删除外键约束
                        IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ApiKeys_ApplicationUser_UserId')
                            ALTER TABLE [ApiKeys] DROP CONSTRAINT [FK_ApiKeys_ApplicationUser_UserId];
                        
                        -- 2. 删除主键约束
                        IF EXISTS (SELECT * FROM sys.key_constraints WHERE name = 'PK_ApiKeys')
                            ALTER TABLE [ApiKeys] DROP CONSTRAINT [PK_ApiKeys];
                        
                        -- 3. 添加临时列
                        SET @sql = 'ALTER TABLE [ApiKeys] ADD [Id_Temp] bigint NULL';
                        EXEC sp_executesql @sql;
                        
                        -- 4. 复制数据
                        SET @sql = 'UPDATE [ApiKeys] SET [Id_Temp] = [Id]';
                        EXEC sp_executesql @sql;
                        
                        -- 5. 删除原列
                        SET @sql = 'ALTER TABLE [ApiKeys] DROP COLUMN [Id]';
                        EXEC sp_executesql @sql;
                        
                        -- 6. 重命名临时列
                        EXEC sp_rename 'ApiKeys.Id_Temp', 'Id', 'COLUMN';
                        
                        -- 7. 设置为 NOT NULL
                        SET @sql = 'ALTER TABLE [ApiKeys] ALTER COLUMN [Id] bigint NOT NULL';
                        EXEC sp_executesql @sql;
                        
                        -- 8. 重命名表
                        EXEC sp_rename 'ApiKeys', 'ApiKey';
                        
                        -- 9. 重命名索引
                        IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_UserId' AND object_id = OBJECT_ID('ApiKey'))
                            EXEC sp_rename 'ApiKey.IX_ApiKeys_UserId', 'IX_ApiKey_UserId', 'INDEX';
                        
                        -- 10. 添加主键
                        SET @sql = 'ALTER TABLE [ApiKey] ADD CONSTRAINT [PK_ApiKey] PRIMARY KEY ([Id])';
                        EXEC sp_executesql @sql;
                        
                        -- 11. 创建新索引
                        SET @sql = 'CREATE INDEX [IX_ApiKey_ExpiresAt] ON [ApiKey] ([ExpiresAt])';
                        EXEC sp_executesql @sql;
                        
                        SET @sql = 'CREATE INDEX [IX_ApiKey_IsActive] ON [ApiKey] ([IsActive])';
                        EXEC sp_executesql @sql;
                        
                        SET @sql = 'CREATE INDEX [IX_ApiKey_KeyHash] ON [ApiKey] ([KeyHash])';
                        EXEC sp_executesql @sql;
                        
                        SET @sql = 'CREATE INDEX [IX_ApiKey_TenantId_UserId] ON [ApiKey] ([TenantId], [UserId])';
                        EXEC sp_executesql @sql;
                        
                        -- 12. 重新添加外键
                        SET @sql = 'ALTER TABLE [ApiKey] ADD CONSTRAINT [FK_ApiKey_ApplicationUser_UserId] FOREIGN KEY ([UserId]) REFERENCES [ApplicationUser] ([Id]) ON DELETE CASCADE';
                        EXEC sp_executesql @sql;
                    END
                    ELSE
                    BEGIN
                        -- 如果列不是 IDENTITY，直接重命名表和索引
                        EXEC sp_rename 'ApiKeys', 'ApiKey';
                        
                        IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ApiKeys_UserId' AND object_id = OBJECT_ID('ApiKey'))
                            EXEC sp_rename 'ApiKey.IX_ApiKeys_UserId', 'IX_ApiKey_UserId', 'INDEX';
                    END
                END
                ELSE
                BEGIN
                    -- 表不存在，什么都不做（可能是新安装）
                    PRINT 'ApiKeys table does not exist, skipping migration';
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApiKey_ApplicationUser_UserId",
                table: "ApiKey");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ApiKey",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_ExpiresAt",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_IsActive",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_KeyHash",
                table: "ApiKey");

            migrationBuilder.DropIndex(
                name: "IX_ApiKey_TenantId_UserId",
                table: "ApiKey");

            migrationBuilder.RenameTable(
                name: "ApiKey",
                newName: "ApiKeys");

            migrationBuilder.RenameIndex(
                name: "IX_ApiKey_UserId",
                table: "ApiKeys",
                newName: "IX_ApiKeys_UserId");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "ApiKeys",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ApiKeys",
                table: "ApiKeys",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApiKeys_ApplicationUser_UserId",
                table: "ApiKeys",
                column: "UserId",
                principalTable: "ApplicationUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
