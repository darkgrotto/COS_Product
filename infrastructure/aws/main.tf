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

module "app_runner" {
  source       = "./modules/app-runner"
  app_name     = var.app_name
  docker_image = var.docker_image
  region       = var.region
}

module "rds" {
  source         = "./modules/rds"
  app_name       = var.app_name
  db_username    = var.db_admin_username
  db_password    = var.db_admin_password
}

module "secrets_manager" {
  source   = "./modules/secrets-manager"
  app_name = var.app_name
}

module "s3" {
  source   = "./modules/s3"
  app_name = var.app_name
}
