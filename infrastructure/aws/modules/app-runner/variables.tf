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
