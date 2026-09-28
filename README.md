# Employee CRUD — C# WPF + SQLite

A complete Windows desktop starter project you can edit, build, and debug in **Visual Studio Code**. No SQL Server installation is needed.

## Features

- **Create:** add an employee with name, email, and department.
- **Read:** display saved employees in a sortable table; search all three fields.
- **Update:** select a row, edit its details, and save changes.
- **Delete:** remove an employee after a confirmation prompt.
- Required-field and email-format validation; case-insensitive unique email addresses.
- Parameterized SQL and automatic local database creation.
- VS Code build/debug configuration and repository smoke tests.

## 1. Install prerequisites (Windows)

1. Install [Visual Studio Code](https://code.visualstudio.com/).
2. Install the **.NET 10 SDK for Windows**, not just the runtime, from [Microsoft](https://dotnet.microsoft.com/download/dotnet/10.0).
3. In VS Code, install the **C# Dev Kit** extension by Microsoft (`ms-dotnettools.csdevkit`). It also installs the C# extension used by the debugger.
4. Restart VS Code after installing the SDK. Check the integrated terminal:

```powershell
dotnet --version
```

The version should be `10.0.xxx` or newer with .NET 10 targeting support.

> WPF is Windows-only. macOS, Linux, WSL, and browser previews cannot run this window. Open the project in a normal Windows VS Code window, not a WSL or Linux container window. VS Code does not include a built-in WPF visual designer; edit XAML and run the app to see it.

## 2. Open and run

Extract `EmployeeCrud.zip`. In VS Code, choose **File → Open Folder** and select the extracted **EmployeeCrud** folder containing `EmployeeCrud.csproj` and `.vscode`.

Open **Terminal → New Terminal**, then run:

```powershell
dotnet restore .\EmployeeCrud.csproj
dotnet run --project .\EmployeeCrud.csproj
```

Alternatively, press **F5** and select **Employee CRUD (WPF)**. The included launch configuration builds before starting the debugger. **Ctrl+Shift+B** builds without launching.

The first restore requires internet access to download NuGet packages.

## 3. Try the CRUD workflow

1. Enter `Alex Santos`, `alex@example.com`, and `Engineering` in the form.
2. Click **Add employee**. The saved row appears and is selected.
3. Change the department to `Operations`; click **Save changes**.
4. Click **New / clear form** to switch back to adding another employee.
5. Search by name, email, or department using the search box above the table.
6. Select a row and click **Delete employee**. Choose **No** to cancel or **Yes** to permanently delete.
7. Close and reopen the app to verify records remain saved.

**Form behavior:** selecting another row, clicking New, or successfully refreshing replaces unsaved form edits. Search filters the list but does not clear the editor; its employee number identifies the record being edited. A successful save clears the search so the saved record is visible.

## Database location

The app creates the database and table automatically at:

```text
%LOCALAPPDATA%\EmployeeCrud\employees.db
```

For example: `C:\Users\YourName\AppData\Local\EmployeeCrud\employees.db`.

Data survives rebuilding or moving the project. To reset the app, close all instances and delete this database file. To back it up, close the app and copy the file. SQLite data is **not encrypted**; use appropriate Windows account/file permissions for real employee information. This starter app has no login, authorization, audit trail, or multi-user server backend.

## Project layout

```text
EmployeeCrud/
├── EmployeeCrud.csproj          .NET 10 WPF project and SQLite dependencies
├── App.xaml                    Shared resources
├── App.xaml.cs                 Database initialization and startup
├── MainWindow.xaml             Directory table and employee editor
├── MainWindow.xaml.cs          CRUD events, filtering, validation, feedback
├── Models/
│   └── Employee.cs             Employee data model
├── Data/
│   └── EmployeeRepository.cs   Parameterized SQLite commands
├── Styles/
│   └── Controls.xaml           Shared control styles
├── .vscode/
│   ├── extensions.json         Suggested C# extension
│   ├── tasks.json              Build task
│   └── launch.json             F5 debugger configuration
└── tests/
    ├── RepositorySmokeTests.csproj
    └── Program.cs              Database behavior checks
```

## How it works

`App.OnStartup` creates the local data folder, initializes the schema, and injects the repository into the main window.

| Operation | Repository method | SQL |
|---|---|---|
| Create | `Create(employee)` | `INSERT ... RETURNING Id` |
| Read | `GetAll()` | `SELECT ... ORDER BY FullName` |
| Update | `Update(employee)` | `UPDATE ... WHERE Id = $id` |
| Delete | `Delete(id)` | `DELETE ... WHERE Id = $id` |

All employee values are passed as SQL parameters, not concatenated into SQL strings. Each operation disposes its connection and command. The unique email constraint lives in the database, so it also protects against duplicates outside the UI.

This beginner-friendly version uses **WPF code-behind plus a separate repository**, not MVVM. It loads the directory into memory and makes short synchronous database calls on the UI thread; for a large dataset or slow storage, move database work off the UI thread and add database-side searching/pagination.

## Run the database tests

```powershell
dotnet run --project .\tests\RepositorySmokeTests.csproj
```

These tests use a temporary database, never the application's real employee database. They test create/read/update/delete, persistence, duplicate email rejection, quoted names, and missing records.

**Verification performed:** the WPF project cross-compiled successfully with zero warnings and zero errors; all 10 repository smoke checks passed. The WPF window itself has not been visually/runtime-tested because the development environment is Linux. On Windows, use the workflow above to verify the UI and F5 debugging.

## Optional: publish a standalone Windows executable

For 64-bit Intel/AMD Windows:

```powershell
dotnet publish .\EmployeeCrud.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Find the executable under `bin\Release\net10.0-windows\win-x64\publish\`. The executable is larger because it includes the .NET runtime. For Windows ARM64, change the runtime identifier to `win-arm64`.

## Troubleshooting

- **`dotnet` is not recognized:** install the Windows .NET SDK and reopen VS Code/your terminal.
- **The SDK does not support .NET 10:** install the .NET 10 SDK; check `dotnet --list-sdks`.
- **F5 cannot find the program:** open the `EmployeeCrud` folder itself (not its parent), and run the build task first.
- **Duplicate email message:** emails must be unique, ignoring letter case.
- **Database locked/unavailable:** close any external SQLite editor or competing writer, verify the local data folder is writable, then retry.
- **XAML changes are not visible:** stop the app and rebuild/relaunch it.

## Dependencies

- `Microsoft.Data.Sqlite` 10.0.9
- `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5, explicitly pinned to use an updated native SQLite bundle rather than the older transitive bundle.

Review dependency updates and vulnerability reports periodically (`dotnet list package --vulnerable --include-transitive`).
