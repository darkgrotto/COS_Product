output "secret_name" {
  description = "Name of the Secret Manager secret"
  value       = google_secret_manager_secret.app.secret_id
}

output "secret_id" {
  description = "Full resource ID of the Secret Manager secret"
  value       = google_secret_manager_secret.app.id
}
