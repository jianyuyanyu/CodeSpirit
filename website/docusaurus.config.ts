import {themes as prismThemes} from 'prism-react-renderer';
import type {Config} from '@docusaurus/types';
import type * as Preset from '@docusaurus/preset-classic';

// This runs in Node.js - Don't use client-side code here (browser APIs, JSX...)

const config: Config = {
  title: 'CodeSpirit（码灵）',
  tagline: '革命性的全栈低代码+AI开发框架',
  favicon: 'img/favicon.ico',

  // Set the production url of your site here
  url: 'https://xin-lai.github.io',
  // Set the /<baseUrl>/ pathname under which your site is served
  // For GitHub pages deployment, it is often '/<projectName>/'
  baseUrl: '/CodeSpirit/',

  // GitHub pages deployment config.
  // If you aren't using GitHub pages, you don't need these.
  organizationName: 'xin-lai', // Usually your GitHub org/user name.
  projectName: 'CodeSpirit', // Usually your repo name.

  onBrokenLinks: 'warn',
  onBrokenMarkdownLinks: 'warn',

  // Even if you don't use internationalization, you can use this field to set
  // useful metadata like html lang. For example, if your site is Chinese, you
  // may want to replace "en" with "zh-Hans".
  i18n: {
    defaultLocale: 'zh-CN',
    locales: ['zh-CN'],
  },

  presets: [
    [
      'classic',
      {
        docs: {
          sidebarPath: './sidebars.ts',
          // Please change this to your repo.
          // Remove this to remove the "edit this page" links.
          editUrl: 'https://github.com/xin-lai/CodeSpirit/tree/main/website/',
          showLastUpdateTime: true,
          showLastUpdateAuthor: true,
        },
        blog: false, // 禁用博客功能
        theme: {
          customCss: './src/css/custom.css',
        },
      } satisfies Preset.Options,
    ],
  ],

  themeConfig: {
    // Replace with your project's social card
    image: 'img/docusaurus-social-card.jpg',
    navbar: {
      title: 'CodeSpirit',
      logo: {
        alt: 'CodeSpirit Logo',
        src: 'img/logo.svg',
      },
      items: [
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebar',
          position: 'left',
          label: '文档',
        },
        {
          type: 'docSidebar',
          sidebarId: 'tutorialSidebarEn',
          position: 'left',
          label: 'Docs (EN)',
        },
        {
          href: 'https://github.com/xin-lai/CodeSpirit',
          label: 'GitHub',
          position: 'right',
        },
        {
          href: 'https://gitee.com/magicodes/code-spirit',
          label: 'Gitee',
          position: 'right',
        },
      ],
    },
    footer: {
      style: 'dark',
      links: [
        {
          title: '文档',
          items: [
            {
              label: '快速开始',
              to: '/docs/Core-Docs/development-environment-setup-zh-CN',
            },
            {
              label: 'API 参考',
              to: '/docs/Core-Docs/codespirit-core-framework-zh-CN',
            },
          ],
        },
        {
          title: '社区',
          items: [
            {
              label: 'GitHub',
              href: 'https://github.com/xin-lai/CodeSpirit',
            },
            {
              label: 'Gitee',
              href: 'https://gitee.com/magicodes/code-spirit',
            },
          ],
        },
        {
          title: '更多',
          items: [
            {
              label: '在线体验',
              href: 'https://codespirit-app.xin-lai.com/',
            },
          ],
        },
      ],
      copyright: `Copyright © ${new Date().getFullYear()} CodeSpirit. Built with Docusaurus.`,
    },
    prism: {
      theme: prismThemes.github,
      darkTheme: prismThemes.dracula,
      additionalLanguages: ['csharp', 'powershell', 'bash', 'json', 'yaml'],
    },
    algolia: {
      // 如果您有 Algolia DocSearch，可以在这里配置
      // 申请地址：https://docsearch.algolia.com/apply/
      appId: 'YOUR_APP_ID',
      apiKey: 'YOUR_SEARCH_API_KEY',
      indexName: 'codespirit',
      contextualSearch: true,
    },
  } satisfies Preset.ThemeConfig,
};

export default config;
