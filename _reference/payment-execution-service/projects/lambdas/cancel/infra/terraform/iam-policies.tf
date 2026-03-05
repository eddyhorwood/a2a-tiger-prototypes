data "aws_iam_policy_document" "lambda_assume_role" {
  statement {
    effect  = "Allow"
    actions = ["sts:AssumeRole"]
    principals {
      type        = "Service"
      identifiers = ["lambda.amazonaws.com"]
    }
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
      local.secrets_kms_arn
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
      local.identity_client_secret,
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
      local.secrets_kms_arn
    ]
  }
}

data "aws_iam_policy_document" "lambda_execution_policy" {
  # checkov:skip=CKV_AWS_111: "DescribeNetworkInterfaces requires '*' resource as per AWS documentation for Lambda VPC access"
  # checkov:skip=CKV_AWS_356: "DescribeNetworkInterfaces requires '*' resource as per AWS documentation for Lambda VPC access"
  statement {
    effect  = "Allow"
    actions = [
      "ec2:CreateNetworkInterface",
      "ec2:DescribeNetworkInterfaces",
      "ec2:DeleteNetworkInterface",
      "ec2:DescribeSubnets",
      "ec2:DescribeSecurityGroups",
      "ec2:DescribeVpcs"
    ]
    resources = [
      "*"
    ]
  }

  statement {
    effect  = "Allow"
    actions = [
      "logs:CreateLogGroup",
      "logs:CreateLogStream",
      "logs:PutLogEvents",
      "logs:DescribeLogGroups",
      "logs:DescribeLogStreams",
      "logs:PutSubscriptionFilter",
      "logs:DescribeSubscriptionFilters"
    ]
    resources = ["arn:aws:logs:*:*:*"]
  }
}
data "aws_iam_policy_document" "process_sqs_message_policy" {
  statement {
    effect  = "Allow"
    actions = [
      "sqs:ReceiveMessage",
      "sqs:DeleteMessage",
      "sqs:GetQueueAttributes"
    ]
    resources = [
      "arn:aws:sqs:${local.aws_region}:${local.aws_account_id}:collectingpayments-execution-cancel-execution-queue-${var.environment}"
    ]
  }

  statement {
    effect  = "Allow"
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

# Policy to read database connection string from Secrets Manager
data "aws_iam_policy_document" "read_db_connection_secret_policy" {
  statement {
    effect = "Allow"
    actions = [
      "secretsmanager:GetSecretValue"
    ]
    resources = [
      "arn:aws:secretsmanager:${local.aws_region}:${local.aws_account_id}:secret:collecting-payments-execution/collecting-payments-execution-payment-execution-database/payment-execution-db/connection-string-??????"
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
      local.db_secrets_kms_key_arn,
    ]
  }
}
