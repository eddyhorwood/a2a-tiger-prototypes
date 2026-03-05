terraform {
  required_version = "~> 1.10.4"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.83"
    }
  }

  backend "local" {
    path = "./payment-execution-execution-worker-local.tfstate"
  }
}

locals {
  localstackEndpoint = "http://localstack:4566"
}


provider "aws" {
  access_key = "mock_access_key"
  region     = "ap-southeast-2"
  # checkov:skip=CKV_SECRET_6:Skipping rule for secrets in terraform CKV_SECRET_6
  secret_key                  = "mock_secret_key"
  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true

  endpoints {
    sqs  = local.localstackEndpoint
    iam  = local.localstackEndpoint
    logs = local.localstackEndpoint
    kms  = local.localstackEndpoint
  }
}

module "payment-execution-queue" {
  source                    = "../../worker/infra/terraform/modules/payment-execution-sqs"
  environment               = "test"
  execution_sqs_kms_key_arn = "*"
  aws_account_id            = ""
}

module "cancel-execution-queue" {
  source                    = "../../lambdas/cancel/infra/terraform/cancel-execution-sqs"
  environment               = "test"
  execution_sqs_kms_key_arn = "*"
}
