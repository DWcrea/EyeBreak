# EyeBreak（护眼小助手）

Windows 10/11 x64 本地护眼提醒工具：后台托盘常驻，中文界面，不联网、无广告、无第三方 NuGet 包。默认累计约 20 分钟有效键鼠使用后，提醒远眺 6 米外至少 20 秒；每完成 3 轮安排一次 5 分钟长休息，替代短休息。

## 直接使用

1. 确保 Windows 已安装 **.NET 10 Desktop Runtime x64**（注意是 Desktop Runtime，不是仅 .NET Runtime）：https://dotnet.microsoft.com/download/dotnet/10.0
2. 双击 `outputs\win-x64-small\EyeBreak.exe`；没有主窗口及命令行黑框，程序显示在任务栏通知区域（托盘）。若未看到图标，可展开托盘隐藏图标。
3. 右键托盘图标查看状态与倒计时，可暂停/继续、立即休息、打开设置、开启开机启动或退出；双击图标打开设置。
4. 工作达到间隔时出现非锁定的休息窗。可等待倒计时结束、结束休息，或选择“推迟 5 分钟”。推迟期间不会重复提醒，到期后键鼠活动时再次弹窗。手动“立即休息”不增加长休息周期计数。
5. 设置保存在 `%AppData%\EyeBreak\settings.json`；工作间隔更改从**下一轮**生效，当前进度保留。**退出及再次启动后重新计时**，计时进度不持久化。启动后立即计时可在设置里关闭。

> 小体积版本是**框架依赖版**，不能在缺少 .NET 10 Desktop Runtime 的电脑上独立运行；若双击无响应，先检查运行时是否安装。发布版未签名，Windows 可能提示未知发布者，请只从可信来源获取。

## 设置与机制

工作间隔 1–120 分钟（默认 20）；短休息 5–300 秒（默认 20）；长休息 1–30 分钟（默认 5）；每 1–12 轮一次长休息（默认 3，可关闭）；空闲阈值 60 / 120 / 180 / 300 秒（默认 120）；开机启动、声音提醒默认关闭，启动后立即计时默认开启。

键鼠长时间无操作时暂停有效使用计时，回来继续原进度；这是电脑活动时间的**近似估计**，不能检测视线或阅读时的真实用眼。锁屏/唤醒或程序长时间阻塞不会一次性补记工作时间。休息弹窗不强制锁定屏幕，可以手动关闭。休息倒计时也不是实际注视距离的证明。

## 编译与测试

需要 .NET 10 SDK，进入此项目根目录：

```powershell
dotnet build .\EyeBreak.sln -c Release
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release
dotnet publish .\EyeBreak\EyeBreak.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugSymbols=false -o .\outputs\win-x64-small
```

测试模式：`EyeBreak.exe --test-mode`，临时采用 **20 秒工作 + 5 秒短休息**，不会更改保存的正式设置。不要与正式实例同时启动：程序只允许一个实例运行。UI 冒烟测试（测试程序会临时启动、检查并结束发布的进程）：

```powershell
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release -- --ui-smoke .\outputs\win-x64-small\EyeBreak.exe
```

## 排错及卸载

- 托盘图标不见：检查通知区域折叠图标和任务管理器中的 `EyeBreak.exe`；确认是否已运行另一个实例。
- 双击 EXE 不启动：检查 x64 Windows 与 .NET 10 **Desktop Runtime**；可用 `dotnet --list-runtimes` 检查是否存在 `Microsoft.WindowsDesktop.App 10.x`。
- 设置损坏：退出程序，备份后手动移走 `%AppData%\EyeBreak\settings.json`，下次启动生成默认配置。
- 卸载：在托盘菜单关闭“开机启动”，再点“退出 EyeBreak”；删除程序文件。如需清除设置，可自行删除 `%AppData%\EyeBreak`。自启项仅为当前用户 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 中的 `EyeBreak` 值；移动 EXE 后应重新开启自启以更新路径。

## 后续可扩展

可以增加全屏暂缓、安静时段、多显示器弹窗位置改进、仅本地的统计；上述功能**不属于当前版本**。
