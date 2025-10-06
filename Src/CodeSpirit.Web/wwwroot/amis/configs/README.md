# AMIS 配置文件目录

本目录包含用于 AMIS 界面生成的 JavaScript 配置文件。

## 文件列表

### enhanced-import.js
增强的批量导入组件配置文件，用于生成批量导入界面。

### question-import-wizard.js
题目导入向导配置文件，包含以下功能：
- 题目导入向导界面配置（3步向导流程）
- 基于input-table组件的题目列表编辑功能
- 支持弹窗编辑、预览、删除等操作
- 详细的错误分类和反馈系统
- 智能的失败原因分析和解决建议

### question-preview.js
题目预览专用配置文件，包含以下功能：
- 美观的题目展示布局
- A、B、C、D格式的选项显示
- 分区域的信息展示（答案、解析、标签）
- 响应式设计和现代化UI
- 支持不同题目类型的智能显示

## 使用方式

### 1. 在控制器中使用

```csharp
// 使用Service组件加载外部配置文件
var serviceWrapper = new JObject
{
    ["type"] = "service",
    ["name"] = "questionImportWizardService",
    ["schemaApi"] = "js:/amis/configs/question-import-wizard.js?type=wizard&baseApi=/exam/api/exam&rootApi=${ROOT_API}"
};
```

### 2. URL参数说明

- `type`: 配置类型
  - `wizard`: 题目导入向导配置
  - `preview`: 题目预览配置
- `baseApi`: 基础API路径，默认为 `/exam/api/exam`
- `rootApi`: 根API路径，默认为 `${ROOT_API}`

### 3. 测试页面

可以通过访问 `/test-question-import-wizard.html` 来测试配置文件是否正常工作。

## 开发规范

1. 所有配置文件必须支持 JSONP 回调
2. 配置函数应该接受参数对象，提供默认值
3. 配置文件应该同时支持 Node.js 和浏览器环境
4. 使用清晰的注释说明每个配置项的作用

## 目录结构

```
amis/configs/
├── README.md                      # 说明文档
├── enhanced-import.js             # 增强批量导入配置
├── question-import-wizard.js      # 题目导入向导配置
├── question-preview.js            # 题目预览专用配置
└── test-question-import-wizard.html # 测试页面
```
