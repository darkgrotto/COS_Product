output "app_url" {
  description = "URL of the deployed application"
  value       = module.app_service.app_url
}

output "postgresql_fqdn" {
  description = "Fully qualified domain name of the PostgreSQL server"
  value       = module.postgresql.server_fqdn
}

output "key_vault_uri" {
  description = "URI of the Key Vault"
  value       = module.key_vault.key_vault_uri
}

output "backup_storage_account_name" {
  description = "Name of the backup storage account"
  value       = module.storage.storage_account_name
}

output "backup_container_name" {
  description = "Name of the backup storage container"
  value       = module.storage.container_name
}
