# Payment Execution Monorepo

The Execution Service is a central data store designed to manage and track payment execution transactions across multiple payment providers

## Components

The Payment Execution project is composed of the following components:
- [Payment Execution Service API](./projects/service): A RESTful API that provides endpoints for managing payment execution transactions.
- [Payment Execution Worker](./projects/worker): A background worker that processes payment execution transactions (primarily to complete finalised transactions)
- [Cancel Execution Lambda](./projects/lambdas/cancel): An AWS Lambda function that cancels abandoned payment requests
- [Payment Execution Database](./projects/database): A PostgreSQL database that stores payment execution transactions and related data.

[Here is the detailed contribution guide](./CONTRIBUTING.md).

## Quick start

To run a project, cd into the project directory and run the following command:

```bash
make start-local-dependencies
```

Then you can run the component using the appropriate make command, or via your preferred startup commands in your IDE or terminal (see individual components READMEs for details)

> Note: to have components interact with localstack correctly if running from Rider, you may need to pretend we have EC2 metadata credentials. Run the following command if Rider complains about missing EC2 metadata:
> ```bash
> aws configure --profile "default"
> ```
> and enter "xxx" for your access key and secret key.  This is a known issue with localstack and the AWS CLI, and this workaround is necessary to allow localstack to work correctly with the AWS CLI in this case.

