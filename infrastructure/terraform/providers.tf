provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "misha"
      Environment = var.environment
      ManagedBy   = "terraform"
    }
  }
}
