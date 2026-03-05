locals {
  bucket_name = split(":::", lookup(kora_resource.cancel-lambda-artifact-bucket.properties, "arn", ""))[1]
}

resource "kora_resource" "cancel-lambda-artifact-bucket" {
  type    = "s3-bucket"
  version = "s3-bucket-1.0"
  # Approvers of this namespace are able to manage the aurora-postgres
  namespace = "cp-payment-execution"

  configuration = jsonencode({
    # The bucket name must be globally unique within AWS. Consider starting with `xero-` to reduce the chance of a name collision with other AWS users.. (string, required)
    name = "${local.resource_name_prefix}-artifacts-${var.environment}"
    # The AWS region of the bucket.. (string, required)
    region = local.aws_region
    # The AWS Account to provision the bucket into.. (string, required)
    account_id = local.aws_account_id
    # The KMS key to use for server-side encryption.. (string, required)
    kms_key = local.infra_kms_key_arn
    # Tags to apply to the bucket. This must include the uuid tag.. (object, required)
    tags = {
      uuid = local.common_tags.uuid
    }
  })
}

resource "aws_s3_object" "cancel_execution_lambda_package" {
  bucket        = local.bucket_name
  key           = local.cancel_execution_lambda_zip_s3
  source        = local.cancel_execution_lambda_zip
  storage_class = "INTELLIGENT_TIERING"

  kms_key_id = local.env_to_infra_kms_key_arn[var.environment]

  depends_on = [kora_resource.cancel-lambda-artifact-bucket, null_resource.build_dotnet_lambda]

  lifecycle {
    replace_triggered_by = [null_resource.build_dotnet_lambda]
  }
}
