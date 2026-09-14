# 构建与二进制接入

## 日常 WPF 构建

在 Windows 上安装 .NET 10 SDK；使用 Visual Studio 时需支持 .NET 10 并安装 .NET 桌面开发组件。以下 PowerShell 命令在仓库根目录执行：

```powershell
dotnet build src/Net_GE45/HandyControl_Net_GE45/HandyControl_Net_GE45.csproj -c Release
dotnet build src/Net_GE45/HandyControlDemo_Net_GE45/HandyControlDemo_Net_GE45.csproj -c Release
```

Demo 项目引用库及 DemoCode，会一起构建。当前仅维护 WPF，优先使用项目入口，避免整套 solution restore 把 Avalonia 的依赖也拉入验证。Visual Studio 中选择 `Release-Net-GE45` / `Debug-Net-GE45`；这些只是保留的配置名称，不表示旧框架支持。

需要完整重编译时添加 `--no-incremental`。第一次构建需要 NuGet 源可用；不要在尚未还原时使用 `--no-restore`。现有程序集版本定义在 `src/Directory.Build.Props`，本次未改变版本与强名称密钥。

产物目录：

- 库：`src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/`
- Demo：`src/Net_GE45/HandyControlDemo_Net_GE45/bin/Release/net10.0-windows/`

核心文件是 `HandyControl.dll`；同时保留 XML 智能提示文件、需要的 PDB，以及各语言子目录的 `HandyControl.resources.dll`。不要只替换主 DLL 却混用不同批次的语言卫星程序集。保留完整构建输出作为归档最方便。

## 下游直接引用 DLL

下游 WPF 项目最低为 `net10.0-windows`，启用 `UseWPF`。推荐把同一批构建产物复制到下游固定的 `lib/HandyControl/` 目录，避免 HintPath 依赖维护者机器上的绝对路径。示例：

```xml
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <UseWPF>true</UseWPF>
</PropertyGroup>
<ItemGroup>
  <Reference Include="HandyControl">
    <HintPath>lib/HandyControl/HandyControl.dll</HintPath>
    <Private>true</Private>
  </Reference>
</ItemGroup>
```

移除下游已有的同名官方 NuGet 引用，保证一次运行只解析到一份 HandyControl。构建及发布后检查实际输出目录中的 DLL 与语言子目录；按需明确配置卫星资源复制，不能仅凭源目录中存在文件认定发布包完整。

应用资源仍使用原有 URI，先皮肤后主题：

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/SkinDefault.xaml" />
      <ResourceDictionary Source="pack://application:,,,/HandyControl;component/Themes/Theme.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

控件命名空间：`xmlns:hc="https://handyorg.github.io/handycontrol"`。参考[官方快速开始](https://handyorg.github.io/handycontrol/quick_start/)，但框架支持政策以个人版[维护范围](scope.md)为准。

## 自动运行验证

[WpfSmoke](verification/WpfSmoke/Program.cs) 是无需额外测试 NuGet 包的 STA 控制台冒烟程序，直接引用库的 **Release DLL**，模拟下游二进制接入，而不是 ProjectReference。

```powershell
dotnet build src/Net_GE45/HandyControlDemo_Net_GE45/HandyControlDemo_Net_GE45.csproj -c Release --no-incremental
if ($LASTEXITCODE -ne 0) { throw 'WPF build failed' }
dotnet run --project doc/maintenance/verification/WpfSmoke/WpfSmoke.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw 'WPF smoke failed' }
```

程序加载默认、深色、紫色皮肤，逐项断言 NumericUpDown 三种模板的画笔绑定/更新、模板重新应用、文本输入及上下界，并创建不抢焦点的屏幕外窗口验证 WindowChrome、窗口模板、Growl 创建与清除。成功返回 0，失败输出异常并返回 1。需要可创建 WPF 窗口的 Windows 用户会话。

该验证不替代真实界面检查。涉及相应区域的补丁还需检查：100%/150%/200% DPI、多屏最大化和任务栏、键盘/鼠标输入、中文输入法、弹出层、通知动画/关闭计时、语言切换。仅编译 Demo 不代表这些交互已验证。

## DLL 归档与回退

每批分发记录 Git SHA（未提交则明确标为工作区构建）、SDK/Runtime、构建配置、程序集版本、DLL SHA256、补丁编号及验证结果。可用 `Get-FileHash -Algorithm SHA256` 获取摘要。相同程序集版本不代表 DLL 相同，本次仍为 `3.6.0.0`，须通过 SHA 与哈希区分。

替换下游文件前归档当前整套库文件；回退时恢复同一批 DLL、XML、卫星资源并重建下游。不得声称已验证未提供的下游项目。

## 旧发布流水线的边界

`build/build.config.xml` 已收敛到 .NET 10 Windows。`build/build.cake` 中发布资产改为从配置枚举目录，并删除 Framework/Core 分支。Squirrel 的 `lib/net45` 是该旧打包工具的布局约定，保留不代表库支持 .NET Framework。

当前交付使用 `dotnet build`。Cake/Squirrel 安装包及公开 NuGet/GitHub 发布未验证；脚本仍含上游仓库发布目标和元数据，`publish` 会提交、打标签、推送包、创建 Release，Setup 还会清理输出并下载工具。后续确需个人版发布时须先单独配置和验证，不能把它当作普通 DLL 编译入口。
