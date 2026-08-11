resource "aws_cognito_user_pool" "misha" {
  name = "misha-${var.environment}"

  username_attributes      = ["email"]
  auto_verified_attributes = ["email"]

  password_policy {
    minimum_length                   = 12
    require_lowercase                = true
    require_uppercase                = true
    require_numbers                  = true
    require_symbols                  = true
    temporary_password_validity_days = 7
  }

  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
  }

  verification_message_template {
    default_email_option = "CONFIRM_WITH_CODE"
  }

  tags = {
    Component = "identity"
  }
}

resource "aws_cognito_user_pool_domain" "misha" {
  domain       = "misha-${var.environment}-auth"
  user_pool_id = aws_cognito_user_pool.misha.id
}

resource "aws_cognito_resource_server" "api" {
  identifier   = "misha-api"
  name         = "MISHA API"
  user_pool_id = aws_cognito_user_pool.misha.id

  scope {
    scope_name        = "read"
    scope_description = "Read MISHA API resources"
  }

  scope {
    scope_name        = "write"
    scope_description = "Write MISHA API resources"
  }

  scope {
    scope_name        = "decision.read"
    scope_description = "Read decision resources"
  }

  scope {
    scope_name        = "decision.write"
    scope_description = "Create and update decision resources"
  }

  scope {
    scope_name        = "review.read"
    scope_description = "Read manual review resources"
  }

  scope {
    scope_name        = "review.write"
    scope_description = "Create and update manual review resources"
  }
}

resource "aws_cognito_user_pool_client" "misha" {
  name         = "misha-${var.environment}-web"
  user_pool_id = aws_cognito_user_pool.misha.id

  generate_secret = false

  allowed_oauth_flows_user_pool_client = true
  allowed_oauth_flows                   = ["code"]
  allowed_oauth_scopes = [
    "openid",
    "email",
    "misha-api/read",
    "misha-api/write",
    "misha-api/decision.read",
    "misha-api/decision.write",
    "misha-api/review.read",
    "misha-api/review.write",
  ]

  callback_urls = var.cognito_callback_urls
  logout_urls   = var.cognito_logout_urls

  supported_identity_providers = ["COGNITO"]

  explicit_auth_flows = [
    "ALLOW_USER_SRP_AUTH",
    "ALLOW_REFRESH_TOKEN_AUTH",
  ]

  prevent_user_existence_errors = "ENABLED"
}
