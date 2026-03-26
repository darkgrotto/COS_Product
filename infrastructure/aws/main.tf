terraform {
  required_version = ">= 1.5.0"
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
  backend "s3" {
    key = "countorsell/terraform.tfstate"
  }
}

provider "aws" {
  region = var.region
}

locals {
  postgres_connection = "Host=${module.rds.db_host};Port=5432;Database=${module.rds.db_name};Username=${var.db_admin_username};Password=${var.db_admin_password};SSL Mode=Require;Trust Server Certificate=true;"
}

module "rds" {
  source      = "./modules/rds"
  app_name    = var.app_name
  db_username = var.db_admin_username
  db_password = var.db_admin_password
}

module "app_runner" {
  source              = "./modules/app-runner"
  app_name            = var.app_name
  docker_image        = var.docker_image
  region              = var.region
  postgres_connection = local.postgres_connection
  instance_name       = var.instance_name
  update_check_time   = var.update_check_time
  backup_schedule     = var.backup_schedule
  backup_retention    = var.backup_retention
  s3_backup_bucket    = module.s3.bucket_name
}

module "secrets_manager" {
  source   = "./modules/secrets-manager"
  app_name = var.app_name
}

module "s3" {
  source   = "./modules/s3"
  app_name = var.app_name
}
