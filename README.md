# 故障机器人 · 互动桌宠 v1.0.0

把《杀戮尖塔 2》的故障机器人留在桌面上：拖动摆放、右键选牌、点击充能球，让卡牌动作在桌面上发生。

这是 Windows 非官方同人桌宠，首次正式发布基于内部开发快照 v23。

## 下载与启动

下载 [Windows 免安装包](https://github.com/s-jia233/DefectCompanion/releases/download/v1.0.0/DefectCompanion-v1.0.0-windows-portable.zip)，完整解压后双击 `DefectCompanion.exe`。

全部文件与后续更新见 [Releases](https://github.com/s-jia233/DefectCompanion/releases)。

适用于 Windows 10 / 11，需要 .NET Framework 4.8。程序离线运行，不需要账户、Python、Node.js 或启动游戏。

## 怎么玩

| 操作 | 效果 |
| --- | --- |
| 按住后立即拖动 | 移动桌宠 |
| 轻点机器人 | 生成一颗闪电球；会等待系统双击判定时间 |
| 双击机器人 | 最右侧充能球连续激发两次 |
| 点击充能球 | 主动激发这一颗球 |
| 按住机器人不动 | 约 0.55 秒后开始充电，松开获得储能 |
| 右键机器人 | 打开卡牌选择，支持搜索和分类 |
| 左键卡牌 | 出牌并展示相应动作与特效 |
| 再次右键或按 Esc | 取消选牌 |
| 卡牌界面底部箭头 | 翻页 |
| 悬停或点击脚下能力图标 | 查看当前能力和次数 |

共有 36 张卡牌和闪电、冰霜、黑暗、等离子四种充能球。默认三槽，使用“扩容”后最多五槽，沿机体上方弧形排列。新球从左侧进入，最早的球在最右侧；满槽时会挤出并激发最右侧的球。卡牌效果经过桌宠改编，可自由使用。

火箭飞拳、超能光束、双重释放、雷霆、雷暴、爪击、冰之长枪、超频、机器学习和聚变等有对应动作与特效。机器学习伸展原有颈部和电线，超频将原有核心变为炽红材质。

## 日常设置

- Windows 系统托盘菜单可调整大小、置顶和安静模式，也可以暂时收起或退出。
- 收起后双击托盘图标，或再次启动程序，即可找回桌宠。
- 位置、大小和偏好保存在 `%LOCALAPPDATA%\DefectCompanion\settings.txt`。卡牌和能力状态在重启后重置。
- 跨显示器不同缩放比例、全屏游戏上的显示行为尚未做完整测试。目标为 60 帧绘制，实际表现取决于设备与桌面负载。

## 修改与编译

下载 `DefectCompanion-v1.0.0-source.zip` 后，进入源码目录，在 Windows PowerShell 中运行：

```powershell
.\build.ps1
```

构建使用 .NET Framework C# 编译器，并运行交互规则检查。运行文件输出到 `release` 文件夹。素材已经包含在源码包中，编译时不需要重新提取游戏资源。

发布版本统一由 `version.json` 管理；构建时同步 Windows 文件版本和产品版本。运行下面的命令可生成运行包、源码包和 SHA-256 校验文件：

```powershell
python .\package_release.py
```

默认发布目录为 `dist`；也可使用 `--output-dir` 指定路径。该命令需要先构建 `release`，或通过 `--binary-dir` 指定已构建的文件夹。

版本规则见 [VERSIONING.md](VERSIONING.md)，本次内容见 [CHANGELOG.md](CHANGELOG.md)。过去 v1 至 v23 属于内部迭代编号，记录见 [开发历史](docs/INTERNAL_HISTORY.md)。

## 素材与参考

角色和游戏素材来自 Mega Crit 的《杀戮尖塔 2》。部分动画、贴图和绑定数据参考 page_ 的 [More Defect Animations / 更多机器人动画](https://steamcommunity.com/sharedfiles/filedetails/?id=3810696310)，并为二维桌宠做了适配。桌宠不加载模组 DLL。

来源说明见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。本项目不代表游戏官方。
