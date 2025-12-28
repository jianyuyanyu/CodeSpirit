import type {SidebarsConfig} from '@docusaurus/plugin-content-docs';

const sidebars: SidebarsConfig = {
  tutorialSidebarEn: [
    {
      type: 'category',
      label: 'Core Documentation',
      items: [
        {
          type: 'doc',
          id: 'Core-Docs/project-architecture-en-US',
          label: 'Project Architecture',
        },
        {
          type: 'doc',
          id: 'Core-Docs/technical-system-overview-en-US',
          label: 'Technical Overview',
        },
        {
          type: 'doc',
          id: 'Core-Docs/development-environment-setup-en-US',
          label: 'Environment Setup',
        },
        {
          type: 'doc',
          id: 'Core-Docs/codespirit-core-framework-en-US',
          label: 'CodeSpirit.Core Framework',
        },
        {
          type: 'doc',
          id: 'Core-Docs/unified-exception-handling-en-US',
          label: 'Exception Handling',
        },
        {
          type: 'doc',
          id: 'Core-Docs/crud-development-example-en-US',
          label: 'CRUD Example',
        },
        {
          type: 'doc',
          id: 'Core-Docs/i18n-localization-guide-en-US',
          label: 'Internationalization',
        },
        {
          type: 'doc',
          id: 'Core-Docs/aliyun-qwen-free-trial-guide-en-US',
          label: 'Aliyun Qwen Guide',
        },
      ],
    },
  ],
  tutorialSidebar: [
    {
      type: 'category',
      label: '开始',
      items: [
        {
          type: 'doc',
          id: 'codespirit-ai-features-zh-CN',
          label: 'AI 特色功能',
        },
        {
          type: 'doc',
          id: 'codespirit-framework-highlights-zh-CN',
          label: '框架核心亮点',
        },
      ],
    },
    {
      type: 'category',
      label: '核心文档',
      link: {
        type: 'generated-index',
        title: '核心文档',
        description: '项目架构、开发环境、核心框架等基础文档',
      },
      items: [
        {
          type: 'doc',
          id: 'Core-Docs/project-architecture-zh-CN',
          label: '项目整体架构设计',
        },
        {
          type: 'doc',
          id: 'Core-Docs/technical-system-overview-zh-CN',
          label: '总体技术体系说明',
        },
        {
          type: 'doc',
          id: 'Core-Docs/development-environment-setup-zh-CN',
          label: '开发环境搭建指南',
        },
        {
          type: 'doc',
          id: 'Core-Docs/codespirit-core-framework-zh-CN',
          label: 'CodeSpirit.Core 核心框架',
        },
        {
          type: 'doc',
          id: 'Core-Docs/unified-exception-handling-zh-CN',
          label: '统一异常处理指南',
        },
        {
          type: 'doc',
          id: 'Core-Docs/crud-development-example-zh-CN',
          label: 'CRUD 开发示例',
        },
        {
          type: 'doc',
          id: 'Core-Docs/i18n-localization-guide-zh-CN',
          label: '多语言国际化使用指南',
        },
        {
          type: 'doc',
          id: 'Core-Docs/aliyun-qwen-free-trial-guide-zh-CN',
          label: '阿里云通义千问免费体验指南',
        },
      ],
    },
    {
      type: 'category',
      label: '界面生成引擎',
      link: {
        type: 'generated-index',
        title: '界面生成引擎',
        description: 'AMIS 引擎、UDL Cards、智能图表、表单组件等',
      },
      items: [
        {
          type: 'doc',
          id: 'UI-Generation/codespirit-amis-engine-zh-CN',
          label: 'AMIS 界面生成引擎',
        },
        {
          type: 'doc',
          id: 'UI-Generation/amis-column-inference-zh-CN',
          label: 'AMIS 列自动推断功能',
        },
        {
          type: 'doc',
          id: 'UI-Generation/codespirit-charts-guide-zh-CN',
          label: '智能图表组件',
        },
        {
          type: 'doc',
          id: 'UI-Generation/codespirit-udl-cards-guide-zh-CN',
          label: 'UDL Cards 卡片使用指南',
        },
        {
          type: 'doc',
          id: 'UI-Generation/codespirit-udlcards-sdk-guide-zh-CN',
          label: 'UDL Cards SDK 使用指南',
        },
        {
          type: 'doc',
          id: 'UI-Generation/udl-ui-description-language-design-zh-CN',
          label: 'UDL UI 描述语言设计方案',
        },
      ],
    },
    {
      type: 'category',
      label: '核心组件',
      link: {
        type: 'generated-index',
        title: '核心组件',
        description: 'AI 表单填充、导航、审计、缓存、定时任务等',
      },
      items: [
        {
          type: 'doc',
          id: 'Core-Components/codespirit-ai-form-fill-guide-zh-CN',
          label: 'AI 表单智能填充组件',
        },
        {
          type: 'doc',
          id: 'Core-Components/codespirit-navigation-guide-zh-CN',
          label: 'Navigation 导航组件',
        },
        {
          type: 'doc',
          id: 'Core-Components/codespirit-unified-startup-guide-zh-CN',
          label: '统一启动框架使用指南',
        },
        {
          type: 'doc',
          id: 'Core-Components/codespirit-audit-integration-guide-zh-CN',
          label: '审计组件集成使用指南',
        },
        {
          type: 'doc',
          id: 'Core-Components/codespirit-llm-guide-zh-CN',
          label: 'LLM 大语言模型组件',
        },
      ],
    },
    {
      type: 'category',
      label: '身份认证与权限',
      items: [
        {
          type: 'doc',
          id: 'Identity-Auth/codespirit-identity-api-zh-CN',
          label: '身份认证服务',
        },
        {
          type: 'doc',
          id: 'Identity-Auth/codespirit-authorization-guide-zh-CN',
          label: '权限组件详解',
        },
      ],
    },
    {
      type: 'category',
      label: '多租户架构',
      items: [
        {
          type: 'doc',
          id: 'Multi-Tenancy/codespirit-tenant-resolver-guide-zh-CN',
          label: '租户解析器使用指南',
        },
        {
          type: 'doc',
          id: 'Multi-Tenancy/codespirit-data-filter-guide-zh-CN',
          label: '数据筛选器使用指南',
        },
      ],
    },
    {
      type: 'category',
      label: '基础设施与运维',
      items: [
        {
          type: 'doc',
          id: 'Infrastructure/rabbitmq-aspire-integration-zh-CN',
          label: 'RabbitMQ 集成指南',
        },
        {
          type: 'doc',
          id: 'Infrastructure/elasticsearch-aspire-migration-summary-zh-CN',
          label: 'Elasticsearch 迁移总结',
        },
        {
          type: 'doc',
          id: 'Infrastructure/codespirit-caching-guide-zh-CN',
          label: '统一缓存组件指南',
        },
      ],
    },
    {
      type: 'category',
      label: '考试系统',
      items: [
        {
          type: 'doc',
          id: 'Exam-System/exam-system-complete-documentation-zh-CN',
          label: '考试系统完整说明文档',
        },
        {
          type: 'doc',
          id: 'Exam-System/exam-system-feature-list-zh-CN',
          label: '考试系统业务功能清单',
        },
      ],
    },
  ],
};

export default sidebars;
