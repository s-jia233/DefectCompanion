# 故障机器人 · 互动桌宠

《杀戮尖塔 2》故障机器人同人桌宠，支持卡牌动作、充能球互动和桌面拖动。

## 下载

[下载 Windows 免安装包](https://github.com/s-jia233/DefectCompanion/releases/latest/download/DefectCompanion-v1.0.1-windows-portable.zip) · [全部版本](https://github.com/s-jia233/DefectCompanion/releases)

完整解压后运行 `DefectCompanion.exe`。适用于 Windows 10 / 11，需要 .NET Framework 4.8；无需启动游戏。

## 操作

| 操作 | 功能 |
| --- | --- |
| 拖动机器人 | 调整位置 |
| 单击 / 双击机器人 | 生成雷球 / 双重释放 |
| 点击充能球 | 激发该球 |
| 按住机器人不动 | 充电，松开获得储能 |
| 右键机器人 | 打开卡牌选择 |
| 左键卡牌 | 出牌 |
| 再次右键 / Esc | 取消选牌 |
| 点击翻页按钮 | 切换卡牌页 |
| 悬停脚下图标 | 查看能力状态 |

系统托盘菜单可调整大小、置顶、安静模式，也可收起或退出。重新运行程序可找回已收起的桌宠。

## 编译

在 Windows PowerShell 中运行 `.\build.ps1`，程序输出到 `release` 文件夹。版本号由 `version.json` 管理；运行 `python .\package_release.py` 可打包。

[更新说明](CHANGELOG.md) · [素材来源与许可](THIRD_PARTY_NOTICES.md)
