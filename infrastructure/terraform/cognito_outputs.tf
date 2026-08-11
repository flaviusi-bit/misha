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
