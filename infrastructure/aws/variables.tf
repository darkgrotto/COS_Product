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
