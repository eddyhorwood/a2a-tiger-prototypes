terraform {
  required_version = "~> 1.8.2"
  required_providers {
    kora = {
      source  = "tf.xero.dev/paas/kora"
      version = "~> 1.7"
    }
  }

  backend "s3" {
    key     = "infra/deploymentRole/terraform.tfstate"
    encrypt = true
  }
}

provider "kora" {}
