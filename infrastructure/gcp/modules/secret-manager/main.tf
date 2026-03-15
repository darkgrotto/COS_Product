resource "google_secret_manager_secret" "app" {
  secret_id = "${var.app_name}-config"
  project   = var.project_id

  replication {
    auto {}
  }
}
