output "vpc_id" {
  value = aws_vpc.this.id
}

output "private_subnet_ids" {
  value = [for subnet in aws_subnet.private : subnet.id]
}

output "public_subnet_ids" {
  value = [for subnet in aws_subnet.public : subnet.id]
}

output "documents_bucket_name" {
  value = aws_s3_bucket.documents.bucket
}

output "application_events_queue_url" {
  value = aws_sqs_queue.application_events.url
}

output "application_events_dlq_url" {
  value = aws_sqs_queue.application_events_dlq.url
}

output "ecs_cluster_name" {
  value = aws_ecs_cluster.this.name
}

output "ecs_api_task_definition" {
  value = aws_ecs_task_definition.api.family
}

output "ecs_api_service_name" {
  value = aws_ecs_service.api.name
}

output "ecs_worker_task_definition" {
  value = aws_ecs_task_definition.worker.family
}

output "ecs_worker_service_name" {
  value = aws_ecs_service.worker.name
}

output "api_load_balancer_dns_name" {
  value = aws_lb.api.dns_name
}

output "api_url" {
  value = local.tls_enabled ? "https://${var.domain_name}" : "http://${aws_lb.api.dns_name}"
}

output "acm_certificate_arn" {
  value = local.tls_enabled ? aws_acm_certificate.api[0].arn : null
}

output "nat_gateway_id" {
  value = aws_nat_gateway.this.id
}

output "rds_endpoint" {
  value = aws_db_instance.postgres.address
}

output "application_secret_arn" {
  value = aws_secretsmanager_secret.application.arn
}

output "github_actions_deploy_role_arn" {
  description = "IAM role ARN assumed by GitHub Actions through OIDC."
  value       = aws_iam_role.github_actions_deploy.arn
}

output "ecr_repository_url" {
  description = "ECR repository URL for the MISHA API image."
  value       = aws_ecr_repository.api.repository_url
}

output "worker_ecr_repository_url" {
  description = "ECR repository URL for the MISHA worker image."
  value       = aws_ecr_repository.worker.repository_url
}

output "cognito_user_pool_id" {
  description = "MISHA Cognito User Pool ID."
  value       = aws_cognito_user_pool.misha.id
}

output "cognito_user_pool_issuer" {
  description = "OIDC issuer URL used by the MISHA API JWT bearer configuration."
  value       = "https://cognito-idp.${var.aws_region}.amazonaws.com/${aws_cognito_user_pool.misha.id}"
}

output "cognito_api_audience" {
  description = "JWT audience/resource-server identifier for the MISHA API."
  value       = aws_cognito_resource_server.api.identifier
}

output "cognito_app_client_id" {
  description = "Public Cognito app client ID for the MISHA web application."
  value       = aws_cognito_user_pool_client.misha.id
}

output "cognito_domain" {
  description = "Cognito hosted UI domain prefix."
  value       = aws_cognito_user_pool_domain.misha.domain
}
