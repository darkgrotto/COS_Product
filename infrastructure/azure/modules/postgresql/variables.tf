variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
}

variable "location" {
  description = "Azure region"
  type        = string
}

variable "server_name" {
  description = "Name of the PostgreSQL Flexible Server"
  type        = string
}

variable "admin_username" {
  description = "Administrator login username"
  type        = string
}

variable "admin_password" {
  description = "Administrator login password"
  type        = string
  sensitive   = true
}
