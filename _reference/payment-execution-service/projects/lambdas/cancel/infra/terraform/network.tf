resource "aws_security_group" "cancel_lambda_security_group" {
  #checkov:skip=CKV_AWS_382: Lambda requires outbound access to AWS services and external APIs
  name        = "cancel_execution_lambda_security_group"
  description = "Security group for Cancel Execution Lambda"
  vpc_id      = var.vpc_id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
    description = "Allow all outbound traffic for Lambda to access AWS services and external APIs"
  }
}