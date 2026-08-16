data "aws_availability_zones" "available" {
  state = "available"
}

data "aws_route53_zone" "primary" {
  count   = var.route53_zone_id != "" ? 1 : 0
  zone_id = var.route53_zone_id
}

locals {
  name        = "misha-${var.environment}"
  azs         = length(var.availability_zones) > 0 ? var.availability_zones : slice(data.aws_availability_zones.available.names, 0, 2)
  tls_enabled = var.domain_name != "" && var.route53_zone_id != ""
}

resource "aws_vpc" "this" {
  cidr_block           = var.vpc_cidr
  enable_dns_hostnames = true
  enable_dns_support   = true
}

resource "aws_internet_gateway" "this" {
  vpc_id = aws_vpc.this.id
}

resource "aws_subnet" "public" {
  for_each                = { for i, az in local.azs : az => i }
  vpc_id                  = aws_vpc.this.id
  availability_zone       = each.key
  cidr_block              = cidrsubnet(var.vpc_cidr, 8, each.value)
  map_public_ip_on_launch = true
}

resource "aws_subnet" "private" {
  for_each          = { for i, az in local.azs : az => i }
  vpc_id            = aws_vpc.this.id
  availability_zone = each.key
  cidr_block        = cidrsubnet(var.vpc_cidr, 8, each.value + 16)
}

resource "aws_route_table" "public" {
  vpc_id = aws_vpc.this.id
  route {
    cidr_block = "0.0.0.0/0"
    gateway_id = aws_internet_gateway.this.id
  }
}

resource "aws_route_table_association" "public" {
  for_each       = aws_subnet.public
  subnet_id      = each.value.id
  route_table_id = aws_route_table.public.id
}

locals {
  nat_azs = var.environment == "prod" ? local.azs : [local.azs[0]]
}

resource "aws_eip" "nat" {
  for_each = toset(local.nat_azs)
  domain   = "vpc"
}

resource "aws_nat_gateway" "this" {
  for_each      = toset(local.nat_azs)
  allocation_id = aws_eip.nat[each.key].id
  subnet_id     = aws_subnet.public[each.key].id
  depends_on    = [aws_internet_gateway.this]
}

resource "aws_route_table" "private" {
  for_each = toset(local.nat_azs)
  vpc_id   = aws_vpc.this.id

  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.this[each.key].id
  }
}

resource "aws_route_table_association" "private" {
  for_each       = aws_subnet.private
  subnet_id      = each.value.id
  route_table_id = aws_route_table.private[var.environment == "prod" ? each.key : local.nat_azs[0]].id
}

# Preserve the historical singleton migration, then migrate the current
# numeric for_each instances to stable Availability-Zone keys.
moved {
  from = aws_eip.nat
  to   = aws_eip.nat["0"]
}
moved {
  from = aws_eip.nat["0"]
  to   = aws_eip.nat["eu-central-1a"]
}
moved {
  from = aws_eip.nat["1"]
  to   = aws_eip.nat["eu-central-1b"]
}

moved {
  from = aws_nat_gateway.this
  to   = aws_nat_gateway.this["0"]
}
moved {
  from = aws_nat_gateway.this["0"]
  to   = aws_nat_gateway.this["eu-central-1a"]
}
moved {
  from = aws_nat_gateway.this["1"]
  to   = aws_nat_gateway.this["eu-central-1b"]
}

moved {
  from = aws_route_table.private
  to   = aws_route_table.private["0"]
}
moved {
  from = aws_route_table.private["0"]
  to   = aws_route_table.private["eu-central-1a"]
}
moved {
  from = aws_route_table.private["1"]
  to   = aws_route_table.private["eu-central-1b"]
}

resource "aws_security_group" "alb" {
  name   = "${local.name}-alb"
  vpc_id = aws_vpc.this.id
  ingress {
    protocol    = "tcp"
    from_port   = 80
    to_port     = 80
    cidr_blocks = ["0.0.0.0/0"]
  }
  ingress {
    protocol    = "tcp"
    from_port   = 443
    to_port     = 443
    cidr_blocks = ["0.0.0.0/0"]
  }
  egress {
    protocol    = "-1"
    from_port   = 0
    to_port     = 0
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "ecs" {
  name   = "${local.name}-ecs"
  vpc_id = aws_vpc.this.id
  ingress {
    protocol        = "tcp"
    from_port       = 8080
    to_port         = 8080
    security_groups = [aws_security_group.alb.id]
  }
  egress {
    protocol    = "-1"
    from_port   = 0
    to_port     = 0
    cidr_blocks = ["0.0.0.0/0"]
  }
}

resource "aws_security_group" "rds" {
  name   = "${local.name}-rds"
  vpc_id = aws_vpc.this.id
  ingress {
    protocol        = "tcp"
    from_port       = 5432
    to_port         = 5432
    security_groups = [aws_security_group.ecs.id]
  }
}

resource "aws_s3_bucket" "documents" {
  bucket_prefix = "${local.name}-documents-"
}

resource "aws_s3_bucket_public_access_block" "documents" {
  bucket                  = aws_s3_bucket.documents.id
  block_public_acls       = true
  block_public_policy     = true
  ignore_public_acls      = true
  restrict_public_buckets = true
}

resource "aws_s3_bucket_versioning" "documents" {
  bucket = aws_s3_bucket.documents.id
  versioning_configuration { status = "Enabled" }
}

resource "aws_s3_bucket_server_side_encryption_configuration" "documents" {
  bucket = aws_s3_bucket.documents.id
  rule {
    apply_server_side_encryption_by_default { sse_algorithm = "AES256" }
  }
}

resource "aws_sqs_queue" "application_events" {
  name                       = "${local.name}-application-events"
  visibility_timeout_seconds = 60
  message_retention_seconds  = 345600
}

resource "aws_sqs_queue" "application_events_dlq" {
  name = "${local.name}-application-events-dlq"
}

resource "aws_sqs_queue_redrive_policy" "application_events" {
  queue_url = aws_sqs_queue.application_events.id
  redrive_policy = jsonencode({
    deadLetterTargetArn = aws_sqs_queue.application_events_dlq.arn
    maxReceiveCount     = 5
  })
}
