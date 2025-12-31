.PHONY: help start stop restart rebuild logs logs-frontend logs-backend logs-db status clean reset db-shell db-migrate db-reset dev-deps dev-backend dev-frontend

# Default target
help:
	@echo "Help Motivate Me - Docker Management"
	@echo ""
	@echo "Usage: make [target]"
	@echo ""
	@echo "Docker Compose Targets:"
	@echo "  start           Start all services (build if needed)"
	@echo "  stop            Stop all services"
	@echo "  restart         Restart all services"
	@echo "  rebuild         Rebuild and restart all services"
	@echo "  status          Show status of all services"
	@echo "  clean           Stop and remove volumes (deletes data!)"
	@echo "  reset           Complete reset (removes everything)"
	@echo ""
	@echo "Logging Targets:"
	@echo "  logs            Show logs from all services"
	@echo "  logs-frontend   Show frontend logs"
	@echo "  logs-backend    Show backend logs"
	@echo "  logs-db         Show database logs"
	@echo ""
	@echo "Database Targets:"
	@echo "  db-shell        Open PostgreSQL shell"
	@echo "  db-migrate      Run database migrations"
	@echo "  db-reset        Reset database (drops and recreates)"
	@echo ""
	@echo "Development Targets:"
	@echo "  dev-deps        Start only dependencies (for local dev)"
	@echo "  dev-backend     Run backend locally with hot reload"
	@echo "  dev-frontend    Run frontend locally with hot reload"

# Docker Compose commands
start:
	@echo "Starting all services..."
	@docker compose up -d --build
	@echo "Services started successfully!"
	@echo "Frontend: http://localhost:3000"
	@echo "Backend API: http://localhost:5000"
	@echo "Mailpit UI: http://localhost:8025"

stop:
	@echo "Stopping all services..."
	@docker compose down
	@echo "Services stopped successfully!"

restart:
	@echo "Restarting all services..."
	@docker compose restart
	@echo "Services restarted successfully!"

rebuild:
	@echo "Rebuilding and restarting all services..."
	@docker compose up -d --build --force-recreate
	@echo "Services rebuilt and restarted successfully!"

status:
	@docker compose ps

# Logging
logs:
	@docker compose logs -f

logs-frontend:
	@docker compose logs -f frontend

logs-backend:
	@docker compose logs -f backend

logs-db:
	@docker compose logs -f postgres

# Database commands
db-shell:
	@echo "Opening PostgreSQL shell..."
	@docker compose exec postgres psql -U postgres -d helpmotivateme

db-migrate:
	@echo "Running database migrations..."
	@docker compose exec backend dotnet ef database update
	@echo "Migrations complete!"

db-reset:
	@echo "⚠️  WARNING: This will DROP and RECREATE the database!"
	@read -p "Are you sure? (y/N) " -n 1 -r; \
	echo; \
	if [ "$$REPLY" = "y" ] || [ "$$REPLY" = "Y" ]; then \
		echo "Resetting database..."; \
		docker compose exec backend dotnet ef database drop --force; \
		docker compose exec backend dotnet ef database update; \
		echo "Database reset complete!"; \
	else \
		echo "Cancelled."; \
	fi

# Cleanup
clean:
	@echo "⚠️  WARNING: This will DELETE ALL DATA!"
	@read -p "Are you sure? (y/N) " -n 1 -r; \
	echo; \
	if [ "$$REPLY" = "y" ] || [ "$$REPLY" = "Y" ]; then \
		echo "Cleaning up..."; \
		docker compose down -v; \
		echo "Cleanup complete!"; \
	else \
		echo "Cancelled."; \
	fi

reset:
	@echo "⚠️  WARNING: This will DELETE EVERYTHING!"
	@read -p "Are you sure? (y/N) " -n 1 -r; \
	echo; \
	if [ "$$REPLY" = "y" ] || [ "$$REPLY" = "Y" ]; then \
		echo "Resetting everything..."; \
		docker compose down -v --rmi all; \
		echo "Reset complete!"; \
	else \
		echo "Cancelled."; \
	fi

# Development helpers
dev-deps:
	@echo "Starting dependencies only (for local development)..."
	@cd backend && docker compose up

dev-backend:
	@echo "Running backend with hot reload..."
	@cd backend/src/HelpMotivateMe.Api && dotnet watch run

dev-frontend:
	@echo "Running frontend with hot reload..."
	@cd frontend && npm run dev
