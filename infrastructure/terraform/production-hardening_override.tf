# Production-only resilience overrides.
# Development retains the existing single-task / single-AZ-cost profile.

resource "aws_ecs_service" "api" {
  desired_count = var.environment == "prod" ? max(var.ecs_desired_count, 2) : var.ecs_desired_count
}

resource "aws_ecs_service" "worker" {
  desired_count = var.environment == "prod" ? max(var.ecs_worker_desired_count, 2) : var.ecs_worker_desired_count
}

resource "aws_db_instance" "postgres" {
  multi_az = var.environment == "prod"
}
