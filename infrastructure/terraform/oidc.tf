data "tls_certificate" "github_actions" {
  url = "https://token.actions.githubusercontent.com/.well-known/openid-configuration"
}

resource "aws_iam_openid_connect_provider" "github_actions" {
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = [data.tls_certificate.github_actions.certificates[0].sha1_fingerprint]
}

resource "aws_ecr_repository" "api" {
  name                 = "${local.name}-api"
  image_tag_mutability = "IMMUTABLE"
  image_scanning_configuration { scan_on_push = true }
  encryption_configuration { encryption_type = "AES256" }
}

resource "aws_ecr_lifecycle_policy" "api" {
  repository = aws_ecr_repository.api.name

  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Keep the latest 20 images"
      selection = {
        tagStatus   = "any"
        countType   = "imageCountMoreThan"
        countNumber = 20
      }
      action = { type = "expire" }
    }]
  })
}

resource "aws_iam_role" "github_actions_deploy" {
  name = "${local.name}-github-actions-deploy"
  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Principal = { Federated = aws_iam_openid_connect_provider.github_actions.arn }
      Action = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringEquals = { "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com" }
        StringLike = { "token.actions.githubusercontent.com:sub" = "repo:flaviusi-bit/misha:environment:aws-dev" }
      }
    }]
  })
}

resource "aws_iam_role_policy" "github_actions_deploy" {
  name = "${local.name}-github-actions-deploy"
  role = aws_iam_role.github_actions_deploy.id
  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      # ECR: image publishing and repository/lifecycle inspection.
      { Effect = "Allow", Action = [
        "ecr:GetAuthorizationToken"
      ], Resource = "*" },
      { Effect = "Allow", Action = [
        "ecr:BatchCheckLayerAvailability",
        "ecr:CompleteLayerUpload",
        "ecr:DescribeRepositories",
        "ecr:GetLifecyclePolicy",
        "ecr:InitiateLayerUpload",
        "ecr:ListTagsForResource",
        "ecr:PutImage",
        "ecr:PutLifecyclePolicy",
        "ecr:DeleteLifecyclePolicy",
        "ecr:UploadLayerPart"
      ], Resource = aws_ecr_repository.api.arn },

      # ECS: Terraform manages the cluster, task definition and service.
      { Effect = "Allow", Action = [
        "ecs:DescribeClusters",
        "ecs:DescribeServices",
        "ecs:DescribeTaskDefinition",
        "ecs:RegisterTaskDefinition",
        "ecs:DeregisterTaskDefinition",
        "ecs:CreateCluster",
        "ecs:DeleteCluster",
        "ecs:UpdateClusterSettings",
        "ecs:TagResource",
        "ecs:UntagResource",
        "ecs:CreateService",
        "ecs:DeleteService",
        "ecs:UpdateService"
      ], Resource = "*" },

      # EC2/VPC/NAT/security groups. EC2 Describe APIs require Resource="*".
      { Effect = "Allow", Action = [
        "ec2:Describe*",
        "ec2:CreateVpc",
        "ec2:DeleteVpc",
        "ec2:ModifyVpcAttribute",
        "ec2:CreateInternetGateway",
        "ec2:DeleteInternetGateway",
        "ec2:AttachInternetGateway",
        "ec2:DetachInternetGateway",
        "ec2:CreateSubnet",
        "ec2:DeleteSubnet",
        "ec2:ModifySubnetAttribute",
        "ec2:CreateRouteTable",
        "ec2:DeleteRouteTable",
        "ec2:CreateRoute",
        "ec2:ReplaceRoute",
        "ec2:DeleteRoute",
        "ec2:AssociateRouteTable",
        "ec2:DisassociateRouteTable",
        "ec2:AllocateAddress",
        "ec2:ReleaseAddress",
        "ec2:CreateNatGateway",
        "ec2:DeleteNatGateway",
        "ec2:CreateSecurityGroup",
        "ec2:DeleteSecurityGroup",
        "ec2:AuthorizeSecurityGroupIngress",
        "ec2:AuthorizeSecurityGroupEgress",
        "ec2:RevokeSecurityGroupIngress",
        "ec2:RevokeSecurityGroupEgress",
        "ec2:CreateTags",
        "ec2:DeleteTags"
      ], Resource = "*" },

      # S3 Terraform state backend.
      { Effect = "Allow", Action = [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject"
      ], Resource = [
        "arn:aws:s3:::misha-terraform-state/misha/dev/terraform.tfstate",
        "arn:aws:s3:::misha-terraform-state/misha/dev/terraform.tfstate.tflock"
      ] },
      { Effect = "Allow", Action = ["s3:ListBucket"], Resource = "arn:aws:s3:::misha-terraform-state", Condition = { StringLike = { "s3:prefix" = ["misha/dev/*"] } } },

      # S3 documents bucket managed by Terraform.
      { Effect = "Allow", Action = [
        "s3:CreateBucket",
        "s3:DeleteBucket",
        "s3:GetBucket*",
        "s3:ListBucket",
        "s3:PutBucket*",
        "s3:DeleteBucketPolicy"
      ], Resource = "arn:aws:s3:::misha-dev-documents-*" },

      # SQS application queues.
      { Effect = "Allow", Action = [
        "sqs:GetQueueAttributes",
        "sqs:GetQueueUrl",
        "sqs:ListQueueTags",
        "sqs:CreateQueue",
        "sqs:DeleteQueue",
        "sqs:SetQueueAttributes",
        "sqs:TagQueue",
        "sqs:UntagQueue"
      ], Resource = [
        "arn:aws:sqs:eu-central-1:576984879588:misha-dev-application-events",
        "arn:aws:sqs:eu-central-1:576984879588:misha-dev-application-events-dlq"
      ] },

      # CloudWatch Logs API log group.
      { Effect = "Allow", Action = [
        "logs:DescribeLogGroups",
        "logs:ListTagsForResource",
        "logs:CreateLogGroup",
        "logs:DeleteLogGroup",
        "logs:PutRetentionPolicy",
        "logs:DeleteRetentionPolicy",
        "logs:TagResource",
        "logs:UntagResource"
      ], Resource = "arn:aws:logs:eu-central-1:576984879588:log-group:/ecs/misha-dev/*" },

      # RDS PostgreSQL instance and subnet group.
      { Effect = "Allow", Action = [
        "rds:Describe*",
        "rds:CreateDBSubnetGroup",
        "rds:DeleteDBSubnetGroup",
        "rds:ModifyDBSubnetGroup",
        "rds:CreateDBInstance",
        "rds:DeleteDBInstance",
        "rds:ModifyDBInstance",
        "rds:AddTagsToResource",
        "rds:RemoveTagsFromResource"
      ], Resource = "*" },

      # Secrets Manager application secret and RDS-managed master secret metadata.
      { Effect = "Allow", Action = [
        "secretsmanager:DescribeSecret",
        "secretsmanager:GetResourcePolicy",
        "secretsmanager:CreateSecret",
        "secretsmanager:DeleteSecret",
        "secretsmanager:UpdateSecret",
        "secretsmanager:PutResourcePolicy",
        "secretsmanager:DeleteResourcePolicy",
        "secretsmanager:TagResource",
        "secretsmanager:UntagResource"
      ], Resource = "arn:aws:secretsmanager:eu-central-1:576984879588:secret:misha-dev/*" },

      # ACM and Route53 are only used when domain_name + route53_zone_id are configured.
      { Effect = "Allow", Action = [
        "acm:DescribeCertificate",
        "acm:RequestCertificate",
        "acm:DeleteCertificate",
        "acm:ListCertificates",
        "acm:AddTagsToCertificate",
        "acm:RemoveTagsFromCertificate"
      ], Resource = "*" },
      { Effect = "Allow", Action = [
        "route53:GetHostedZone",
        "route53:ListResourceRecordSets",
        "route53:ChangeResourceRecordSets",
        "route53:ListHostedZonesByName"
      ], Resource = "*" },

      # IAM role management for the ECS roles and GitHub deployment role.
      { Effect = "Allow", Action = [
        "iam:GetRole",
        "iam:CreateRole",
        "iam:DeleteRole",
        "iam:UpdateAssumeRolePolicy",
        "iam:PutRolePolicy",
        "iam:DeleteRolePolicy",
        "iam:GetRolePolicy",
        "iam:ListRolePolicies",
        "iam:AttachRolePolicy",
        "iam:DetachRolePolicy",
        "iam:ListAttachedRolePolicies",
        "iam:TagRole",
        "iam:UntagRole"
      ], Resource = [
        aws_iam_role.github_actions_deploy.arn,
        aws_iam_role.ecs_execution.arn,
        aws_iam_role.ecs_task.arn
      ] },
      # OIDC provider creation is account-scoped; read/update/delete are provider-scoped.
      { Effect = "Allow", Action = ["iam:CreateOpenIDConnectProvider"], Resource = "*" },
      { Effect = "Allow", Action = [
        "iam:GetOpenIDConnectProvider",
        "iam:DeleteOpenIDConnectProvider",
        "iam:AddClientIDToOpenIDConnectProvider",
        "iam:RemoveClientIDFromOpenIDConnectProvider",
        "iam:UpdateOpenIDConnectProviderThumbprint"
      ], Resource = aws_iam_openid_connect_provider.github_actions.arn },
      { Effect = "Allow", Action = [
        "iam:PassRole"
      ], Resource = [aws_iam_role.ecs_execution.arn, aws_iam_role.ecs_task.arn] }
    ]
  })
}
