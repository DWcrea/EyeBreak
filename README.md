# EyeBreak · 护眼小助手

一款面向 Windows 10 / 11 x64 的**本地护眼提醒工具**。常驻系统托盘，按 **20-20-20 法则**工作：累计约 20 分钟有效电脑使用后，提醒你看向 6 米以外至少 20 秒。

不联网、无广告、不收集数据、不依赖手机或其他设备，适合长时间读论文、写代码、使用 Abaqus 等场景。

```
C# · .NET 10 LTS · Windows Forms · 零第三方 NuGet 依赖 · 简体中文界面
```

---

## 特性

| 能力 | 说明 |
|---|---|
| 20-20-20 提醒 | 累计有效使用时间达标后弹出轻量休息窗，显示倒计时，到时自动关闭并开始下一轮 |
| 智能空闲检测 | 基于 `GetLastInputInfo` 判断键鼠空闲；超过阈值暂停累计，恢复后**保留原进度** |
| 长休息机制 | 每完成 N 轮（默认 3 轮）安排一次 5 分钟长休息，**替代**该轮短休息，不会连续弹两次 |
| 非阻塞提醒 | 休息窗不锁定电脑、不禁用键鼠、不关闭其他软件，可随时手动关闭 |
| 休息时显示桌面 | 可选：休息开始时最小化所有窗口（等同 Win+D，**不关闭任何程序**），休息结束、推迟或退出程序时自动还原 |
| 托盘常驻 | 状态与倒计时、暂停/继续、立即休息、推迟 5 分钟、设置、开机启动、退出 |
| 托盘悬浮提示 | 鼠标悬停在托盘图标上显示当前状态与剩余时间（如「学习中 · 距提醒 19:32」） |
| 推迟提醒 | 推迟 5 分钟再次提醒；推迟期间不会触发第二个弹窗 |
| 中文设置窗口 | 工作间隔、短/长休息、长休息频率、空闲阈值、声音、自启、启动即计时 |
| 配置持久化 | 保存到 `%AppData%\EyeBreak\settings.json`，损坏时安全回退默认值 |
| 开机自启 | 可选，仅写入当前用户注册表启动项，不需要管理员权限 |
| 单实例运行 | 命名互斥锁保证只有一个计时实例，重复双击不会产生两个计时器 |
| 轻量稳定 | 纯 WinForms + Win32 API，不使用 Electron，不捆绑浏览器运行时 |

---

## 系统要求

| 项目 | 要求 |
|---|---|
| 操作系统 | Windows 10 / 11，x64 |
| 运行时 | **.NET 10 Desktop Runtime (x64)** —— 注意是 Desktop Runtime，不是仅 .NET Runtime |
| 权限 | 普通用户权限即可，无需管理员 |
| 网络 | **完全不需要**，程序不做任何后台网络请求 |

下载 .NET 10 Desktop Runtime：<https://dotnet.microsoft.com/download/dotnet/10.0>

检查本机是否已安装（应出现 `Microsoft.WindowsDesktop.App 10.x`）：

```powershell
dotnet --list-runtimes
```

---

## 下载与运行

1. 到本仓库的 **Releases** 页面下载 `EyeBreak.exe`（框架依赖型单文件版本，体积约 190 KB）。
2. 确保已安装上述 .NET 10 Desktop Runtime。
3. 双击 `EyeBreak.exe`。

程序**没有主窗口、也没有命令行黑框**，启动后直接驻留在任务栏通知区域（托盘）。如果看不到图标，请展开托盘的隐藏图标区域。

> 该版本为**框架依赖版**：文件很小，但目标电脑必须已安装 .NET 10 Desktop Runtime，**不能**在缺少运行时的电脑上独立运行。
> 程序未做代码签名，Windows 可能提示「未知发布者」，请只从本仓库 Releases 获取。

---

## 使用说明

**托盘图标右键菜单**

| 菜单项 | 作用 |
|---|---|
| 当前状态 | 只读显示：学习中 / 休息中 / 长休息中 / 已暂停 / 空闲暂停 / 提醒已推迟 |
| 距离下一次提醒 | 显示到下次提醒或休息结束的剩余时间 |
| 暂停计时 / 继续计时 | 手动暂停累计用眼时间，暂停期间不弹出提醒 |
| 立即休息 | 手动开始本轮休息，**不增加**长休息周期计数 |
| 推迟提醒 5 分钟 | 仅在自动提醒弹窗出现时可用；推迟期间不会重复弹窗 |
| 设置 | 打开中文设置窗口 |
| 开机启动 | 勾选状态开关，写入或删除当前用户启动项 |
| 退出 EyeBreak | 真正结束后台进程并释放托盘图标 |

**双击**托盘图标可打开设置窗口。

**鼠标悬停**在托盘图标上会显示当前状态与剩余时间，例如「EyeBreak · 学习中 · 距提醒 19:32」、「EyeBreak · 休息中 · 休息剩余 00:15」。

**休息窗口**：可等待倒计时结束、点击「结束休息」提前结束，或在自动提醒时选择「推迟 5 分钟」。

**休息时显示桌面（可选）**：在设置中勾选「休息时最小化所有窗口」后，休息开始时会最小化所有窗口，相当于按下 Win+D——**只是最小化，不会关闭或退出任何程序**，也不会丢失未保存的内容。以下任一情况发生时会自动还原窗口：休息倒计时结束、点击「结束休息」、「推迟 5 分钟」，或退出 EyeBreak。

该功能默认**关闭**：它会明显改变桌面状态，属于侵入性行为，因此需要你主动开启。手动点击「立即休息」时同样适用。需要注意，还原使用的是系统的「撤销全部最小化」，因此你在休息前已经手动最小化的窗口也会一并被还原。

---

## 设置项

| 设置项 | 默认值 | 可配置范围 |
|---|---:|---|
| 工作间隔 | 20 分钟 | 1–120 分钟 |
| 短休息时间 | 20 秒 | 5–300 秒 |
| 长休息时间 | 5 分钟 | 1–30 分钟 |
| 长休息频率 | 每 3 轮 | 1–12 轮，可关闭 |
| 键鼠空闲阈值 | 120 秒 | 60 / 120 / 180 / 300 秒 |
| 开机启动 | 关闭 | 手动启用 |
| 声音提醒 | 关闭 | 开关 |
| 启动后立即计时 | 开启 | 开关 |
| 休息时最小化所有窗口 | 关闭 | 开关（等同 Win+D，休息结束自动还原） |

配置保存于 `%AppData%\EyeBreak\settings.json`，首次启动自动生成。

**几点约定：**

- 修改「工作间隔」后，**当前周期保持原进度不变，新间隔从下一轮开始生效**。
- **计时进度不持久化**：退出程序后再次启动会重新计时，不会接着上次的剩余时间。
- 「启动后立即计时」关闭时，程序启动后处于暂停状态，需要手动「继续计时」。

---

## 工作原理

计时核心与界面完全分离，便于测试与扩展：

```text
Working（有效使用计时）
   ├─ 键鼠空闲达到阈值 → IdlePaused（暂停累计）→ 检测到操作 → Working
   ├─ 用户暂停         → UserPaused → 用户继续 → Working
   └─ 累计有效时间达标
          ├─ 非第 N 轮 → ShortBreak（默认 20 秒）→ Working
          └─ 第 N 轮   → LongBreak（默认 5 分钟）→ Working
```

计时使用单调时钟（`Stopwatch`）而非累加定时器触发次数，避免长时间运行后产生漂移。程序同时监听 Windows 会话切换与电源事件；锁屏、休眠唤醒或界面长时间阻塞时，不会把中断的时间一次性计入工作时长。

**关于精度的说明（请勿夸大其能力）：**

- 这是**电脑活动时间的近似估计**，不是摄像头视线追踪，无法确认人是否真的在看屏幕。
- 空闲阈值可调，因为阅读论文时可能数分钟不操作键鼠。
- 休息倒计时只表示提醒窗停留时长，不代表实际注视距离。

---

## 构建与测试

需要 **.NET 10 SDK**。在仓库根目录执行：

```powershell
# 编译
dotnet build .\EyeBreak.sln -c Release

# 自动化测试（计时状态机、配置持久化与提示文本，13 项）
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release

# 发布：框架依赖型单文件（体积小，需目标机安装 .NET 10 Desktop Runtime）
dotnet publish .\EyeBreak\EyeBreak.csproj -c Release -r win-x64 `
  --self-contained false -p:PublishSingleFile=true -p:DebugSymbols=false `
  -o .\outputs\win-x64-small

# 发布：自包含单文件（体积明显更大，目标机无需安装运行时）
dotnet publish .\EyeBreak\EyeBreak.csproj -c Release -r win-x64 `
  --self-contained true -p:PublishSingleFile=true -p:DebugSymbols=false `
  -o .\outputs\win-x64-standalone
```

**测试模式**（20 秒工作 → 5 秒休息，便于快速验证，不会修改已保存的正式设置）：

```powershell
.\outputs\win-x64-small\EyeBreak.exe --test-mode
```

**UI 冒烟测试**（临时启动发布版，检查启动、单实例、休息弹窗与自动关闭后结束进程）：

```powershell
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release `
  -- --ui-smoke .\outputs\win-x64-small\EyeBreak.exe
```

**显示桌面实测**（会短暂最小化当前桌面的所有窗口并立即还原，用受控窗口验证 MinimizeAll / UndoMinimizeAll 是否生效）：

```powershell
dotnet run --project .\tests\EyeBreak.Tests\EyeBreak.Tests.csproj -c Release -- --verify-shell
```

> 同一时刻只允许一个实例运行，测试模式不要与正式实例同时启动。
> 实际测试记录与尚未验证的项目见 [TESTING.md](TESTING.md)。

---

## 项目结构

```text
EyeBreak/
├── EyeBreak.sln
├── README.md
├── TESTING.md                 # 实测记录与未验证项
├── EyeBreak/
│   ├── EyeBreak.csproj
│   ├── app.manifest           # 每显示器 DPI 感知
│   ├── Program.cs             # 入口、单实例互斥
│   ├── TrayApplicationContext.cs
│   ├── Services/
│   │   ├── EyeBreakTimerService.cs   # 纯计时状态机（与 UI 解耦）
│   │   ├── IdleDetector.cs           # GetLastInputInfo 空闲检测
│   │   ├── SettingsStore.cs          # JSON 读写与损坏回退
│   │   ├── StartupManager.cs         # HKCU 开机启动项
│   │   └── DesktopMinimizer.cs       # 显示桌面 / 还原（Shell 对象模型）
│   ├── Models/
│   │   ├── AppSettings.cs
│   │   ├── DisplayText.cs            # 状态与提示文本拼装（可单元测试）
│   │   └── TimerState.cs
│   └── Forms/
│       ├── SettingsForm.cs
│       └── BreakReminderForm.cs
├── tests/EyeBreak.Tests/      # 自动化测试 + UI 冒烟测试
└── outputs/                   # 发布产物（未纳入版本控制）
```

---

## 常见问题

**双击 EXE 没有反应？**
检查是否为 x64 Windows，以及是否已安装 .NET 10 **Desktop** Runtime；用 `dotnet --list-runtimes` 确认存在 `Microsoft.WindowsDesktop.App 10.x`。另外确认是否已有一个实例在运行。

**托盘图标不见了？**
展开通知区域的隐藏图标；也可在任务管理器中确认 `EyeBreak.exe` 是否仍在运行。

**配置文件损坏了？**
程序会自动回退到默认配置而不会崩溃。如需彻底重置，退出程序后备份并删除 `%AppData%\EyeBreak\settings.json`，下次启动会重新生成。

**开机自启没有生效？**
自启项位于 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 下的 `EyeBreak` 值。移动或重命名过 EXE 后，请重新开关一次「开机启动」以更新路径。

**休息提醒打断了我正在做的事？**
提醒窗口不会锁定电脑。可在弹窗时选择「推迟 5 分钟」，或在托盘中调整工作间隔与空闲阈值；也可以在托盘中「暂停计时」。

---

## 卸载

1. 托盘右键 → 取消勾选「开机启动」。
2. 托盘右键 → 「退出 EyeBreak」。
3. 删除 `EyeBreak.exe` 及所在目录。
4. 如需清除设置，删除 `%AppData%\EyeBreak` 目录。

---

## 后续计划

以下为候选功能，**均不属于当前版本**：全屏游戏/演示时暂缓弹窗、午休与夜间静默时段、多显示器弹窗位置优化、仅本地存储的用眼统计、配置导入导出、便携模式。

V1 的核心原则是**功能可靠、占用低、不打断工作**，不会为了堆功能引入复杂依赖。

---

## 许可证

本仓库目前**尚未指定开源许可证**。在添加许可证文件之前，默认保留所有权利，他人不可直接复制、修改或再分发本项目代码。如果你计划开源，建议补充 MIT 或 Apache-2.0 等许可证文件。
