# 家具桌面 · Desktop Furniture

Windows 10 / 11 的家具式快捷方式收纳工具，使用 C# / WPF / .NET 8。

## 使用

在 Windows 上安装 .NET 8 SDK 后，在项目目录执行：

```powershell
dotnet run
```

生成无需安装 .NET 运行时的 x64 版本：

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

也可以在 GitHub Actions 的 Build Windows app 中下载 DesktopFurniture-win-x64 构建产物，解压并运行 DesktopFurniture.exe。

- 左上角储物仓添加任意数量的地毯、抽屉、带盖收纳盆。
- 拖动家具顶部摆放；点击盖子或把手开合，收起时内容隐藏。
- 展开后从资源管理器拖入 .lnk、.exe、文件、文件夹。双击打开，右键移出收纳或移动到其他家具。
- 右键家具顶部重命名或移除家具。托盘菜单可显示/隐藏家具、退出。
- 数据保存在 `%LOCALAPPDATA%\DesktopFurniture\layout.json`，原子替换保存；解析失败先备份。

## 明确的边界

这是可编译的第一版源码，尚未在真实 Windows 环境验证。当前使用透明无边框独立窗口，不强制置顶；普通应用窗口可以遮住家具。尚未实现 Explorer WorkerW 桌面嵌入，因此 Win+D 也可能隐藏家具；可从托盘恢复。

收纳保存原路径引用，不移动、不删除原始文件，不自动隐藏 Explorer 桌面图标。要清空桌面，可在收纳确认后手动将原快捷方式移到自己的快捷方式备份目录，再重新从备份目录拖入；不要删除被引用的快捷方式。

当前抽屉使用滑动展开动画，地毯和盆子使用同一展开机制；家具为统一暖木色的简化二维外观，尚未达到视频中的写实材质与独立掀盖动画。多显示器位置按虚拟桌面保存；显示器断开后完全离屏的家具会回到主屏工作区。

程序不会自动开机启动，不需要管理员权限。不建议以管理员身份运行，避免 Windows 权限隔离影响拖放。
