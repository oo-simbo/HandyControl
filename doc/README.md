# HandyControl 个人版维护文档

本目录保留上游 Hexo 使用文档，并增加面向个人 WPF 分支的维护说明。当前维护政策：**最低 .NET 10，仅维护 WPF；不保留旧 .NET 兼容代码，Avalonia 后续单独处理。**

## 阅读入口

- [维护范围与基线](maintenance/scope.md)：仓库关系、支持范围、兼容契约。
- [源码与主题定位](maintenance/architecture.md)：控件实现、共享编译、主题生成、Demo 入口。
- [构建与二进制接入](maintenance/build-and-use.md)：构建、验证、DLL 分发和回滚。
- [PR 与 Bug 维护流程](maintenance/workflow.md)：补丁来源、复现、回归、上游同步。
- [修改与验证台账](maintenance/changes.md)：实际修改、验证结果和待办。
- [上游快速开始原文](source/handycontrol/quick_start/index.md)。

`source/`、`themes/`、`scaffolds/` 及 Hexo 配置属于原有站点；其中历史兼容性说明不代表个人版支持范围。`maintenance/` 是仓库内 Markdown 文档，不自动发布到官方站点。
