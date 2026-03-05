data "aws_caller_identity" "current" {}

locals {
  resource_name_prefix = "cancel-execution-lambda"

  env_to_aws_region = {
    "test" = "ap-southeast-2"
    "uat" = "us-west-2"
    "production" = "us-east-1"
  }
  aws_region = local.env_to_aws_region[var.environment]
  
  env_to_aws_account = {
    "test" = "590184096169"
    "uat" = "471112828694"
    "production" = "211125634578"
  }
  aws_account_id = local.env_to_aws_account[var.environment]
  
  env_to_infra_kms_key_arn = {
    "test" = "arn:aws:kms:ap-southeast-2:333186395126:key/mrk-d056f8a301b2467496ae8ca4cfe14298"
    "uat" = "arn:aws:kms:us-west-2:333186395126:key/mrk-b883401ceb07443e846ee9999581032b"
    "production" = "arn:aws:kms:us-east-1:333186395126:key/mrk-b6707da6297841878502555ec9d1111e"
  }
  infra_kms_key_arn = local.env_to_infra_kms_key_arn[var.environment]
  
  env_to_execution_sqs_kms_key_arn = {
    "test"       = "arn:aws:kms:ap-southeast-2:333186395126:key/mrk-306535ce57e545e1ba601d6047bee055"
    "uat"        = "arn:aws:kms:us-west-2:333186395126:key/mrk-2d2eb954b34d4ecbb9ca03630667b020"
    "production" = "arn:aws:kms:us-east-1:333186395126:key/mrk-a4ae4929c5ca49f3b753589d7473294b"
  }
  execution_sqs_kms_key_arn = local.env_to_execution_sqs_kms_key_arn[var.environment]
  
  env_to_db_secrets_kms_key_arn = {
    "test"       = "arn:aws:kms:ap-southeast-2:333186395126:key/mrk-d2383b3a51f34c9998a86533a954eca0"
    "uat"        = "arn:aws:kms:us-west-2:333186395126:key/mrk-b8ebf1ec60304c63897cdeaa3e98761c"
    "production" = "arn:aws:kms:us-east-1:333186395126:key/mrk-3f409b4773434143ba42dcce583c6157"
  }
  db_secrets_kms_key_arn = local.env_to_db_secrets_kms_key_arn[var.environment]
  env_to_secrets_kms_arn = {
    "test"       = "arn:aws:kms:ap-southeast-2:333186395126:key/mrk-0189429af5434cfeaa2fc09577836f56"
    "uat"        = "arn:aws:kms:us-west-2:333186395126:key/mrk-ce1ccc0db8b74bec90237092efb4931e"
    "production" = "arn:aws:kms:us-east-1:333186395126:key/mrk-0ba3f90baa79430097b54952973bcb5f"
  }
  secrets_kms_arn = local.env_to_secrets_kms_arn[var.environment]

  launch_darkly_global_project_sdk_key = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/cancel-execution-lambda/launch-darkly-config/xero-global-sdk-key-??????"
  identity_client_secret = "arn:aws:secretsmanager:${local.aws_region}:${data.aws_caller_identity.current.account_id}:secret:collecting-payments-execution/cancel-execution-lambda/identity-client-secret-??????"
  common_tags = {
    uuid = "bfifma5ebpfcbq4cb9a5ud"
  }
}
