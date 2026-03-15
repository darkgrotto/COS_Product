output "cloud_run_url" {
  description = "URL of the Cloud Run service"
  value       = module.cloud_run.service_url
}

output "cloud_sql_connection_name" {
  description = "Connection name of the Cloud SQL instance"
  value       = module.cloud_sql.connection_name
}

output "backup_bucket_name" {
  description = "Name of the GCS backup bucket"
  value       = module.cloud_storage.bucket_name
}

output "secret_name" {
  description = "Name of the Secret Manager secret"
  value       = module.secret_manager.secret_name
}
