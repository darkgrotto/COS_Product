variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "app_name" {
  description = "Application name"
  type        = string
}

variable "docker_image" {
  description = "Docker image URI"
  type        = string
}

variable "key_vault_id" {
  description = "ID of the Key Vault for access policy"
  type        = string
}
