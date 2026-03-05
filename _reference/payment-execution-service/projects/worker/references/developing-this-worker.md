# Developing this worker

## Running the worker

The recommended way to develop this worker is via Visual Studio Code.

In the Run and Debug tab there are the following launch configurations:
 - `Run and Debug Worker`
 - `Run and Debug Worker in Docker`
 - `Attach to .NET Process`
 - `Run Worker with Hot Reload`

Dependencies are started prior to debugging and stopped once the debugger has been detached.

Alternatively you can:
<details><summary>Run the worker via the CLI</summary>

```sh
# Start local dependencies
$ make start-local-dependencies

# Run the worker
$ dotnet run --project src/PaymentExecutionWorker.Worker

# Stop local dependencies
$ make stop-local-dependencies
```

</details>

<details><summary>Run the worker in a Docker container</summary>

```sh
# Start local dependencies
$ make start-local-dependencies

# Run container
$ make start-worker

# Stop container
$ make stop-worker

# Stop local dependencies
$ make stop-local-dependencies
```

</details>


## Tasks

### Running tasks

```sh
# Confirm you have GNU Make installed
$ make --version

# Compile the worker's TeamCity Kotlin pipeline code
$ make build-teamcity

# Run unit tests with code coverage
$ make unit-test

# Run Sonarscan against repository
$ make sonarscan

# Run Checkov against repository
$ make checkov

# Run a Checkov secrets check against repository
$ make checkov-secrets-only

# Builds and starts the worker from the Dockerfile
$ make start-worker

# Stops the worker
$ make stop-worker

# Start local dependencies
$ make start-local-dependencies

# Stop local dependencies
$ make stop-local-dependencies
```

## Logs

Seq aggregates logs from everything running in your worker's local environment.

`make start-local-dependencies` will start Seq, you can view and query your local environments aggregated logs via your browser at http://localhost:5341.

[Search and analyze logs with Seq](https://docs.datalust.co/docs/the-seq-query-language).
