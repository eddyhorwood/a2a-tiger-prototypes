locals {
  cancel_execution_lambda_folder = "${path.module}/../../src/PaymentExecutionLambda.CancelLambda"
  cancel_execution_lambda_file   = "${local.cancel_execution_lambda_folder}/Function.cs"
  cancel_execution_lambda_hash   = substr(filemd5(local.cancel_execution_lambda_file), 0, 6)
  cancel_execution_lambda_zip    = "${local.cancel_execution_lambda_folder}/bin/Release/net8.0/PaymentExecutionLambda.CancelLambda-${local.cancel_execution_lambda_hash}.zip"
  cancel_execution_lambda_zip_s3 = "lambda-packages/PaymentExecutionLambda.CancelLambda-${local.cancel_execution_lambda_hash}.zip"
}

data "aws_subnets" "zone_services" {
  filter {
    name   = "tag:network.tier"
    values = ["zoneservices"]
  }
  filter {
    name   = "vpc-id"
    values = [var.vpc_id]
  }
}

resource "null_resource" "build_dotnet_lambda" {
  provisioner "local-exec" {
    command     = <<EOT
      dotnet tool install -g Amazon.Lambda.Tools
      $env:PATH += ":$HOME/.dotnet/tools"
      dotnet lambda package `
        --project-location ${local.cancel_execution_lambda_folder} `
        --output-package ${local.cancel_execution_lambda_zip} `
        --configuration Release
    EOT
    interpreter = ["pwsh", "-Command"]
    working_dir = path.module
  }
  triggers = {
    always_build = timestamp()
  }
}

resource "aws_lambda_function" "cancel_execution_lambda" {
  #checkov:skip=CKV_AWS_272: We don't need code-signing
  #checkov:skip=CKV_AWS_116:we don't need a DLQ for failed invocations
  function_name = "${var.cancel_execution_lambda_name}-${var.environment}"
  handler       = "PaymentExecutionLambda.CancelLambda::PaymentExecutionLambda.CancelLambda.Function_Handler_Generated::Handler"
  role          = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:role/lambda-execution-${var.environment}-${local.resource_name_prefix}"
  runtime       = "dotnet8"
  description   = "Lambda for Cancelling abandoned payment requests"
  timeout       = 30
  memory_size   = 512
  tags          = merge({
    "sumo:sourceCategory" = "collecting_payments_execution/cancel_execution_lambda/${var.environment}/app/aws_lambda"
  }, local.common_tags)
  reserved_concurrent_executions = 100
  s3_bucket                      = aws_s3_object.cancel_execution_lambda_package.bucket
  s3_key                         = aws_s3_object.cancel_execution_lambda_package.key
  source_code_hash               = aws_s3_object.cancel_execution_lambda_package.etag

  depends_on = [
    aws_s3_object.cancel_execution_lambda_package,
    aws_cloudwatch_log_group.cancel_execution_lambda,
    kora_resource.cancel_execution_lambda_exec_role,
    aws_security_group.cancel_lambda_security_group,
  ]
  tracing_config {
    mode = "Active"
  }

  vpc_config {
    security_group_ids = [aws_security_group.cancel_lambda_security_group.id]
    subnet_ids         = data.aws_subnets.zone_services.ids
  }

  environment {
    variables = {
      #checkov:skip=CKV_AWS_173: Skip check as these are not sensitive variable
      ENVIRONMENT = var.environment
      Override_DataAccess__PaymentExecutionDB__ConnectionString = "collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/connection-string"
      Override_LaunchDarkly__SdkKey = "collecting-payments-execution/cancel-execution-lambda/launch-darkly-config/xero-global-sdk-key"
      #checkov:skip=CKV_SECRET_6: Environment variable contains secret reference path, not secret value
      Override_Identity__Client__ClientSecret = "collecting-payments-execution/cancel-execution-lambda/identity-client-secret"
    }
  }
}

resource "aws_cloudwatch_log_group" "cancel_execution_lambda" {
  #checkov:skip=CKV_AWS_158: SumoLogForwarder does not support encrypted log groups
  name              = "/aws/lambda/${var.cancel_execution_lambda_name}-${var.environment}"
  retention_in_days = 365

  tags = {
    "uuid" = local.common_tags.uuid
  }
}

resource "aws_lambda_event_source_mapping" "cancel_lambda_sqs_trigger" {
  event_source_arn                   = module.cancel-execution-queue.cancel_execution_sqs_arn
  function_name                      = aws_lambda_function.cancel_execution_lambda.function_name
  batch_size                         = 10
  maximum_batching_window_in_seconds = 0
  enabled                            = true
  function_response_types            = ["ReportBatchItemFailures"]

  depends_on = [
    aws_lambda_function.cancel_execution_lambda,
    module.cancel-execution-queue,
  ]
}
