# 提交前验证报告

**验证时间**：{timestamp}  
**分支**：{branch}  
**提交哈希**：{commit_hash}  
**待提交文件数**：{file_count}

---

## 执行摘要

| 验证阶段 | 状态 | 问题数 | 耗时 |
|---------|------|--------|------|
| 静态代码审查 | ✅/⚠️/❌ | {code_review_issues} | {code_review_duration} |
| 运行时检查 | ✅/⚠️/❌ | {runtime_issues} | {runtime_duration} |
| 功能验证 | ✅/⚠️/❌ | {functional_issues} | {functional_duration} |

**整体评估**：✅ 可以提交 / ⚠️ 有警告但可提交 / ❌ 不建议提交

**总体耗时**：{total_duration}

---

## 1. 静态代码审查结果

### 审查范围

**修改的文件**：
- {modified_file_1}
- {modified_file_2}
- ...

**新增的文件**：
- {new_file_1}
- {new_file_2}
- ...

### 问题统计

| 严重程度 | 数量 | 状态 |
|---------|------|------|
| 🔴 严重级别 | {critical_count} | {critical_status} |
| 🟡 重要级别 | {important_count} | {important_status} |
| 🟢 建议级别 | {suggestion_count} | {suggestion_status} |

### 严重问题详情

{critical_issues_list}

**示例格式**：
```
1. **安全问题**：密码字段未使用 [JsonIgnore]
   - 文件：`Src/ApiServices/CodeSpirit.IdentityApi/Dtos/CreateUserDto.cs`
   - 行号：15
   - 修复建议：添加 `[JsonIgnore]` 特性排除密码字段
```

### 重要问题详情

{important_issues_list}

### 建议改进

{suggestion_issues_list}

### 代码审查通过项

- ✅ 无 SQL 注入风险
- ✅ 异步编程规范正确
- ✅ 依赖注入配置正确
- ...

---

## 2. 运行时验证结果

### 资源状态

**总资源数**：{total_resources}  
**运行中**：{running_resources}  
**失败**：{failed_resources}  
**启动中**：{starting_resources}  
**已停止**：{stopped_resources}

#### 资源详情

| 资源名称 | 类型 | 状态 | 健康状态 | 端点 |
|---------|------|------|---------|------|
| {resource_name} | {resource_type} | {state} | {health} | {endpoint} |
| ... | ... | ... | ... | ... |

#### 失败的资源

{failed_resources_list}

**示例格式**：
```
1. **CodeSpirit.IdentityApi**
   - 状态：Failed
   - 错误：数据库连接失败
   - 日志：查看下方错误日志详情
```

### 错误日志分析

**Error 级别日志数**：{error_count}  
**Warning 级别日志数**：{warning_count}

#### 关键错误日志

{error_logs_summary}

**示例格式**：
```
[2026-01-26 10:30:15] [Error] CodeSpirit.IdentityApi
Database connection failed: Unable to connect to MySQL server at 'localhost:3306'
Stack trace: ...
```

#### 警告日志摘要

{warning_logs_summary}

### 控制台日志检查

#### 各资源控制台日志状态

| 资源名称 | 日志状态 | 关键信息 |
|---------|---------|---------|
| {resource_name} | ✅/⚠️/❌ | {key_info} |
| ... | ... | ... |

---

## 3. 功能验证结果

### 登录测试

| 测试项 | 状态 | 说明 |
|--------|------|------|
| 登录页面加载 | ✅/❌ | {login_page_status} |
| 表单字段识别 | ✅/❌ | {form_fields_status} |
| 表单填充 | ✅/❌ | {form_fill_status} |
| 登录按钮点击 | ✅/❌ | {login_button_status} |
| 登录成功 | ✅/❌ | {login_success_status} |
| 页面跳转 | ✅/❌ | {redirect_status} |
| 管理后台访问 | ✅/❌ | {admin_access_status} |

### 测试详情

**测试租户 ID**：{test_tenant_id}  
**测试用户名**：{test_username}  
**登录 URL**：{login_url}  
**管理后台 URL**：{admin_url}

#### 登录页面测试

- **页面加载时间**：{page_load_time}ms
- **页面元素检查**：
  - 用户名输入框：✅/❌
  - 密码输入框：✅/❌
  - 登录按钮：✅/❌
  - 其他元素：{other_elements}

#### 登录流程测试

1. **导航到登录页**
   - 状态：✅/❌
   - URL：{actual_login_url}
   - 说明：{navigation_note}

2. **填充登录表单**
   - 状态：✅/❌
   - 用户名填充：✅/❌
   - 密码填充：✅/❌
   - 说明：{form_fill_note}

3. **点击登录按钮**
   - 状态：✅/❌
   - 说明：{button_click_note}

4. **验证登录成功**
   - 状态：✅/❌
   - 跳转 URL：{redirect_url}
   - 预期 URL：{expected_url}
   - 说明：{login_verification_note}

5. **检查管理后台**
   - 状态：✅/❌
   - 页面元素：{admin_page_elements}
   - 说明：{admin_check_note}

### 页面截图

{如果失败，包含截图或页面快照信息}

---

## 4. 修复建议

### 必须修复（严重问题）

{critical_fixes_list}

**示例格式**：
```
1. **安全问题**：修复密码字段暴露
   - 文件：`Src/ApiServices/CodeSpirit.IdentityApi/Dtos/CreateUserDto.cs`
   - 操作：添加 `[JsonIgnore]` 特性
   - 优先级：🔴 高

2. **数据库连接失败**：检查数据库配置
   - 资源：CodeSpirit.IdentityApi
   - 操作：检查连接字符串和数据库服务状态
   - 优先级：🔴 高
```

### 建议修复（重要问题）

{important_fixes_list}

**示例格式**：
```
1. **多语言支持**：添加 Display 特性
   - 文件：`Src/ApiServices/CodeSpirit.ExamApi/Dtos/CreateQuestionDto.cs`
   - 操作：为属性添加 `[Display]` 特性并配置资源文件
   - 优先级：🟡 中
```

### 可选优化（建议改进）

{suggestion_fixes_list}

---

## 5. 下一步行动

### 立即行动

- [ ] 修复所有严重问题
- [ ] 重新运行验证流程
- [ ] 确认所有检查通过

### 后续优化

- [ ] 修复重要问题
- [ ] 实施建议改进
- [ ] 更新文档和注释

### 提交准备

- [ ] 所有严重问题已修复
- [ ] 代码审查通过
- [ ] 运行时验证通过
- [ ] 功能验证通过
- [ ] 准备提交代码

---

## 6. 详细日志（可选）

### 完整错误日志

<details>
<summary>点击展开完整错误日志</summary>

```
{full_error_logs}
```

</details>

### 完整警告日志

<details>
<summary>点击展开完整警告日志</summary>

```
{full_warning_logs}
```

</details>

### 代码审查详细报告

<details>
<summary>点击展开详细代码审查报告</summary>

```
{detailed_code_review_report}
```

</details>

---

## 7. 验证环境信息

**操作系统**：{os_version}  
**.NET 版本**：{dotnet_version}  
**Aspire 版本**：{aspire_version}  
**验证工具版本**：{validation_tool_version}

**MCP 工具状态**：
- Aspire MCP：✅/❌
- Playwright MCP：✅/❌

---

## 报告生成说明

本报告由提交前验证技能自动生成。

**报告版本**：1.0  
**生成时间**：{report_generation_time}

---

## 附录

### A. 相关资源

- [代码审查技能文档](../code-review/SKILL.md)
- [测试配置文档](test-config.md)
- [验证检查清单](validation-checklist.md)

### B. 联系方式

如遇到问题或需要帮助，请联系：
- 项目维护者：{maintainer}
- 技术支持：{support_contact}

---

**报告结束**
