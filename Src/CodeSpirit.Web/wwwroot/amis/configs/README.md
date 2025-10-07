# AMIS 配置文件目录

本目录包含用于 AMIS 界面生成的 JavaScript 配置文件，按服务组件进行分类组织。

## 目录结构

### common/ - 通用组件配置
包含可跨服务复用的通用组件配置文件。

#### enhanced-import.js
增强的批量导入组件配置文件，包含以下功能：
- 支持模板下载的三步向导流程
- 文件上传和数据验证
- 批量导入结果展示
- 支持自定义字段配置
- 响应式设计和现代化UI

### ai/ - AI相关服务配置
包含AI智能功能相关的配置文件。

#### ai-form-wizard.js
AI表单向导配置文件，包含以下功能：
- AI智能表单生成的三步向导流程
- 表单填写、AI处理进度、结果展示
- 实时日志显示和进度跟踪
- 支持自定义表单字段和API配置
- 响应式设计和现代化UI

### exam/ - 考试系统配置
包含考试系统相关的配置文件。

#### question-import-wizard.js
题目导入向导配置文件，包含以下功能：
- 题目导入向导界面配置（3步向导流程）
- 基于input-table组件的题目列表编辑功能
- 支持弹窗编辑、预览、删除等操作
- 详细的错误分类和反馈系统
- 智能的失败原因分析和解决建议

#### question-preview.js
题目预览专用配置文件，包含以下功能：
- 美观的题目展示布局
- A、B、C、D格式的选项显示
- 分区域的信息展示（答案、解析、标签）
- 响应式设计和现代化UI
- 支持不同题目类型的智能显示

### survey/ - 问卷调查系统配置
包含问卷调查系统相关的配置文件（待扩展）。

## 使用方式

### 1. 在控制器中使用

```csharp
// 使用Service组件加载外部配置文件（注意新的路径结构）

// 加载AI表单向导配置
var aiFormWizard = new JObject
{
    ["type"] = "service",
    ["name"] = "aiFormWizardService",
    ["schemaApi"] = "js:/amis/configs/ai/ai-form-wizard.js"
};

// 加载题目导入向导配置
var questionImportWizard = new JObject
{
    ["type"] = "service",
    ["name"] = "questionImportWizardService",
    ["schemaApi"] = "js:/amis/configs/exam/question-import-wizard.js?type=wizard&baseApi=/exam/api/exam&rootApi=${ROOT_API}"
};

// 加载增强导入组件配置
var enhancedImport = new JObject
{
    ["type"] = "service",
    ["name"] = "enhancedImportService",
    ["schemaApi"] = "js:/amis/configs/common/enhanced-import.js"
};
```

### 2. URL参数说明

#### 考试系统配置参数
- `type`: 配置类型
  - `wizard`: 题目导入向导配置
  - `preview`: 题目预览配置
- `baseApi`: 基础API路径，默认为 `/exam/api/exam`
- `rootApi`: 根API路径，默认为 `${ROOT_API}`

#### AI配置参数
- 通过 `api.context.aiFormConfig` 传递配置参数
- 支持自定义表单字段、API路径等

#### 通用导入配置参数
- 通过 `api.context.enhancedImportConfig` 传递配置参数
- 支持自定义字段、模板下载等

## 开发规范

1. 所有配置文件必须支持 JSONP 回调
2. 配置函数应该接受参数对象，提供默认值
3. 配置文件应该同时支持 Node.js 和浏览器环境
4. 使用清晰的注释说明每个配置项的作用

## 完整目录结构

```
amis/configs/
├── README.md                           # 总体说明文档
├── common/                             # 通用组件配置
│   └── enhanced-import.js              # 增强批量导入组件
├── ai/                                 # AI相关服务配置
│   └── ai-form-wizard.js               # AI表单向导配置
├── exam/                               # 考试系统相关配置
│   ├── question-import-wizard.js       # 题目导入向导配置
│   └── question-preview.js             # 题目预览配置
└── survey/                             # 问卷调查系统相关配置
    └── (待扩展)                        # 未来的问卷相关配置文件
```

## 分类说明

### 按服务组件分类的优势
1. **清晰的职责划分**：每个目录对应一个具体的服务组件
2. **便于维护**：相关功能的配置文件集中管理
3. **易于扩展**：新增服务时可以直接创建对应目录
4. **降低耦合**：不同服务的配置文件相互独立
5. **提高复用性**：通用组件可以被多个服务复用
