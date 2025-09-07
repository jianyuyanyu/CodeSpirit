# 问卷系统资源目录

本目录包含问卷系统相关的前端资源文件，按功能模块进行组织。

## 目录结构

```
survey/
├── css/                          # 样式文件
│   ├── survey-list.css          # 问卷列表页样式
│   ├── survey-participate.css   # 问卷参与页样式
│   └── survey-success.css       # 问卷提交成功页样式
├── js/                          # 脚本文件
│   ├── survey-list.js          # 问卷列表页脚本
│   ├── survey-participate.js   # 问卷参与页脚本
│   └── survey-success.js       # 问卷提交成功页脚本
└── README.md                   # 本说明文件
```

## 文件说明

### CSS 样式文件

- **survey-list.css**: 问卷列表页面的样式，包含卡片布局、响应式设计等
- **survey-participate.css**: 问卷参与页面的样式，包含表单样式、进度条等
- **survey-success.css**: 问卷提交成功页面的样式，包含动画效果、按钮样式等

### JavaScript 脚本文件

- **survey-list.js**: 问卷列表页面的交互逻辑，包含数据加载、筛选等功能
- **survey-participate.js**: 问卷参与页面的交互逻辑，包含表单验证、提交等功能
- **survey-success.js**: 问卷提交成功页面的交互逻辑，包含动画效果、音效等

## 使用方式

在 Razor 页面中使用 `<resource>` 标签引用资源：

```html
@section Styles {
    <resource path="survey/css/survey-list.css" type="css" />
}

@section Scripts {
    <resource path="survey/js/survey-list.js" type="js" />
}
```

## 注意事项

1. 所有资源引用必须使用 `<resource>` 标签，不要直接使用 `<link>` 或 `<script>` 标签
2. 资源路径相对于 `wwwroot` 目录
3. 遵循项目的资源管理规范，优先将样式和脚本写入独立文件而非内联
4. 不要引入 Amis SDK 相关资源，模板页中已经引入
