variable "aws_region" {
  type        = string
  description = "AWS region for the MISHA environment."
  default     = "eu-central-1"
}

variable "environment" {
  type        = string
  description = "Deployment environment name."
  default     = "dev"
}

variable "vpc_cidr" {
  type        = string
  description = "CIDR block for the application VPC."
  default     = "10.40.0.0/16"
}

variable "availability_zones" {
  type        = list(string)
  description = "Availability zones used by the environment."
  default     = ["eu-central-1a", "eu-central-1b"]
}

variable "db_name" {
  type    = string
  default = "misha"
}

variable "db_username" {
  type    = string
  default = "misha"
}

variable "db_instance_class" {
  type    = string
  default = "db.t4g.micro"
}

variable "ecs_cpu" {
  type    = number
  default = 512
}

variable "ecs_memory" {
  type    = number
  default = 1024
}

variable "ecs_desired_count" {
  type        = number
  description = "Desired number of ECS API tasks."
  default     = 1
}

variable "ecs_worker_cpu" {
  type        = number
  description = "CPU units allocated to each ECS worker task."
  default     = 256
}

variable "ecs_worker_memory" {
  type        = number
  description = "Memory in MiB allocated to each ECS worker task."
  default     = 512
}

variable "ecs_worker_desired_count" {
  type        = number
  description = "Desired number of ECS worker tasks."
  default     = 1
}

variable "container_image" {
  type        = string
  description = "ECR image URI for the MISHA API."
  default     = ""
}

variable "worker_container_image" {
  type        = string
  description = "ECR image URI for the MISHA worker."
  default     = ""
}

variable "domain_name" {
  type        = string
  description = "Public API DNS name. Leave empty to keep the HTTP-only development listener."
  default     = ""
}

variable "route53_zone_id" {
  type        = string
  description = "Route53 hosted zone ID for the API domain. Leave empty to keep the HTTP-only development listener."
  default     = ""
}

variable "enable_deletion_protection" {
  type        = bool
  description = "Protect production databases from accidental deletion."
  default     = false
}

variable "final_snapshot_identifier" {
  type        = string
  description = "Final RDS snapshot identifier required for production database deletion."
  default     = ""
}

variable "cognito_callback_urls" {
  type        = list(string)
  description = "OAuth2 authorization-code callback URLs for the MISHA web client."
  default     = ["http://localhost:3000/callback"]
}

variable "cognito_logout_urls" {
  type        = list(string)
  description = "OAuth2 logout callback URLs for the MISHA web client."
  default     = ["http://localhost:3000/"]
}

variable "cognito_api_identifier" {
  type        = string
  description = "URL-form resource identifier used to bind Cognito access tokens to the MISHA API."
  default     = "https://misha-api"
}
