resource "google_storage_bucket" "backups" {
  name          = "${var.project_id}-${var.app_name}-backups"
  location      = var.region
  project       = var.project_id
  force_destroy = false

  uniform_bucket_level_access = true

  versioning {
    enabled = true
  }
}
