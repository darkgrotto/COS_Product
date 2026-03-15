resource "aws_secretsmanager_secret" "app" {
  name        = "${var.app_name}/config"
  description = "CountOrSell application secrets"

  recovery_window_in_days = 7
}
