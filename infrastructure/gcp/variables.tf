variable "project_id" {
  description = "GCP project ID"
  type        = string
}

variable "region" {
  description = "GCP region (e.g. us-central1)"
  type        = string
  default     = "us-central1"
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
  description = "Cloud SQL admin username"
  type        = string
}

variable "db_admin_password" {
  description = "Cloud SQL admin password"
  type        = string
  sensitive   = true
}

variable "state_bucket" {
  description = "GCS bucket name for Terraform state"
  type        = string
}
