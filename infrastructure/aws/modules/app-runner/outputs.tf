output "service_url" {
  description = "URL of the App Runner service"
  value       = "https://${aws_apprunner_service.main.service_url}"
}

output "service_arn" {
  description = "ARN of the App Runner service"
  value       = aws_apprunner_service.main.arn
}
