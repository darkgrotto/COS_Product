output "bucket_name" {
  description = "Name of the GCS backup bucket"
  value       = google_storage_bucket.backups.name
}

output "bucket_url" {
  description = "URL of the GCS backup bucket"
  value       = google_storage_bucket.backups.url
}
