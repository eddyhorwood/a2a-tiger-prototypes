
data "aws_secretsmanager_secret" "pagerduty_url_secret" {
  name = "collecting-payments-execution/pagerduty-cloudwatch-endpoint"
}

data "aws_secretsmanager_secret_version" "pagerduty_url" {
  secret_id = data.aws_secretsmanager_secret.pagerduty_url_secret.id
}

resource "aws_sns_topic" "pagerduty_alerts" {
  name              = "execution-dlq-pagerduty-alerts-production"
  kms_master_key_id = var.sns_kms_key_arn
}

resource "aws_sns_topic_subscription" "pagerduty_https" {
  topic_arn = aws_sns_topic.pagerduty_alerts.arn
  protocol  = "https"
  endpoint  = data.aws_secretsmanager_secret_version.pagerduty_url.secret_string
}

resource "aws_cloudwatch_metric_alarm" "dlq_message_alarm" {
  alarm_name          = "execution-dlq-message-alarm-production"
  comparison_operator = "GreaterThanThreshold"
  evaluation_periods  = 1
  metric_name         = "ApproximateNumberOfMessagesVisible"
  namespace           = "AWS/SQS"
  period              = 60
  statistic           = "Sum"
  threshold           = 0
  alarm_description   = "Alarm when execution DLQ has more than 0 messages. Runbook: https://xero.atlassian.net/wiki/x/fQexDj8"
  dimensions = {
    QueueName = var.sqs_queue_name
  }
  alarm_actions      = [aws_sns_topic.pagerduty_alerts.arn] # Creates incident when status changes to 'in alarm'
  ok_actions         = [aws_sns_topic.pagerduty_alerts.arn] # Closes incident when status changes to OK
  treat_missing_data = "notBreaching"
}
