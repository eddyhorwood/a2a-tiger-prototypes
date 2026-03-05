locals {
  branches = {
    pull_requests = "refs/pull/*/merge"
    main          = "refs/heads/main"
  }
}

resource "kora_resource" "deployment_role" {
  type      = "aws-iam-role"
  version   = "aws-iam-role-1.0"
  namespace = "aws-iam-roles"

  configuration = jsonencode({
    "name" : "deployment-${var.environment}-${var.deployment_role_name}",
    "tags" : {
      "uuid" : "334db7HzPYMT119B9go9X9"
    },
    "account_id" : var.aws_account_id,
    "permissions_boundary" : "arn:aws:iam::${var.aws_account_id}:policy/XeroDeveloperPermissionsBoundary",
    "assume_role_policy" : yamldecode(templatefile("${path.module}/policies/assumeRole.yaml", {
      oidc_role_arn      = var.oidc_role_arn,
      request_tag_branch = var.environment == "test" ? [local.branches.pull_requests, local.branches.main] : [local.branches.main]
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
        "name" : "service-terraform-backend",
        "policy" : yamldecode(templatefile("${path.module}/policies/terraformBackend.yaml", {
          infra_kms_key_arn = var.infra_kms_key_arn,
          aws_account_id    = var.aws_account_id,
          service_name      = var.service_name,
          region            = var.region,
          environment       = var.environment
        }))
      },
      {
        "name" : "database-terraform-backend",
        "policy" : yamldecode(templatefile("${path.module}/policies/terraformBackend.yaml", {
          infra_kms_key_arn = var.infra_kms_key_arn,
          aws_account_id    = var.aws_account_id,
          service_name      = var.database_name,
          region            = var.region,
          environment       = var.environment
        }))
      },
      {
        "name" : "database-connection-string-access",
        "policy" : yamldecode(templatefile("${path.module}/policies/deploymentSecrets.yaml", {
          aws_account_id                      = var.aws_account_id,
          region                              = var.region,
          connection_string_secret_name       = var.database_connection_string_secret_name
          retool_user_password_secret_name    = var.retool_password_secret_name
          schema_manager_password_secret_name = var.schema_manager_password_secret_name
          db_secret_kms_key_arn               = var.db_secret_kms_key_arn

        }))
      },
      {
        "name" : "worker-terraform-backend",
        "policy" : yamldecode(templatefile("${path.module}/policies/terraformBackend.yaml", {
          infra_kms_key_arn = var.infra_kms_key_arn,
          aws_account_id    = var.aws_account_id,
          service_name      = var.worker_name,
          region            = var.region,
          environment       = var.environment
        }))
      },
      {
        "name" : "create-queue",
        "policy" : yamldecode(templatefile("${path.module}/policies/sqsRole.yaml", {
          aws_account_id = var.aws_account_id,
          queue_name     = var.queue_name,
          dlq_name       = var.dlq_name,
          region         = var.region,
          environment    = var.environment
        }))
      },
      {
        "name" : "create-cancel-queue",
        "policy" : yamldecode(templatefile("${path.module}/policies/sqsRole.yaml", {
          aws_account_id = var.aws_account_id,
          queue_name     = var.cancel_queue_name,
          dlq_name       = var.cancel_dlq_name,
          region         = var.region,
          environment    = var.environment
        }))
      },
      {
        "name" : "cloudwatch-dlq-alerts",
        "policy" : yamldecode(templatefile("${path.module}/policies/cloudWatchDlqAlerts.yaml", {
          aws_account_id                            = var.aws_account_id,
          region                                    = var.region,
          environment                               = var.environment,
          cloudwatch_secret_kms_arn                 = var.infra_kms_key_arn,
          pagerduty_cloudwatch_endpoint_secret_name = var.pagerduty_cloudwatch_endpoint_secret_name
        }))
      },
      {
        "name" : "cancel-lambda-terraform-backend",
        "policy" : yamldecode(templatefile("${path.module}/policies/terraformBackend.yaml", {
          infra_kms_key_arn = var.infra_kms_key_arn,
          aws_account_id    = var.aws_account_id,
          service_name      = var.cancel_lambda_name,
          region            = var.region,
          environment       = var.environment
        }))
      },
      {
        "name" : "publish-cancel-lambda",
        "policy" : yamldecode(templatefile("${path.module}/policies/publishCancelLambda.yaml", {
          service_name        = var.cancel_lambda_name,
          environment         = var.environment,
          aws_account_id      = var.aws_account_id,
          region              = var.region,
          default_kms_key_arn = var.default_kms_key_arn,
        }))
      },
    ]
  })
}

resource "kora_namespace_approver_user" "approve" {
  namespace     = var.namespace
  approver_user = "arn:aws:iam::${var.aws_account_id}:role/deployment-${var.environment}-${var.deployment_role_name}"
}
