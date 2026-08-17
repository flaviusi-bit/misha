# Production hardening is implemented on the actual Terraform resources.
# This file intentionally contains no override blocks.
# The previous override referenced aws_ecs_service.api and aws_db_instance.postgres
# under addresses that do not exist in this configuration, which made Terraform
# fail during terraform init with "Missing resource to override".
