# 源码与主题定位

路径相对仓库根目录。当前结构保留上游命名，WPF 实际目标为 .NET 10。

## 编译结构

```text
src/Net_GE45/HandyControl_Net_GE45/*.csproj
  ├─ Import Shared/HandyControl_Shared/*.projitems
  ├─ Import Shared/Microsoft.Expression.Drawing/*.projitems
  ├─ Import Shared/System.Windows.Interactivity/*.projitems
  └─ Import Shared/Microsoft.Expression.Interactions/*.projitems
                         ↓
                   HandyControl.dll

src/Net_GE45/HandyControlDemo_Net_GE45/*.csproj
  ├─ ProjectReference → HandyControl_Net_GE45
  ├─ ProjectReference → Shared/HandyControlDemo_Code
  └─ Import Shared/HandyControlDemo_Shared/*.projitems
```

共享项目将源码编进宿主程序集，不单独产出同名 DLL。`HandyControl_Shared.projitems` 显式列出 Compile/Page/None；新增文件必须核实编译项，不能只放进 Shared 文件夹。Demo_Code 将示例源码作为 Resource，服务于示例展示，不是自动化测试工程。

## 修改入口

| 需求 | 主要位置 |
| --- | --- |
| 控件行为、依赖属性、事件 | `src/Shared/HandyControl_Shared/Controls/`，按 Input/Window/Growl/PropertyGrid 等分类 |
| 附加属性 | `Controls/Attach/` |
| 控件模板 | `Themes/Styles/Base/` |
| 默认样式、命名样式 | `Themes/Styles/` |
| 颜色与皮肤 | `Themes/SkinDefault.xaml`、`SkinDark.xaml`、`SkinViolet.xaml`、`Themes/Basic/Colors/` |
| 几何、转换器、字体和尺寸资源 | `Themes/Basic/` |
| 资源查找、窗口、DPI、配置 | `Tools/Helper/` |
| 原生调用 | `Tools/Interop/`；先核实 SDK 契约再改签名 |
| 多语言 | `Properties/Langs/` 和 `Themes/Lang.xaml` |
| XAML 命名空间映射 | `Properties/AssemblyInfo.cs` |
| 构建属性与版本 | `src/Directory.Build.Props` 和 WPF `.csproj` |

上表中 Controls/Themes/Tools/Properties 均相对 `src/Shared/HandyControl_Shared/`。

## 主题生成链

1. 在 `Themes/Styles/`、`Themes/Styles/Base/` 或 `Themes/Basic/` 修改源字典。
2. `Themes/Theme_GE45.txt` 按顺序列出合并输入；新增字典时同时登记，注意 StaticResource 先定义后引用。
3. WPF 库 `.csproj` 的 PreBuild 调用仓库自带 `Themes/XamlCombine.exe`，传入 `Theme_GE45.txt` 与 `Theme.xaml`。
4. 共享 `Themes/Theme.xaml` 是生成文件，文件头明确说明手改会被覆盖；`.projitems` 将它作为 Page 编译。
5. 消费端先加载 SkinDefault，再加载 Theme；样式修复必须验证实际生成及打包结果。

不要把当前结构误当作只维护 Generic.xaml 的普通自定义控件项目。保持 `pack://application:,,,/HandyControl;component/Themes/Theme.xaml` 与 Skin 资源路径稳定。XAML 中 `assembly=mscorlib` 在当前 .NET 10 通过类型转发仍可解析，不属于应机械删除的条件分支；只有实际解析问题才单独处理。

`Themes/Theme.cs` 的 Theme/StandaloneTheme 与 `Tools/Helper/ResourceHelper.cs` 管理皮肤和资源字典；Demo 另有自己的资源层和切换逻辑。修改缓存、资源合并顺序、动态颜色时需同时测试重新应用模板。

## Demo 定位

- 启动与应用配置：`src/Shared/HandyControlDemo_Shared/App.xaml`、`App.xaml.cs`。并非位于 Net_GE45 项目目录。
- 导航数据：`Data/DemoInfo.json`；读取逻辑：`Service/Data/DataService.cs`。
- 示例类型创建：`Tools/Helper/AssemblyHelper.cs`；ViewModel 注册：`ViewModel/ViewModelLocator.cs`。
- 示例视图：`UserControl/Controls/`、`UserControl/Styles/`；界面仍使用现有 MVVM Light。

例如 NumericUpDown 修复需要联查：

- `Controls/Input/NumericUpDown.cs`：Value/Minimum/Maximum 强制转换、模板部件事件解绑、选择画笔绑定。
- `Themes/Styles/Base/NumericUpDownBaseStyle.xaml` 与 `Themes/Styles/NumericUpDown.xaml`：模板部件和命名样式。
- Demo `UserControl/Controls/NumericUpDownDemo.xaml` 与 `Tools/ValidationRule/NumericUpDownDemoRule.cs`。
- `doc/source/handycontrol/extend_controls/numericUpDown/index.md`：原有使用说明。

这些是修改定位入口，不表示已确认其中存在其他 Bug。
