resource "kora_resource" "deployment_role" {
  type      = "aws-iam-role"
  version   = "aws-iam-role-1.0"
  namespace = "aws-iam-roles"

  configuration = jsonencode({
    "name" : "deployment-${var.environment}-${var.service_name}",
    "tags" : {
      "uuid" : "BdzazQewZtyQbUfCK7XYxc"
    },
    "account_id" : var.aws_account_id,
    "permissions_boundary" : "arn:aws:iam::${var.aws_account_id}:policy/XeroDeveloperPermissionsBoundary",
    "assume_role_policy" : yamldecode(templatefile("${path.module}/policies/assumeRole.yaml", {
      oidc_role_arn = var.oidc_role_arn
    })),
    "inline_policies" : [
      {
        "name" : "deploy-to-k8s",
        "policy" : yamldecode(templatefile("${path.module}/policies/deployToK8s.yaml", {
          artifactory_password_arn     = var.artifactory_password_arn,
          artifactory_password_kms_arn = var.artifactory_password_kms_arn
        }))
      },
      {
        "name" : "terraform-backend",
        "policy" : yamldecode(templatefile("${path.module}/policies/terraformBackend.yaml", {
          infra_kms_key_arn = var.infra_kms_key_arn,
          aws_account_id    = var.aws_account_id,
          service_name      = var.service_name,
          region            = var.region,
          environment       = var.environment
        }))
      }
    ]
  })
}

resource "kora_namespace_approver_user" "approve" {
  namespace     = var.namespace
  approver_user = "arn:aws:iam::${var.aws_account_id}:role/deployment-${var.environment}-${var.service_name}"
}