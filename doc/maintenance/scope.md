# 维护范围与基线

## 项目定位

个人库用于吸收官方尚未合并的 PR，以及修复官方尚未修复的 Bug。其他项目直接引用个人库编译后的二进制文件。

| 角色 | 地址 | 本地位置 |
| --- | --- | --- |
| 官方仓库 | https://github.com/handyorg/HandyControl | `D:\Develop\HandyControl-master` |
| 个人仓库 | https://github.com/oo-simbo/HandyControl | `D:\Develop\HandyControl` |
| 官方使用文档 | https://handyorg.github.io/handycontrol/quick_start/ | 本库 `doc/source/handycontrol/` 保留历史副本 |

## 已确定的维护政策（2026-09-14）

1. 最低支持 **.NET 10**，当前 WPF 目标为 `net10.0-windows`。不测试、不修复、不新增 .NET Framework、.NET Core 3.x、.NET 5–9 兼容代码。
2. 当前仅维护 WPF 控件库及必要的 Demo、构建和验证入口。Avalonia 源码、依赖和主题不在本次修改范围，后续单独评估。
3. 删除旧框架条件分支、旧项目与专用兼容实现；保留 Windows 系统版本、DPI、窗口句柄等与运行环境有关的判断，它们不是旧 .NET 兼容分支。
4. 保留 `HandyControl` 程序集名称、公开命名空间、XAML XMLNS 和资源 URI；修改公开 API、依赖属性默认值或模板部件时明确记录影响。
5. 现有 `Net_GE45` 目录名暂时保留，便于与上游定位相同文件及减少引用路径迁移。目录名不表示仍支持 .NET 4.5。
6. 不因本次清理迁移 MVVM 框架、改写所有原生调用或开展无依据的性能重构。共享的 Expression/Interactivity 代码仍被 WPF 使用，不能按名称直接删除。

## 核实基线

文档建立前个人仓库 HEAD：`c67b8eba8d697652236ebf2787be1f2c5b48c9e7`，分支 `main`。其父提交 `fa650c5` 是初始导入；HEAD 仅新增 `CLAUDE.md`。初始工作区干净。

本机：Windows x64、.NET SDK 10.0.302、MSBuild 18.6.11。初始 WPF 库与 Demo Release 编译通过（各 0 警告、0 错误，使用已有 restore 资产）。后续验证见[台账](changes.md)。

## 维护优先级

先保证 .NET 10 WPF 构建和主题资源链稳定，再依据可复现的 Bug/PR 逐项维护。没有复现与测量证据时，不预设性能瓶颈。下游项目名称、二进制归档位置、具体候选 PR 编号尚未提供，不虚构接入或合并记录。
