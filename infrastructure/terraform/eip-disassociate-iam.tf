# Terraform needs ec2:DisassociateAddress when releasing an EIP that is
# still associated with a resource after its NAT gateway is removed.
resource "aws_iam_role_policy" "github_actions_eip_cleanup" {
  name = "${local.name}-github-actions-eip-cleanup"
  role = aws_iam_role.github_actions_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = [
        "ec2:DisassociateAddress"
      ]
      Resource = "*"
    }]
  })
}
