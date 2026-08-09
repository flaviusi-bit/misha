terraform {
  backend "s3" {
    bucket       = "misha-terraform-state"
    key          = "misha/dev/terraform.tfstate"
    region       = "eu-central-1"
    use_lockfile = true
    encrypt      = true
  }
}
