# Terraform Backend

A CloudFormation stack including:
 - S3 Bucket for storage of Terraform State

This Terraform backend holds state for:
 - `infra/deploymentRole/` terraform resources
 - `infra/terraform/` terraform resources

## Deployment Steps

```sh
cd infra/terraformBackend

# Validate the CloudFormation stack
bash validateBackend.sh

# Assume a Test developer role via AWS SSO
aws-sso-login developer-role-test
bash deployBackend.sh test

# Repeat for your UAT account
aws-sso-login developer-role-uat
bash deployBackend.sh uat

# Repeat for your production account
aws-sso-login developer-role-production
bash deployBackend.sh production
```
