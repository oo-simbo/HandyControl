# 构建与二进制接入

## 日常 WPF 构建

在 Windows 上安装 .NET 10 SDK；使用 Visual Studio 时需支持 .NET 10 并安装 .NET 桌面开发组件。以下 PowerShell 命令在仓库根目录执行：

```powershell
dotnet build src/Net_GE45/HandyControl_Net_GE45/HandyControl_Net_GE45.csproj -c Release
dotnet build src/Net_GE45/HandyControlDemo_Net_GE45/HandyControlDemo_Net_GE45.csproj -c Release
```

Demo 项目引用库及 DemoCode，会一起构建。当前仅维护 WPF，优先使用项目入口，避免整套 solution restore 把 Avalonia 的依赖也拉入验证。Visual Studio 中选择 `Release-Net-GE45` / `Debug-Net-GE45`；这些只是保留的配置名称，不表示旧框架支持。

需要完整重编译时添加 `--no-incremental`。第一次构建需要 NuGet 源可用；不要在尚未还原时使用 `--no-restore`。现行维护版本为 `3.6.3.0`，由 `src/Directory.Build.Props` 统一设置 `Version`、`FileVersion` 和 `AssemblyVersion`。

产物目录：

- 库：`src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/`
- Demo：`src/Net_GE45/HandyControlDemo_Net_GE45/bin/Release/net10.0-windows/`

核心文件是 `HandyControl.dll`；简体中文内置其中，不产生独立中文语言包。英文包为 `en/HandyControl.resources.dll`。XML 是可选的 IDE 智能提示文档，不是运行依赖；按当前部署要求不向 JuLink.Ultron 分发 PDB。主 DLL 与英文包应使用同批构建产物。语言支持及接入细节见下文。

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

## 中英文语言包与切换

库只保留 `Lang.resx`（简体中文默认资源）和 `Lang.en.resx`（英文）。产物为：

- 中文及控件本体：`src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/HandyControl.dll`。
- 英文：`src/Net_GE45/HandyControl_Net_GE45/bin/Release/net10.0-windows/en/HandyControl.resources.dll`。
- Demo 自身英文文案：`src/Net_GE45/HandyControlDemo_Net_GE45/bin/Release/net10.0-windows/en/HandyControlDemo.resources.dll`，业务项目不需要此文件。

将英文卫星程序集放在应用输出目录的 `en/` 子目录，不能与主 DLL 平铺。卫星程序集通过 ResourceManager 按文化自动加载，无须给它添加普通程序集 Reference。若放到项目的 `lib/HandyControl/en/`，可显式配置复制到构建和发布目录：

```xml
<ItemGroup>
  <None Include="lib/HandyControl/en/HandyControl.resources.dll"
        CopyToOutputDirectory="PreserveNewest"
        CopyToPublishDirectory="PreserveNewest"
        TargetPath="en/HandyControl.resources.dll" />
</ItemGroup>
```

以上使用 `Include` 显式加入英文 DLL；如果项目已将该文件列为 `None` 项，则改用 `Update`，避免重复加入。仅使用 `Update` 不会创建原本不存在的文件项。主程序集按前文添加 Reference。

先加载 HandyControl 皮肤和主题字典，然后在 UI 线程初始化语言（建议创建首个窗口之前）：

```csharp
HandyControl.Tools.ConfigHelper.Instance.SetLang("zh-cn"); // 简体中文
HandyControl.Tools.ConfigHelper.Instance.SetLang("en");    // 英文，二选一
```

`en-US` 等英文区域会回退使用 `en` 包；没有匹配翻译时回退到内置中文。Demo 仅显示中英文按钮，旧配置中保存的其他语言在启动时归一为 `zh-cn`，英文区域归一为 `en`。

需要运行时更新的业务控件可绑定库的语言提供器：

```csharp
HandyControl.Properties.Langs.LangProvider.SetLang(
    confirmButton, System.Windows.Controls.ContentControl.ContentProperty, "Confirm");
```

再调用 `ConfigHelper.Instance.SetLang` 时此绑定会更新。`x:Static Lang.Confirm` 或一次性赋值的文案不会自动重新求值，需重新创建相关视图或改用动态语言绑定。该接口管理控件库语言，不翻译业务项目自身的文案，也不替代业务日期/数字格式策略。

升级已有部署时移除旧 HandyControl/HandyControlDemo 其他语言卫星 DLL；同目录可能含其他依赖的资源，不能直接删除其他依赖的语言文件。此次 Release 编译前执行了 clean，已确认 WPF 库输出只有英文卫星程序集。

## PropertyGrid 排序与枚举描述

`CategoryOrder` 接受分类名称序列，`PropertyOrder` 接受 CLR 属性名称序列（不是 DisplayName）。分类模式依次按分类优先级、分类名称、属性优先级、显示名称排序；未列出的项仍显示在对应优先项之后。比较忽略大小写，空白和重复配置不增加排序项，未知名称不影响实际项目之间的顺序。

```csharp
propertyGrid.CategoryOrder = new[] { "通讯参数", "运行参数" };
propertyGrid.PropertyOrder = new[] { "PortName", "BaudRate", "Timeout" };
propertyGrid.SelectedObject = settings;
```

运行时请赋予新的序列以刷新排序；不监听已有集合内部的 Add/Remove。更新优先级不会改变用户当前选择的“按名称”模式，也不会重建编辑器。按名称模式仍按 CLR 属性名称排序。

枚举成员带 `DescriptionAttribute` 时，下拉框显示描述；没有描述时显示字段名称，绑定写回值仍为枚举。只读属性对应下拉框禁用。Description 是静态元数据，不随中英文切换自动翻译。

```csharp
public enum ConnectionMode
{
    [System.ComponentModel.Description("自动连接")]
    Automatic,
    Manual
}
```

## 日期时间控件选择 Clock / ListClock

`DateTimePicker` 和 `CalendarWithClock` 均提供 `ClockType`，默认 `Clock`，可在 XAML 或运行时改为 `ListClock`：

```xml
<hc:DateTimePicker ClockType="ListClock" />
<hc:CalendarWithClock ClockType="ListClock" ShowConfirmButton="True" />
```

切换会保留 `DisplayDateTime` 中尚未确认的编辑内容；确认后才更新 `SelectedDateTime`。DateTimePicker 自动将 ClockType 传给弹窗内的 CalendarWithClock。内置模板复用现有 ListClock 样式；业务若完全替换 CalendarWithClock 模板，需要自行适配两种时钟的布局。

Demo 的 PropertyGrid 页面有排序开关及枚举描述示例；CalendarWithClock 和 DateTimePicker 页面有运行时 ClockType 开关及已确认值展示，DateTimePicker 还展示 ListClock 的 Plus/Small 样式。

## 自动运行验证

[WpfSmoke](verification/WpfSmoke/Program.cs) 是无需额外测试 NuGet 包的 STA 控制台冒烟程序，直接引用库的 **Release DLL**，模拟下游二进制接入，而不是 ProjectReference。

```powershell
dotnet build src/Net_GE45/HandyControlDemo_Net_GE45/HandyControlDemo_Net_GE45.csproj -c Release --no-incremental
if ($LASTEXITCODE -ne 0) { throw 'WPF build failed' }
dotnet run --project doc/maintenance/verification/WpfSmoke/WpfSmoke.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw 'WPF smoke failed' }
```

程序加载默认、深色、紫色皮肤，逐项断言 NumericUpDown 三种模板的画笔绑定/更新、模板重新应用、文本输入及上下界，并创建不抢焦点的屏幕外窗口验证 WindowChrome、窗口模板、Growl 创建与清除。成功返回 0，失败输出异常并返回 1。需要可创建 WPF 窗口的 Windows 用户会话。

程序还验证 PropertyGrid 排序边界、枚举描述/回写/只读、排序时编辑器实例保持，以及 CalendarWithClock/DateTimePicker 时钟切换、待确认时间、旧事件解绑、列表编辑、确认关闭、模板更换和最终 DLL 的 ListClock 标题布局。

可传入已构建的 Demo DLL 路径，额外加载三个相关 Demo 页面并验证时钟开关绑定：

```powershell
dotnet run --project doc/maintenance/verification/WpfSmoke/WpfSmoke.csproj -c Release -- src/Net_GE45/HandyControlDemo_Net_GE45/bin/Release/net10.0-windows/HandyControlDemo.dll
```

该验证不替代真实界面检查。涉及相应区域的补丁还需检查：100%/150%/200% DPI、多屏最大化和任务栏、键盘/鼠标输入、中文输入法、弹出层、通知动画/关闭计时、语言切换。仅编译 Demo 不代表这些交互已验证。

## DLL 归档与回退

每批分发记录 Git SHA（未提交则明确标为工作区构建）、SDK/Runtime、构建配置、程序集版本、DLL SHA256、补丁编号及验证结果。可用 `Get-FileHash -Algorithm SHA256` 获取摘要。相同程序集版本不代表 DLL 相同，本次分发版本为 `3.6.2.0`，须通过 SHA 与哈希区分。

替换下游文件前归档当前整套库文件；回退时恢复同一批 DLL、XML、卫星资源并重建下游。不得声称已验证未提供的下游项目。

## 旧发布流水线的边界

`build/build.config.xml` 已收敛到 .NET 10 Windows。`build/build.cake` 中发布资产改为从配置枚举目录，并删除 Framework/Core 分支。Squirrel 的 `lib/net45` 是该旧打包工具的布局约定，保留不代表库支持 .NET Framework。

当前交付使用 `dotnet build`。Cake/Squirrel 安装包及公开 NuGet/GitHub 发布未验证；脚本仍含上游仓库发布目标和元数据，`publish` 会提交、打标签、推送包、创建 Release，Setup 还会清理输出并下载工具。后续确需个人版发布时须先单独配置和验证，不能把它当作普通 DLL 编译入口。
