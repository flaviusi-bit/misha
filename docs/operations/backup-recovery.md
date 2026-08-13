# Backup and Recovery Baseline

## Scope

The application database is protected by both native RDS automated backups and an AWS Backup recovery plan.

## Current controls

- RDS is private and is not publicly accessible.
- Production requires deletion protection and a final snapshot identifier.
- Production keeps seven days of native RDS automated backups.
- AWS Backup creates a daily recovery point at 02:00 UTC.
- AWS Backup retains recovery points for 35 days in production and 7 days in non-production.
- The backup vault and plan are managed as Terraform resources.

## Recovery procedure

1. Confirm the incident and freeze application writes if data integrity is at risk.
2. Identify the most recent known-good RDS recovery point.
3. Restore the recovery point to a new RDS instance rather than overwriting the original database.
4. Validate schema, migrations, application connectivity, and critical business records.
5. Switch application configuration to the restored database through the normal controlled deployment path.
6. Run `/health/live`, `/health/ready`, authorization, and critical-path smoke tests.
7. Keep the original database isolated until the recovery is validated and the incident owner approves cleanup.

## Recovery objective

The current baseline provides scheduled recovery points, but a formal RPO/RTO target and a tested restore drill are still required before production launch.

## Next hardening step

Run a non-production restore drill and record the measured restore time, data-loss window, validation checks, and rollback decision.
