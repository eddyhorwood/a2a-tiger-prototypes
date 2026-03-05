output "arn" {
  value       = lookup(kora_resource.aurora_postgres_aurora_postgres.properties, "arn", null)
  description = "ARN used to identify the Aurora DB cluster"
}

output "endpoint" {
  value       = lookup(kora_resource.aurora_postgres_aurora_postgres.properties, "endpoint", null)
  description = "The connection endpoint for the primary instance of the DB cluster."
}

output "port" {
  value       = lookup(kora_resource.aurora_postgres_aurora_postgres.properties, "port", null)
  description = "The port number on which the DB instances in the DB cluster accept connections."
}

output "reader_endpoint" {
  value       = lookup(kora_resource.aurora_postgres_aurora_postgres.properties, "reader_endpoint", null)
  description = "A load-balanced endpoint for a query heavy workloads."
}