output "app_url" {
  description = "Default hostname of the App Service"
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
}

output "principal_id" {
  description = "Principal ID of the managed identity"
  value       = azurerm_linux_web_app.main.identity[0].principal_id
}
