# Local Setup

## 1. Set up environment

Run this command to check whether you have all the prerequisites and set up Git hooks:

On macOS:
```shell
$ scripts/envSetup.sh
```

On Windows:
```shell
PS> scripts/envSetup.ps1
```

You will need to install the following:
- [.NET SDK](https://dotnet.microsoft.com/download) for .NET development. The generated code uses .NET 8.0.
- [GNU Make](https://www.gnu.org/software/make/) to run tasks (e.g. running unit tests)
- [Docker](https://www.docker.com/) to run containerised tasks


The above scripts set up Git hooks for this workspace, which validate your changes when a Git command is run.

Supported validations:

On commit (`git commit`):
- When specifying a commit message, validate that the commit message starts with a Jira issue reference e.g. `[ABC-123] Fixed a typo`. This ensures that changes can be traced back to a Jira issue, helping your service align with [PTP XREQ-89](https://docs.google.com/document/d/1pFRgEkF-vohU3coxATKgOHqOe4Aiuyr7ZB2zWKOA5dE/view#heading=h.8th664zgyrt).
- Prior to a commit, run `dotnet format` across changes to `*.cs` files. If there are any formatting issues that cannot be automatically fixed, the commit is blocked.
- Prior to a commit, checks if scripts in the repo are marked as executable. Fails the commit if there are non-executable scripts.

On push (`git push`):
- Runs a secrets check against the repository.

## 2. Import Postman Collection and Environment

A basic Postman collection and Environment exists [here](../.postman). Import both of these into Postman (these may already exist in a team workspace).

## 3. Configure your IDE

### Visual Studio Code

Install the following VS Code extensions:

- [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp)
- [VS Code Docker](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-docker)
- [EditorConfig for VS Code](https://marketplace.visualstudio.com/items?itemName=EditorConfig.EditorConfig)

### JetBrains Rider

[Use EditorConfig with Rider](https://www.jetbrains.com/help/rider/Using_EditorConfig.html)

JetBrains Rider supports code formatting styles, code syntax styles, C# naming styles, and code inspection severity levels defined in the EditorConfig format.

### Visual Studio

[EditorConfig settings](https://docs.microsoft.com/en-us/visualstudio/ide/create-portable-custom-editor-options?view=vs-2022)

When you add an EditorConfig file to your project in Visual Studio, new lines of code are formatted based on the EditorConfig settings. The formatting of existing code isn't changed unless you run one of the following commands:

[Code Cleanup](https://docs.microsoft.com/en-us/visualstudio/ide/code-styles-and-code-cleanup?view=vs-2022) (Ctrl+K, Ctrl+E), which applies any white-space settings, such as indent style, and selected code style settings, such as how to sort using directives.

Configure VS2022 to run cleanup on save via `Options` > `Text Editor` > `Code Cleanup` > Tick `"Run Code Cleanup profile on Save"`.
