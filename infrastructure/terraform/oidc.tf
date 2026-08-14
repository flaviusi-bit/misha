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
      Effect    = "Allow"
      Principal = { Federated = aws_iam_openid_connect_provider.github_actions.arn }
      Action    = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringEquals = {
          "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
          "token.actions.githubusercontent.com:sub" = "repo:flaviusi-bit@314439510/misha@1327835803:environment:aws-dev"
        }
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
      { Effect = "Allow", Action = ["ecr:GetAuthorizationToken"], Resource = "*" },
      { Effect = "Allow", Action = [
        "ecr:BatchCheckLayerAvailability",
        "ecr:CompleteLayerUpload",
        "ecr:CreateRepository",
        "ecr:DeleteRepository",
        "ecr:DeleteLifecyclePolicy",
        "ecr:DescribeRepositories",
        "ecr:GetLifecyclePolicy",
        "ecr:InitiateLayerUpload",
        "ecr:ListTagsForResource",
        "ecr:PutImage",
        "ecr:PutImageScanningConfiguration",
        "ecr:PutImageTagMutability",
        "ecr:PutLifecyclePolicy",
        "ecr:TagResource",
        "ecr:UntagResource",
        "ecr:UploadLayerPart"
      ], Resource = [aws_ecr_repository.api.arn, aws_ecr_repository.worker.arn] },
      { Effect = "Allow", Action = [
        "ecs:DescribeClusters",
        "ecs:DescribeServices",
        "ecs:DescribeTaskDefinition",
        "ecs:DescribeTasks",
        "ecs:ListTasks",
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
      { Effect = "Allow", Action = [
        "cognito-idp:ListUserPools",
        "cognito-idp:DescribeResourceServer"
      ], Resource = "*" },
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
        "ec2:ModifySecurityGroupRules",
        "ec2:AuthorizeSecurityGroupIngress",
        "ec2:AuthorizeSecurityGroupEgress",
        "ec2:RevokeSecurityGroupIngress",
        "ec2:RevokeSecurityGroupEgress",
        "ec2:CreateTags",
        "ec2:DeleteTags"
      ], Resource = "*" },
      { Effect = "Allow", Action = [
        "s3:GetObject",
        "s3:PutObject",
        "s3:DeleteObject"
        ], Resource = [
        "arn:aws:s3:::misha-terraform-state/misha/dev/terraform.tfstate",
        "arn:aws:s3:::misha-terraform-state/misha/dev/terraform.tfstate.tflock"
      ] },
      { Effect = "Allow", Action = ["s3:ListBucket"], Resource = "arn:aws:s3:::misha-terraform-state", Condition = { StringLike = { "s3:prefix" = ["misha/dev/*"] } } },
      { Effect = "Allow", Action = [
        "s3:CreateBucket",
        "s3:DeleteBucket",
        "s3:GetBucket*",
        "s3:GetAccelerateConfiguration",
        "s3:GetLifecycleConfiguration",
        "s3:GetReplicationConfiguration",
        "s3:GetEncryptionConfiguration",
        "s3:ListBucket",
        "s3:PutBucket*",
        "s3:DeleteBucketPolicy"
      ], Resource = "arn:aws:s3:::misha-dev-documents-*" },
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
      { Effect = "Allow", Action = ["logs:DescribeLogGroups"], Resource = "*" },
      { Effect = "Allow", Action = [
        "logs:ListTagsForResource",
        "logs:CreateLogGroup",
        "logs:DeleteLogGroup",
        "logs:PutRetentionPolicy",
        "logs:DeleteRetentionPolicy",
        "logs:TagResource",
        "logs:UntagResource"
      ], Resource = [
        "arn:aws:logs:eu-central-1:576984879588:log-group:/ecs/misha-dev/api",
        "arn:aws:logs:eu-central-1:576984879588:log-group:/ecs/misha-dev/worker"
      ] },
      { Effect = "Allow", Action = [
        "rds:Describe*",
        "rds:ListTagsForResource",
        "rds:CreateDBSubnetGroup",
        "rds:DeleteDBSubnetGroup",
        "rds:ModifyDBSubnetGroup",
        "rds:CreateDBInstance",
        "rds:DeleteDBInstance",
        "rds:ModifyDBInstance",
        "rds:AddTagsToResource",
        "rds:RemoveTagsFromResource"
      ], Resource = "*" },
      { Effect = "Allow", Action = [
        "secretsmanager:DescribeSecret",
        "secretsmanager:GetResourcePolicy",
        "secretsmanager:GetSecretValue",
        "secretsmanager:PutSecretValue",
        "secretsmanager:CreateSecret",
        "secretsmanager:DeleteSecret",
        "secretsmanager:UpdateSecret",
        "secretsmanager:PutResourcePolicy",
        "secretsmanager:DeleteResourcePolicy",
        "secretsmanager:TagResource",
        "secretsmanager:UntagResource"
      ], Resource = "arn:aws:secretsmanager:eu-central-1:576984879588:secret:misha-dev/*" },
      { Effect = "Allow", Action = [
        "acm:DescribeCertificate",
        "acm:RequestCertificate",
        "acm:DeleteCertificate",
        "acm:ListCertificates",
        "acm:ListTagsForCertificate",
        "acm:AddTagsToCertificate",
        "acm:RemoveTagsFromCertificate"
      ], Resource = "*" },
      { Effect = "Allow", Action = [
        "route53:GetHostedZone",
        "route53:ListResourceRecordSets",
        "route53:ChangeResourceRecordSets",
        "route53:ListHostedZonesByName",
        "route53:GetChange"
      ], Resource = "*" },
      { Effect = "Allow", Action = [
        "backup:CreateBackupVault",
        "backup:DeleteBackupVault",
        "backup:DescribeBackupVault",
        "backup:ListBackupVaults",
        "backup:TagResource",
        "backup:UntagResource",
        "backup:CreateBackupPlan",
        "backup:DeleteBackupPlan",
        "backup:GetBackupPlan",
        "backup:ListBackupPlans",
        "backup:UpdateBackupPlan",
        "backup:CreateBackupSelection",
        "backup:DeleteBackupSelection",
        "backup:GetBackupSelection",
        "backup:ListBackupSelections",
        "backup:ListTags",
        "backup:PutBackupVaultAccessPolicy",
        "backup:DeleteBackupVaultAccessPolicy",
        "backup:GetBackupVaultAccessPolicy"
      ], Resource = "*" },
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
        aws_iam_role.ecs_task.arn,
        aws_iam_role.ecs_worker_task.arn,
        "arn:aws:iam::576984879588:role/${local.name}-backup"
      ] },
      { Effect = "Allow", Action = ["iam:CreateOpenIDConnectProvider"], Resource = "*" },
      { Effect = "Allow", Action = [
        "iam:GetOpenIDConnectProvider",
        "iam:DeleteOpenIDConnectProvider",
        "iam:AddClientIDToOpenIDConnectProvider",
        "iam:RemoveClientIDFromOpenIDConnectProvider",
        "iam:UpdateOpenIDConnectProviderThumbprint"
      ], Resource = aws_iam_openid_connect_provider.github_actions.arn },
      { Effect = "Allow", Action = ["iam:PassRole"], Resource = [aws_iam_role.ecs_execution.arn, aws_iam_role.ecs_task.arn, aws_iam_role.ecs_worker_task.arn, "arn:aws:iam::576984879588:role/${local.name}-backup"] },
      { Effect = "Allow", Action = [
        "elasticloadbalancing:Describe*",
        "elasticloadbalancing:CreateLoadBalancer",
        "elasticloadbalancing:DeleteLoadBalancer",
        "elasticloadbalancing:ModifyLoadBalancerAttributes",
        "elasticloadbalancing:SetSecurityGroups",
        "elasticloadbalancing:AddTags",
        "elasticloadbalancing:RemoveTags",
        "elasticloadbalancing:CreateTargetGroup",
        "elasticloadbalancing:DeleteTargetGroup",
        "elasticloadbalancing:ModifyTargetGroup",
        "elasticloadbalancing:ModifyTargetGroupAttributes",
        "elasticloadbalancing:RegisterTargets",
        "elasticloadbalancing:DeregisterTargets",
        "elasticloadbalancing:CreateListener",
        "elasticloadbalancing:DeleteListener",
        "elasticloadbalancing:ModifyListener"
      ], Resource = "*" },
      { Effect = "Allow", Action = [
        "cloudwatch:DescribeAlarms",
        "cloudwatch:PutMetricAlarm",
        "cloudwatch:DeleteAlarms",
        "cloudwatch:TagResource",
        "cloudwatch:UntagResource"
      ], Resource = "arn:aws:cloudwatch:eu-central-1:576984879588:alarm:${local.name}-*" }
    ]
  })
}
