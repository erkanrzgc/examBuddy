Sure—here’s a richer README styled like the example you shared, but tailored to ExamBuddy. You can paste this into `README.md`:

---

# ExamBuddy – Study Task Manager

ExamBuddy is a lightweight Windows study planner built with WinForms. It lets you organize exam-week tasks, schedule study sessions, track status, and receive gentle reminders via the system tray.

> **Disclaimer**  
> ExamBuddy stores data locally in a plain-text JSON file (`tasks.db`). Review the code and adapt it before distributing builds to others.  

---

## Features

### Task Management
- Add study tasks with optional descriptions.
- Edit, delete, or mark tasks as completed.
- Three status values: `Pending`, `In Progress`, `Completed`.

### Scheduling & Reminders
- Optional `Çalışma saati` (study time) with date/time picker.
- Tray notifications appear 1 minute before the scheduled time.
- Tasks automatically drop off 2 hours after their scheduled time, preventing clutter.

### Persistence
- Tasks persist in `tasks.db` (JSON).  
- Legacy pipe-delimited format is still supported for backward compatibility.

### UI Notes
- Dark+ inspired theme.
- JetBrains Mono (fallback available).
- Automatic table column resizing based on window size.

---

## Usage

### 1. Build From Source
```bash
dotnet restore
dotnet build
```

### 2. Run in Debug
```bash
dotnet run --project ExamBuddy.csproj
```

### 3. Publish Executable (win-x64)
```bash
dotnet publish ExamBuddy.csproj \
  -c Release \
  -r win-x64 \
  --self-contained false \
  -o publish
```
Run `publish\ExamBuddy.exe`.

---

## Tray Notifications

- Keep the app running; Focus Assist must be turned off.
- Tray icon uses the executable’s icon (defaults to system application icon).
- Clicking the notification area reveals the icon if hidden.
- Reminder lead time is configurable in code (`_reminderLeadTime`, default 1 minute).

---

## Data Storage

- Location: same folder as the executable (`tasks.db`).
- Format: JSON (fallback to legacy pipe-delimited).
- To reset: delete `tasks.db`; a blank file is created on next run.

---

## Project Structure

| File            | Description                                  |
|-----------------|----------------------------------------------|
| `MainForm.cs`   | UI, task list management, notifications      |
| `TaskItem.cs`   | Task model and status helpers                |
| `Program.cs`    | Entry point, window sizing, DPI settings     |
| `ExamBuddy.csproj` | Project definition (.NET 8 WinForms)     |

---

## Example Interaction

1. Start the app (`ExamBuddy.exe`).
2. Enter a course name, optional description.
3. Enable `Çalışma saati` and pick a date/time.
4. Set status (default `Pending`).
5. Click **Ekle** to add task.
6. Wait − you’ll see a tray balloon 1 minute before the scheduled time.

---

## Development Notes

- Target Framework: `net8.0-windows`.
- Tray notifications implemented via `System.Windows.Forms.NotifyIcon`.
- JetBrains Mono is optional. If not installed, default font is used.
- Tasks auto-reload on startup; JSON is rewritten after each change.

---

## Roadmap Ideas

- Optional toast notifications via Windows App SDK.
- Custom reminder intervals per task.
- Multi-language support (currently Turkish labels).
- CSV/JSON export/import.

---

## License

MIT License – see `LICENSE`.

---

Let me know if you want this localized or want extra sections (screenshots, contributing, etc.).
