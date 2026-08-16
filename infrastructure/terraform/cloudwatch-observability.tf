locals {
  cloudwatch_alarm_prefix = "${local.name}-"
}

resource "aws_cloudwatch_metric_alarm" "alb_5xx" {
  alarm_name          = "${local.cloudwatch_alarm_prefix}alb-5xx"
  alarm_description   = "ALB generated 5xx responses indicate an unhealthy edge or service path."
  namespace           = "AWS/ApplicationELB"
  metric_name         = "HTTPCode_ELB_5XX_Count"
  dimensions = {
    LoadBalancer = aws_lb.api.arn_suffix
  }
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 1
  threshold           = 5
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
}

resource "aws_cloudwatch_metric_alarm" "target_5xx" {
  alarm_name          = "${local.cloudwatch_alarm_prefix}target-5xx"
  alarm_description   = "Backend target 5xx responses indicate an application failure."
  namespace           = "AWS/ApplicationELB"
  metric_name         = "HTTPCode_Target_5XX_Count"
  dimensions = {
    LoadBalancer = aws_lb.api.arn_suffix
  }
  statistic           = "Sum"
  period              = 300
  evaluation_periods  = 1
  threshold           = 5
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
}

resource "aws_cloudwatch_metric_alarm" "unhealthy_targets" {
  alarm_name        = "${local.cloudwatch_alarm_prefix}unhealthy-targets"
  alarm_description = "One or more API targets are unhealthy behind the load balancer."
  namespace         = "AWS/ApplicationELB"
  metric_name       = "UnHealthyHostCount"
  dimensions = {
    LoadBalancer = aws_lb.api.arn_suffix
    TargetGroup  = aws_lb_target_group.api.arn_suffix
  }
  statistic           = "Maximum"
  period              = 60
  evaluation_periods  = 2
  threshold           = 0
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
}

resource "aws_cloudwatch_metric_alarm" "ecs_cpu" {
  alarm_name        = "${local.cloudwatch_alarm_prefix}ecs-cpu"
  alarm_description = "API ECS service CPU utilization is persistently high."
  namespace         = "AWS/ECS"
  metric_name       = "CPUUtilization"
  dimensions = {
    ClusterName = aws_ecs_cluster.this.name
    ServiceName = aws_ecs_service.api.name
  }
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = 80
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
}

resource "aws_cloudwatch_metric_alarm" "rds_cpu" {
  alarm_name          = "${local.cloudwatch_alarm_prefix}rds-cpu"
  alarm_description   = "PostgreSQL CPU utilization is persistently high."
  namespace           = "AWS/RDS"
  metric_name         = "CPUUtilization"
  dimensions = {
    DBInstanceIdentifier = aws_db_instance.postgres.id
  }
  statistic           = "Average"
  period              = 300
  evaluation_periods  = 3
  threshold           = 80
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
}

resource "aws_cloudwatch_metric_alarm" "rds_free_storage" {
  alarm_name          = "${local.cloudwatch_alarm_prefix}rds-free-storage"
  alarm_description   = "PostgreSQL free storage is below 5 GiB."
  namespace           = "AWS/RDS"
  metric_name         = "FreeStorageSpace"
  dimensions = {
    DBInstanceIdentifier = aws_db_instance.postgres.id
  }
  statistic           = "Minimum"
  period              = 300
  evaluation_periods  = 1
  threshold           = 5368709120
  comparison_operator = "LessThanThreshold"
  treat_missing_data  = "notBreaching"
}

resource "aws_cloudwatch_metric_alarm" "queue_age" {
  alarm_name        = "${local.cloudwatch_alarm_prefix}application-events-age"
  alarm_description = "The oldest application event has waited more than five minutes."
  namespace         = "AWS/SQS"
  metric_name       = "ApproximateAgeOfOldestMessage"
  dimensions = {
    QueueName = aws_sqs_queue.application_events.name
  }
  statistic           = "Maximum"
  period              = 60
  evaluation_periods  = 5
  threshold           = 300
  comparison_operator = "GreaterThanThreshold"
  treat_missing_data  = "notBreaching"
}

resource "aws_cloudwatch_dashboard" "platform" {
  dashboard_name = "${local.name}-platform-health"

  dashboard_body = jsonencode({
    widgets = [
      {
        type   = "metric"
        x      = 0
        y      = 0
        width  = 12
        height = 6
        properties = {
          title  = "ALB HTTP 5xx"
          region = var.aws_region
          stat   = "Sum"
          period = 300
          metrics = [
            ["AWS/ApplicationELB", "HTTPCode_ELB_5XX_Count", "LoadBalancer", aws_lb.api.arn_suffix],
            [".", "HTTPCode_Target_5XX_Count", ".", "."]
          ]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 0
        width  = 12
        height = 6
        properties = {
          title  = "Unhealthy API targets"
          region = var.aws_region
          stat   = "Maximum"
          period = 60
          metrics = [
            [
              "AWS/ApplicationELB",
              "UnHealthyHostCount",
              "LoadBalancer",
              aws_lb.api.arn_suffix,
              "TargetGroup",
              aws_lb_target_group.api.arn_suffix
            ]
          ]
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 6
        width  = 12
        height = 6
        properties = {
          title  = "ECS API CPU"
          region = var.aws_region
          stat   = "Average"
          period = 300
          metrics = [
            [
              "AWS/ECS",
              "CPUUtilization",
              "ClusterName",
              aws_ecs_cluster.this.name,
              "ServiceName",
              aws_ecs_service.api.name
            ]
          ]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 6
        width  = 12
        height = 6
        properties = {
          title  = "RDS PostgreSQL"
          region = var.aws_region
          stat   = "Average"
          period = 300
          metrics = [
            [
              "AWS/RDS",
              "CPUUtilization",
              "DBInstanceIdentifier",
              aws_db_instance.postgres.id
            ],
            [".", "DatabaseConnections", ".", "."]
          ]
        }
      },
      {
        type   = "metric"
        x      = 0
        y      = 12
        width  = 12
        height = 6
        properties = {
          title  = "Application event queue age"
          region = var.aws_region
          stat   = "Maximum"
          period = 60
          metrics = [
            [
              "AWS/SQS",
              "ApproximateAgeOfOldestMessage",
              "QueueName",
              aws_sqs_queue.application_events.name
            ]
          ]
        }
      },
      {
        type   = "metric"
        x      = 12
        y      = 12
        width  = 12
        height = 6
        properties = {
          title  = "Application event DLQ depth"
          region = var.aws_region
          stat   = "Maximum"
          period = 60
          metrics = [
            [
              "AWS/SQS",
              "ApproximateNumberOfMessagesVisible",
              "QueueName",
              aws_sqs_queue.application_events_dlq.name
            ]
          ]
        }
      }
    ]
  })
}
