# Production resilience scope

This change hardens only the production environment:

- ECS API desired count is at least 2.
- ECS Worker desired count is at least 2.
- RDS PostgreSQL uses Multi-AZ.
- Development retains its existing single-task / single-AZ-cost profile.

NAT topology is intentionally deferred to a separate change because converting the existing singleton NAT resources to per-AZ resources changes Terraform resource addressing and needs an explicit migration plan. No AWS Apply is performed by this change.