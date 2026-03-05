locals {

  aws_account_id             = data.aws_caller_identity.current.account_id
  db_connection_secret_arn   = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/connection-string-??????"
  identity_client_secret_arn = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/payment-execution-worker/identity-client-secret-??????"

  launch_darkly_global_project_sdk_key = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/payment-execution-worker/launch-darkly-config/xero-global-sdk-key-??????"


  env_to_new_relic_license_kms_arn = {
    "test"       = "arn:aws:kms:ap-southeast-2:333186395126:key/bda77658-1fb1-42b5-948e-5ea5c90646de"
    "uat"        = "arn:aws:kms:us-west-2:333186395126:key/252ff4e9-5be7-4ac3-ab66-5e1d0f243534"
    "production" = "arn:aws:kms:us-east-1:333186395126:key/314cfc4f-df77-4e23-bede-d6458edc65ad"
  }

  new_relic_license_kms_arn = local.env_to_new_relic_license_kms_arn[var.environment]

  new_relic_license_secret_arn = "arn:aws:secretsmanager:${local.aws_region}:372425709208:secret:newrelic.xero_fsrv.licensekey-??????"

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
      local.new_relic_license_kms_arn
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

data "aws_iam_policy_document" "process_sqs_message_policy" {
  statement {
    effect = "Allow"
    actions = [
      "sqs:ReceiveMessage",
      "sqs:DeleteMessage"
    ]
    resources = [
      "arn:aws:sqs:${local.aws_region}:${local.aws_account_id}:collectingpayments-execution-payment-execution-queue-${var.environment}"
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
