resource "kora_resource" "service_exec_role" {
  type      = "aws-iam-role"
  version   = "aws-iam-role-1.0"
  namespace = "aws-iam-roles"

  configuration = jsonencode({
    "name" : "service-execution-${var.environment}-${local.resource_name_prefix}",
    "tags" : {
      "uuid" : "334db7HzPYMT119B9go9X9"
    },
    "account_id" : data.aws_caller_identity.current.account_id,
    "permissions_boundary" : "arn:aws:iam::${data.aws_caller_identity.current.account_id}:policy/XeroDeveloperPermissionsBoundary",
    "assume_role_policy" : jsondecode(data.aws_iam_policy_document.trust_policy.json),
    "admin_roles" : ["arn:aws:iam::${data.aws_caller_identity.current.account_id}:role/deployment-${var.environment}-${local.resource_name_prefix}"],
    "inline_policies" : [
      {
        "name" : "read-new-relic-license-key-policy"
        "policy" : jsondecode(data.aws_iam_policy_document.read_new_relic_license_key_policy.json)
      },
      {
        "name" : "read-identity-client-secret-policy"
        "policy" : jsondecode(data.aws_iam_policy_document.read_identity_client_secret_policy.json)
      },
      {
        "name" : "read-db-connection-secret-policy"
        "policy" : jsondecode(data.aws_iam_policy_document.read_db_connection_secret_policy.json)
      },
      {
        "name" : "read_parameter_store_policy",
        "policy" : jsondecode(data.aws_iam_policy_document.read_parameter_store_policy.json)
      },
      {
        "name" : "send_message_sqs_policy",
        "policy" : jsondecode(data.aws_iam_policy_document.send_message_sqs_policy.json)
      },
      {
        "name" : "read-launch-darkly-config-secrets-policy",
        "policy" : jsondecode(data.aws_iam_policy_document.read_launch_darkly_config_secrets.json)
      }
    ]
  })
}
