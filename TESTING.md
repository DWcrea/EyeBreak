# EyeBreak V1 测试记录（2026-10-15）

环境：Windows x64；项目局部 .NET SDK 10.0.402；Windows Desktop Runtime 10.0.12。Release 构建：**0 警告、0 错误**。没有系统级安装 SDK。所有测试均在本机执行，不将设计推断当成实测。

| 教程验收点 | 结果与验证方式 |
|---|---|
| Windows 发布版启动、无黑框 | 通过：实测发布版进程启动并持续运行；项目类型为 WinExe。未做屏幕录制。 |
| 托盘图标出现，菜单可用，双击设置 | 未人工目视/点击验证（当前会话仅做 Win32 窗口探测）；代码创建可见 NotifyIcon 及中文菜单。 |
| 测试模式 20 秒工作后弹窗 | 通过：UI 冒烟测试运行发布 EXE，约 19.0 秒检测到可见休息窗口（采样/启动时刻误差）。 |
| 休息约 5 秒关闭、重新计时 | 通过：发布版窗口约 5.0 秒关闭；状态机测试验证进入新周期。 |
| 正式 20 分钟 / 20 秒配置 | 通过：默认配置及状态机参数测试；实际等待 20 分钟未验证。 |
| 手动暂停、恢复保留进度 | 通过：状态机自动测试；托盘菜单人工点击未验证。 |
| 空闲暂停、活动恢复 | 通过：模拟空闲输入的状态机自动测试；实际静置 120 秒未验证。 |
| 锁屏 / 休眠唤醒不连续弹窗 | 部分验证：代码订阅 Windows 会话/电源事件并丢弃过长时钟间隔；未执行真实锁屏及休眠试验。 |
| 第 3 轮长休息替代短休息 | 通过：状态机测试连续模拟三轮，长休息只出现一次。 |
| 推迟 5 分钟且无重复弹窗 | 通过：状态机测试模拟 299 秒、到期时空闲及再次活动。实际弹窗按钮点击未验证。 |
| 保存设置重启仍生效 | 通过：配置读写测试使用临时测试文件重新加载；实际设置窗口人工操作未验证。 |
| 损坏配置恢复默认 | 通过：临时文件写入无效 JSON，加载自动恢复并回写默认值。 |
| 开机自启写入/删除 | 未验证：避免自动测试修改用户注册表；实现只操作 HKCU Run 的 EyeBreak 值。建议用户自行切换并检查。 |
| 第二次启动不产生双计时实例 | 通过：实际双启发布版，第二进程正常退出、首进程继续运行。 |
| 退出后进程及托盘图标消失 | 部分验证：退出逻辑释放托盘图标；测试进程由测试程序结束，未实际点击“退出”。 |
| 发布目录直接运行 | 通过：冒烟测试直接执行发布目录的 EXE。 |
| 说明运行环境 | 通过：README 明确 .NET 10 Desktop Runtime x64。 |

自动化测试：**11/11 通过**；UI 冒烟测试：启动、单实例、休息弹窗、自动关闭通过；该次实例工作集约 **65.3 MiB**（仅单次测量，随机器和运行状态变化）。没有为追求小体积开启 trimming/AOT，也没有任何后台联网功能。

构建发布命令：

```powershell
dotnet build .\EyeBreak.sln -c Release
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release
dotnet publish .\EyeBreak\EyeBreak.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:DebugSymbols=false -o .\outputs\win-x64-small
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release -- --ui-smoke .\outputs\win-x64-small\EyeBreak.exe
```
