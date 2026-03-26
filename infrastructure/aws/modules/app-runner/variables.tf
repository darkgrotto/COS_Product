variable "app_name" {
  description = "Application name"
  type        = string
}

variable "docker_image" {
  description = "Docker image URI (ECR)"
  type        = string
}

variable "region" {
  description = "AWS region"
  type        = string
}

variable "postgres_connection" {
  description = "PostgreSQL connection string"
  type        = string
  sensitive   = true
}

variable "instance_name" {
  description = "Instance display name for branding"
  type        = string
}

variable "update_check_time" {
  description = "Daily update check time (HH:MM)"
  type        = string
}

variable "backup_schedule" {
  description = "Backup schedule cron expression"
  type        = string
}

variable "backup_retention" {
  description = "Number of backups to retain"
  type        = string
}

variable "s3_backup_bucket" {
  description = "S3 bucket name for backups"
  type        = string
}
