data "aws_caller_identity" "current" {}

data "aws_vpc" "default" {
  default = true
}

data "aws_subnets" "default" {
  filter {
    name   = "vpc-id"
    values = [data.aws_vpc.default.id]
  }
}

# Security group for the App Runner VPC connector
resource "aws_security_group" "app_runner" {
  name        = "${var.app_name}-app-runner-sg"
  description = "Security group for CountOrSell App Runner VPC connector"
  vpc_id      = data.aws_vpc.default.id

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
}

# VPC connector allows App Runner to reach RDS in the default VPC
resource "aws_apprunner_vpc_connector" "main" {
  vpc_connector_name = "${var.app_name}-vpc-connector"
  subnets            = data.aws_subnets.default.ids
  security_groups    = [aws_security_group.app_runner.id]
}

# Access role - used by App Runner to pull the image from ECR
resource "aws_iam_role" "app_runner_access" {
  name = "${var.app_name}-app-runner-access"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "build.apprunner.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_iam_role_policy_attachment" "app_runner_ecr" {
  role       = aws_iam_role.app_runner_access.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSAppRunnerServicePolicyForECRAccess"
}

# Instance role - used by the running container for AWS API calls
resource "aws_iam_role" "app_runner_instance" {
  name = "${var.app_name}-app-runner-instance"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Action = "sts:AssumeRole"
        Effect = "Allow"
        Principal = {
          Service = "tasks.apprunner.amazonaws.com"
        }
      }
    ]
  })
}

resource "aws_ecr_repository" "main" {
  name                 = var.app_name
  image_tag_mutability = "MUTABLE"

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_apprunner_service" "main" {
  service_name = var.app_name

  source_configuration {
    image_repository {
      image_configuration {
        port = "8080"
        runtime_environment_variables = {
          CLOUD_PROVIDER                = "aws"
          CLOUD_APP_RUNNER_SERVICE_NAME = var.app_name
          CLOUD_REGION                  = var.region
          CLOUD_ECR_REGISTRY            = "${data.aws_caller_identity.current.account_id}.dkr.ecr.${var.region}.amazonaws.com"
          POSTGRES_CONNECTION           = var.postgres_connection
          INSTANCE_NAME                 = var.instance_name
          UPDATE_CHECK_TIME             = var.update_check_time
          BACKUP_SCHEDULE               = var.backup_schedule
          BACKUP_RETENTION              = var.backup_retention
          BLOB_BACKUP_CONNECTION        = var.s3_backup_bucket
        }
      }
      image_identifier      = var.docker_image
      image_repository_type = "ECR"
    }
    authentication_configuration {
      access_role_arn = aws_iam_role.app_runner_access.arn
    }
    auto_deployments_enabled = false
  }

  instance_configuration {
    cpu               = "1024"
    memory            = "2048"
    instance_role_arn = aws_iam_role.app_runner_instance.arn
  }

  network_configuration {
    egress_configuration {
      egress_type       = "VPC"
      vpc_connector_arn = aws_apprunner_vpc_connector.main.arn
    }
  }

  auto_scaling_configuration_arn = aws_apprunner_auto_scaling_configuration_version.main.arn

  depends_on = [aws_ecr_repository.main]
}

# Allow the instance role to look up and redeploy itself
resource "aws_iam_role_policy" "app_runner_self_deploy" {
  name = "self-deploy"
  role = aws_iam_role.app_runner_instance.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect   = "Allow"
        Action   = ["apprunner:StartDeployment", "apprunner:DescribeService", "apprunner:UpdateService"]
        Resource = aws_apprunner_service.main.arn
      },
      {
        Effect   = "Allow"
        Action   = "apprunner:ListServices"
        Resource = "*"
      }
    ]
  })
}

# Allow the instance role to read and write the S3 backup bucket
resource "aws_iam_role_policy" "app_runner_s3_backup" {
  name = "s3-backup"
  role = aws_iam_role.app_runner_instance.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "s3:PutObject",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Resource = [
          "arn:aws:s3:::${var.s3_backup_bucket}",
          "arn:aws:s3:::${var.s3_backup_bucket}/*"
        ]
      }
    ]
  })
}

resource "aws_apprunner_auto_scaling_configuration_version" "main" {
  auto_scaling_configuration_name = "${var.app_name}-scaling"
  min_size                         = 1
  max_size                         = 5
  max_concurrency                  = 100
}
