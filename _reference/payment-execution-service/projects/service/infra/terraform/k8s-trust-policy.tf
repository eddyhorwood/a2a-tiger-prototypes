data "aws_caller_identity" "current" {}

locals {
  env_to_kubernetes_cluster = {
    "test"       = "ap-southeast-2.k8s-paas-test-rua"
    "uat"        = "us-west-2.k8s-paas-uat-rua"
    "production" = "us-east-1.k8s-paas-prod-rua"
  }

  kubernetes_cluster_to_issuer_id = {
    "ap-southeast-2.k8s-paas-test-rua" = "DDC655B7C38D899EBEB7E42A06873D5F"
    "us-west-2.k8s-paas-uat-rua"       = "1AA2D293D303AC30E9B9481352BE8263"
    "us-east-1.k8s-paas-prod-rua"      = "CA5F7A1B2CA7FC4CA216FF78043E7AA4"
  }

  kubernetes_service_account_name = trim(substr(local.kubernetes_release_name, 0, 63), "-")

  k8s_cluster              = local.env_to_kubernetes_cluster[var.environment]
  k8s_issuer_id            = local.kubernetes_cluster_to_issuer_id[local.k8s_cluster]
  k8s_service_account_name = "system:serviceaccount:${local.kubernetes_namespace}:${local.kubernetes_service_account_name}"
}

data "aws_iam_policy_document" "trust_policy" {
  statement {
    actions = [
      "sts:AssumeRoleWithWebIdentity"
    ]
    principals {
      type = "Federated"
      identifiers = [
        "arn:aws:iam::${data.aws_caller_identity.current.account_id}:oidc-provider/oidc.eks.${local.aws_region}.amazonaws.com/id/${local.k8s_issuer_id}"
      ]
    }
    condition {
      test     = "StringEquals"
      variable = "oidc.eks.${local.aws_region}.amazonaws.com/id/${local.k8s_issuer_id}:sub"
      values = [
        local.k8s_service_account_name
      ]
    }
  }
}
