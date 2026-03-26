output "db_endpoint" {
  description = "Endpoint of the RDS instance (host:port)"
  value       = aws_db_instance.main.endpoint
}

output "db_host" {
  description = "Hostname of the RDS instance (without port)"
  value       = split(":", aws_db_instance.main.endpoint)[0]
}

output "db_name" {
  description = "Name of the database"
  value       = aws_db_instance.main.db_name
}

output "db_instance_id" {
  description = "ID of the RDS instance"
  value       = aws_db_instance.main.id
}
