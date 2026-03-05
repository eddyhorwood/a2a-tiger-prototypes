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
- [GNU Make](https://www.gnu.org/software/make/) to run tasks (e.g. building the TeamCity pipeline)
- [Docker](https://www.docker.com/) to run containerised tasks

The above scripts set up Git hooks for this workspace, which validate your changes when a Git command is run.

Supported validations:

On commit (`git commit`):
- When specifying a commit message, validate that the commit message starts with a Jira issue reference e.g. `[ABC-123] Fixed a typo`. This ensures that changes can be traced back to a Jira issue, helping your service align with [PTP XREQ-89](https://docs.google.com/document/d/1pFRgEkF-vohU3coxATKgOHqOe4Aiuyr7ZB2zWKOA5dE/view#heading=h.8th664zgyrt).
- Prior to a commit, run `dotnet format` across changes to `*.cs` files. If there are any formatting issues that cannot be automatically fixed, the commit is blocked.
- Prior to a commit, checks if scripts in the repo are marked as executable. Fails the commit if there are unexecutable scripts.

On push (`git push`):
- Ensures any changed TeamCity code is compilable.
- Runs a secrets check against the repository.

## 2. Configure your IDE

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

Configure VS2022 to run cleanup on save via `Options` > `Text Editor` > `Code Cleanup` > Tick `"Run Code Cleanup profile on Save"`


### Localstack
We use localstack to mock locally aws resources e.g. sqs queue for local testing. To spin up localstack, you can run `make start-local-dependencies`
which will start all the dependencies including local stack and terraform runner container which will provision all the necessary terraform resources in localstack.

You can access these aws resources either using aws cli tool or awsLocal CLI tool. 

For example, if you use AWS CLI, then you need to setup aws custom profile to your aws config file. (`/.aws/config`)
```
[profile localstack]
region=us-east-1
output=json
endpoint_url = http://localhost:4566
```

Add the following profile to your AWS credentials file (by default, this file is at ~/.aws/credentials):
```
[localstack]
aws_access_key_id=test
aws_secret_access_key=test
```

You can use AWS cli to check the queue in localstack
```
> aws sqs list-queues --profile localstack
```
You can set `AWS_DEFAULT_PROFILE` env variable in bash to set the default aws profile.

More info you can visit localstack [website](https://docs.localstack.cloud/user-guide/integrations/aws-cli/#localstack-aws-cli-awslocal).



