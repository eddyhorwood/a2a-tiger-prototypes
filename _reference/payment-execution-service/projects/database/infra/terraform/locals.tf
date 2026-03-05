data "aws_caller_identity" "current" {}

locals {
  env_to_aws_region = {
    "test"       = "ap-southeast-2"
    "uat"        = "us-west-2"
    "production" = "us-east-1"
  }

  env_to_aws_vpc_id = {
    "test"       = "vpc-06b00336825efbcb9"
    "uat"        = "vpc-0eed8ff8e49fb3635"
    "production" = "vpc-0c851a4a090a5bb0c"
  }

  env_to_min_capacity = {
    "test"       = 0.5
    "uat"        = 4
    "production" = 4
  }

  env_to_max_capacity = {
    "test"       = 1
    "uat"        = 8
    "production" = 8
  }

  aws_region     = local.env_to_aws_region[var.environment]
  aws_account_id = data.aws_caller_identity.current.account_id
  aws_vpc_id     = local.env_to_aws_vpc_id[var.environment]
  namespace      = "cp-payment-execution"
  cortex_tag     = "svZ2goYQnrGkthqfsaUeBb"
}

