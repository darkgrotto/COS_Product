terraform {
  required_version = ">= 1.5.0"
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.0"
    }
  }
  backend "gcs" {
    bucket = var.state_bucket
    prefix = "terraform/state"
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

module "cloud_run" {
  source       = "./modules/cloud-run"
  app_name     = var.app_name
  docker_image = var.docker_image
  region       = var.region
  project_id   = var.project_id
}

module "cloud_sql" {
  source         = "./modules/cloud-sql"
  app_name       = var.app_name
  region         = var.region
  db_username    = var.db_admin_username
  db_password    = var.db_admin_password
}

module "secret_manager" {
  source     = "./modules/secret-manager"
  app_name   = var.app_name
  project_id = var.project_id
}

module "cloud_storage" {
  source     = "./modules/cloud-storage"
  app_name   = var.app_name
  region     = var.region
  project_id = var.project_id
}
