# Deployment Role

Deploys everything for this component via GHActions
 - Service to k8s
 - Service execution role

This is a Kora IAM Role - a wrapper around an AWS IAM Role. It is managed outside the scope of the pipeline.

Also in this solution is the bootstrap for the service execution role.

## Deployment Steps

```sh
cd infra/deploymentRole

# Assume a UAT developer role via AWS SSO
aws-sso-login developer-role-uat
bash runTerraform.sh plan uat
bash runTerraform.sh apply uat

# Repeat for your production account
aws-sso-login developer-role-production
bash runTerraform.sh plan production
bash runTerraform.sh apply production
```
