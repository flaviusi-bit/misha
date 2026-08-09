# ECS service and ALB

This slice wires the API task definition into an ECS Fargate service behind an internet-facing Application Load Balancer.

- ALB listens on HTTP/80 for the current infrastructure/dev slice.
- `/health/ready` is used for target health checks.
- ECS tasks run in private subnets with no public IP.
- A NAT gateway provides outbound access for private Fargate tasks (for image pulls and external provider calls).
- ECS ingress on port 8080 is restricted to the ALB security group.
- HTTPS/TLS listener, certificate management, DNS, autoscaling and production hardening are subsequent deployment slices.
