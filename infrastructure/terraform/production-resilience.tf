locals {
  production = var.environment == "prod"

  nat_azs = local.production ? local.azs : [local.azs[0]]
}

resource "aws_eip" "nat_resilient" {
  for_each = toset(local.nat_azs)
  domain   = "vpc"

  lifecycle {
    precondition {
      condition     = length(local.azs) >= 2 || !local.production
      error_message = "Production requires at least two availability zones for NAT resilience."
    }
  }
}

resource "aws_nat_gateway" "resilient" {
  for_each      = toset(local.nat_azs)
  allocation_id = aws_eip.nat_resilient[each.key].id
  subnet_id     = aws_subnet.public[each.key].id

  depends_on = [aws_internet_gateway.this]
}

resource "aws_route_table" "private_resilient" {
  for_each = aws_subnet.private
  vpc_id   = aws_vpc.this.id

  route {
    cidr_block     = "0.0.0.0/0"
    nat_gateway_id = aws_nat_gateway.resilient[local.production ? each.key : local.azs[0]].id
  }
}

resource "aws_route_table_association" "private_resilient" {
  for_each       = aws_subnet.private
  subnet_id      = each.value.id
  route_table_id = aws_route_table.private_resilient[each.key].id
}

resource "aws_db_instance" "postgres_resilience" {
  count = local.production ? 1 : 0

  identifier                  = aws_db_instance.postgres.identifier
  engine                      = aws_db_instance.postgres.engine
  engine_version              = aws_db_instance.postgres.engine_version
  instance_class              = aws_db_instance.postgres.instance_class
  allocated_storage           = aws_db_instance.postgres.allocated_storage
  storage_type                = aws_db_instance.postgres.storage_type
  db_name                     = aws_db_instance.postgres.db_name
  username                    = aws_db_instance.postgres.username
  manage_master_user_password = true
  db_subnet_group_name        = aws_db_subnet_group.this.name
  vpc_security_group_ids      = [aws_security_group.rds.id]
  publicly_accessible         = false
  multi_az                    = true
  backup_retention_period     = 7
  deletion_protection         = true
  final_snapshot_identifier   = var.final_snapshot_identifier

  lifecycle {
    prevent_destroy = true

    precondition {
      condition     = length(local.azs) >= 2
      error_message = "Production RDS Multi-AZ requires at least two availability zones."
    }
  }
}
