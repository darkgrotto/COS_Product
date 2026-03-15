variable "app_name" {
  description = "Application name"
  type        = string
}

variable "region" {
  description = "GCP region"
  type        = string
}

variable "db_username" {
  description = "Database user name"
  type        = string
}

variable "db_password" {
  description = "Database user password"
  type        = string
  sensitive   = true
}
