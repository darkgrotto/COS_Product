variable "region" {
  description = "AWS region (e.g. us-east-1)"
  type        = string
  default     = "us-east-1"
}

variable "app_name" {
  description = "Application name used as prefix for resources"
  type        = string
  default     = "countorsell"
}

variable "docker_image" {
  description = "Docker image URI for the application"
  type        = string
}

variable "db_admin_username" {
  description = "RDS master username"
  type        = string
}

variable "db_admin_password" {
  description = "RDS master password"
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
  default     = "03:00"
}

variable "backup_schedule" {
  description = "Backup schedule cron expression"
  type        = string
  default     = "0 0 * * 0"
}

variable "backup_retention" {
  description = "Number of backups to retain"
  type        = string
  default     = "4"
}
