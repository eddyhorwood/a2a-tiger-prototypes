# Terraform Backend

A CloudFormation stack including:
 - S3 Bucket for storage of Terraform State
 - DynamoDB Table for Terraform State Locking

This Terraform backend holds state for:
 - `infra/deploymentRole/` terraform resources
 - `infra/terraform/` terraform resources

## Deployment Steps

```sh
cd infra/terraformBackend

# Validate the CloudFormation stack
bash validateBackend.sh

# Assume a UAT developer role via AWS SSO
aws-sso-login developer-role-uat
bash deployBackend.sh apply uat

# Repeat for your production account
aws-sso-login developer-role-production
bash deployBackend.sh apply prod
```
