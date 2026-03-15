variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
}

variable "tenant_id" {
  description = "Azure tenant ID"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region (e.g. eastus)"
  type        = string
  default     = "eastus"
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
  description = "PostgreSQL admin username"
  type        = string
}

variable "db_admin_password" {
  description = "PostgreSQL admin password"
  type        = string
  sensitive   = true
}

variable "state_resource_group_name" {
  description = "Resource group containing Terraform state storage"
  type        = string
}

variable "state_storage_account_name" {
  description = "Storage account name for Terraform state"
  type        = string
}
