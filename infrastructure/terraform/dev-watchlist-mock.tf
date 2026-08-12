resource "aws_ecr_repository" "watchlist_mock" {
  count                = var.environment == "dev" ? 1 : 0
  name                 = "${local.name}-watchlist-mock"
  image_tag_mutability = "IMMUTABLE"
  image_scanning_configuration { scan_on_push = true }
  encryption_configuration { encryption_type = "AES256" }
}

resource "aws_ecr_lifecycle_policy" "watchlist_mock" {
  count      = var.environment == "dev" ? 1 : 0
  repository = aws_ecr_repository.watchlist_mock[0].name
  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Keep the latest 10 mock images"
      selection = { tagStatus = "any", countType = "imageCountMoreThan", countNumber = 10 }
      action = { type = "expire" }
    }]
  })
}
