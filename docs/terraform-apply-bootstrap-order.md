# Terraform Apply bootstrap order

The AWS Terraform Apply workflow bootstraps the IAM policies required by the deployment role before running the first full Terraform plan. The workflow then waits for IAM propagation, re-assumes the GitHub OIDC role, and runs the full plan/apply with fresh credentials.
