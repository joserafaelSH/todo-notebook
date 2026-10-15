# Todo Notebook

A tiny Windows todo list. Each todo is a card with a title, a description and a date. The whole UI is the list of cards: open it with a hotkey, add todos from anywhere with a quick-add hotkey, and get a tray reminder at 10:00 and 16:00. Same look as [Goal Notebook](https://github.com/joserafaelSH/goal-notebook) (dark theme only for now).

- **Ctrl+Alt+T** shows or hides the list, **Ctrl+Alt+A** opens quick add (Enter adds, Esc closes), **Ctrl+N** adds from inside the list
- Click a card to edit it; tick it to mark it done
- Your data is a plain `todos.json` in `%AppData%\TodoNotebook` (hotkeys live in `settings.json` next to it)
- Everything stays on your machine; there is no account and no telemetry

## Download

Grab `TodoNotebook-win-x64.zip` from the [latest release](../../releases/latest) (or from the artifacts of a [workflow run](../../actions)), unzip it anywhere and run `TodoNotebook.exe`. It bundles the .NET runtime, so nothing else needs to be installed. Windows may show a SmartScreen warning because the exe is not signed: choose *More info > Run anyway*.

## Build from source

Requires Windows and the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```powershell
dotnet run                       # run
.\publish.ps1 -SelfContained     # single-file exe in .\dist
```

## Releases

Pushing a tag such as `v1.0.0` makes GitHub Actions build and attach the zip to a new release:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

## License

[MIT](LICENSE)
