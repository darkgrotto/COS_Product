# Service account used by the running container
resource "google_service_account" "app" {
  account_id   = "${var.app_name}-sa"
  display_name = "${var.app_name} service account"
  project      = var.project_id
}

# Allow the service account to update its own Cloud Run service (triggers new revision)
resource "google_project_iam_member" "app_self_deploy" {
  project = var.project_id
  role    = "roles/run.developer"
  member  = "serviceAccount:${google_service_account.app.email}"
}

resource "google_cloud_run_v2_service" "main" {
  name     = var.app_name
  location = var.region
  project  = var.project_id

  template {
    service_account = google_service_account.app.email

    containers {
      image = var.docker_image

      ports {
        container_port = 8080
      }

      env {
        name  = "CLOUD_PROVIDER"
        value = "gcp"
      }

      env {
        name  = "GCP_PROJECT_ID"
        value = var.project_id
      }

      env {
        name  = "GCP_REGION"
        value = var.region
      }

      env {
        name  = "GCP_SERVICE_NAME"
        value = var.app_name
      }

      resources {
        limits = {
          cpu    = "1000m"
          memory = "512Mi"
        }
      }
    }

    scaling {
      min_instance_count = 0
      max_instance_count = 10
    }
  }

  traffic {
    type    = "TRAFFIC_TARGET_ALLOCATION_TYPE_LATEST"
    percent = 100
  }
}

resource "google_cloud_run_v2_service_iam_member" "public" {
  project  = var.project_id
  location = var.region
  name     = google_cloud_run_v2_service.main.name
  role     = "roles/run.invoker"
  member   = "allUsers"
}
