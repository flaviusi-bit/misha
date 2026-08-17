# Production hardening is implemented directly on the Terraform resources.
#
# The deployment role policy is temporarily ignored because AWS enforces a
# 10,240-character aggregate limit for inline role policies. The current
# policy is already at that limit, so Terraform cannot safely add another EC2
# action to it during recovery. The apply workflow grants the required VPC
# permission temporarily and removes it again after the deployment.
resource "aws_iam_role_policy" "github_actions_deploy" {
  lifecycle {
    ignore_changes = [policy]
  }
}
