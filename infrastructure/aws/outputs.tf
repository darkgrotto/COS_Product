output "app_runner_url" {
  description = "URL of the App Runner service"
  value       = module.app_runner.service_url
}

output "rds_endpoint" {
  description = "Endpoint of the RDS PostgreSQL instance"
  value       = module.rds.db_endpoint
}

output "backup_bucket_name" {
  description = "Name of the S3 backup bucket"
  value       = module.s3.bucket_name
}

output "secrets_manager_arn" {
  description = "ARN of the Secrets Manager secret"
  value       = module.secrets_manager.secret_arn
}
