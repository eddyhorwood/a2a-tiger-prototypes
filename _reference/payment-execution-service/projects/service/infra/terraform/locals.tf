locals {
  # If you update this, ensure you also update 'service-name-kebab-case' in '.github/workflows/workflow-get-config.yaml'.
  kubernetes_release_name = "payment-execution-service"

  # If you update this, ensure you also update 'service-name-kebab-case' in '.github/workflows/workflow-get-config.yaml'.
  resource_name_prefix = "payment-execution-service"

  # If you update this, ensure you also update 'kubernetes-namespace' in '.github/workflows/workflow-get-config.yaml'.
  kubernetes_namespace = "cp-payment-execution"

  env_to_aws_region = {
    "test"       = "ap-southeast-2"
    "uat"        = "us-west-2"
    "production" = "us-east-1"
  }

  aws_region = local.env_to_aws_region[var.environment]

  env_to_execution_sqs_kms_key_arn = {
    "test"       = "arn:aws:kms:ap-southeast-2:333186395126:key/mrk-306535ce57e545e1ba601d6047bee055"
    "uat"        = "arn:aws:kms:us-west-2:333186395126:key/mrk-2d2eb954b34d4ecbb9ca03630667b020"
    "production" = "arn:aws:kms:us-east-1:333186395126:key/mrk-a4ae4929c5ca49f3b753589d7473294b"
  }

  execution_sqs_kms_key_arn = local.env_to_execution_sqs_kms_key_arn[var.environment]
}
