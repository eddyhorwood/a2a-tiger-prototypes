locals {
  aws_account_id = data.aws_caller_identity.current.account_id

  new_relic_license_secret_arn         = "arn:aws:secretsmanager:${local.aws_region}:372425709208:secret:newrelic.xero_fsrv.licensekey-??????"
  identity_client_secret_arn           = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/payment-execution-service/identity-client-secret-??????"
  db_connection_secret_arn             = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/connection-string-??????"
  launch_darkly_global_project_sdk_key = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/payment-execution-service/launch-darkly-config/xero-global-sdk-key-??????"
}

data "aws_iam_policy_document" "read_new_relic_license_key_policy" {
  statement {
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue"
    ]
    resources = [
      local.new_relic_license_secret_arn
    ]
  }

  statement {
    effect = "Allow"
    actions = [
      "kms:Decrypt",
      "kms:DescribeKey",
      "kms:ListAliases"
    ]
    resources = [
      var.newrelic_kms_arn
    ]
  }
}

data "aws_iam_policy_document" "read_identity_client_secret_policy" {
  statement {
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue"
    ]
    resources = [
      local.identity_client_secret_arn
    ]
  }

  statement {
    effect = "Allow"
    actions = [
      "kms:Decrypt",
      "kms:DescribeKey",
      "kms:ListAliases"
    ]
    resources = [
      var.secrets_kms_arn
    ]
  }
}

data "aws_iam_policy_document" "read_launch_darkly_config_secrets" {
  statement {
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue"
    ]
    resources = [
      local.launch_darkly_global_project_sdk_key,
    ]
  }
}

data "aws_iam_policy_document" "read_db_connection_secret_policy" {
  statement {
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue"
    ]
    resources = [
      local.db_connection_secret_arn
    ]
  }

  statement {
    effect = "Allow"
    actions = [
      "kms:Decrypt",
      "kms:DescribeKey",
      "kms:ListAliases"
    ]
    resources = [
      var.db_secrets_kms_arn
    ]
  }
}

data "aws_iam_policy_document" "read_parameter_store_policy" {
  statement {
    effect = "Allow"
    actions = [
      "ssm:Get*"
    ]
    resources = [
      "arn:aws:ssm:${local.aws_region}:${local.aws_account_id}:parameter/collecting-payments-execution/collecting-payments-execution/*"
    ]
  }
}

data "aws_iam_policy_document" "send_message_sqs_policy" {
  statement {
    effect = "Allow"
    actions = [
      "sqs:SendMessage"
    ]
    resources = [
      "arn:aws:sqs:${local.aws_region}:${local.aws_account_id}:collectingpayments-execution-payment-execution-queue-${var.environment}",
      "arn:aws:sqs:${local.aws_region}:${local.aws_account_id}:collectingpayments-execution-cancel-execution-queue-${var.environment}"
    ]
  }

  statement {
    effect = "Allow"
    actions = [
      "kms:Decrypt",
      "kms:Encrypt",
      "kms:DescribeKey",
      "kms:ListAliases",
      "kms:GenerateDataKey*"
    ]
    resources = [
      local.execution_sqs_kms_key_arn
    ]
  }
}
