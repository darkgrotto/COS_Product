variable "app_name" {
  description = "Application name"
  type        = string
}

variable "docker_image" {
  description = "Docker image URI"
  type        = string
}

variable "region" {
  description = "GCP region"
  type        = string
}

variable "project_id" {
  description = "GCP project ID"
  type        = string
}
