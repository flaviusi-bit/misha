resource "aws_secretsmanager_secret_version" "application_dev_watchlist_mock" {
  count = var.environment == "dev" ? 1 : 0

  secret_id = aws_secretsmanager_secret.application.id

  secret_string = jsonencode({
    WatchlistProviderName = "dev-mock"
    WatchlistBaseUrl      = "https://dev-mock.invalid"
    WatchlistEndpoint     = "/screen"
    WatchlistApiKey       = "dev-mock-only-not-a-production-credential"
  })
}
