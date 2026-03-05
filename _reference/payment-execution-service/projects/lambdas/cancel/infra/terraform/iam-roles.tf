resource "kora_resource" "cancel_execution_lambda_exec_role" {
  type      = "aws-iam-role"
  version   = "aws-iam-role-1.0"
  namespace = "aws-iam-roles"

  configuration = jsonencode({
    name                 = "lambda-execution-${var.environment}-${local.resource_name_prefix}"
    description          = "Execution role for Cancel Execution Lambda"
    account_id           = data.aws_caller_identity.current.account_id
    assume_role_policy   = jsondecode(data.aws_iam_policy_document.lambda_assume_role.json),
    permissions_boundary = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:policy/XeroDeveloperPermissionsBoundary",
    admin_roles          = ["arn:aws:iam::${data.aws_caller_identity.current.account_id}:role/deployment-${var.environment}-${local.resource_name_prefix}"],
    inline_policies = [
      {
        "name" : "lambda-execution-policy"
        "policy" : jsondecode(data.aws_iam_policy_document.lambda_execution_policy.json)
      },
      {
        "name" : "process-sqs-message-policy"
        "policy" : jsondecode(data.aws_iam_policy_document.process_sqs_message_policy.json)
      },
      {
        "name" : "read-db-connection-secret-policy"
        "policy" : jsondecode(data.aws_iam_policy_document.read_db_connection_secret_policy.json)
      },
      {
        "name" : "read-launch-darkly-config-secrets-policy",
        "policy" : jsondecode(data.aws_iam_policy_document.read_launch_darkly_config_secrets.json)
      },
      {
        "name" : "read-identity-client-secret-policy",
        "policy" : jsondecode(data.aws_iam_policy_document.read_identity_client_secret_policy.json)
      }
    ],
    tags = {
      uuid = local.common_tags.uuid
    },
  })
}

# This output is used to get the ARN of the lambda execution role since it's managed through the kora_resource module
output "lambda_exec_role_arn" {
  value       = lookup(kora_resource.cancel_execution_lambda_exec_role.properties, "arn", null)
  description = "The ARN of the AWS IAM role for lambda service execution"
}
