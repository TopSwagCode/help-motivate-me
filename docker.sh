#!/bin/bash

# Help Motivate Me - Docker Helper Script
# This script provides convenient commands for managing the Docker environment

set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Function to print colored messages
print_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

print_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

print_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

# Function to check if Docker is running
check_docker() {
    if ! docker info > /dev/null 2>&1; then
        print_error "Docker is not running. Please start Docker and try again."
        exit 1
    fi
}

# Function to display help
show_help() {
    cat << EOF
Help Motivate Me - Docker Management Script

Usage: ./docker.sh [COMMAND]

Commands:
    start               Start all services (build if needed)
    stop                Stop all services
    restart             Restart all services
    rebuild             Rebuild and restart all services
    logs                Show logs from all services
    logs-frontend       Show frontend logs
    logs-backend        Show backend logs
    logs-db             Show database logs
    clean               Stop services and remove volumes (deletes data!)
    reset               Complete reset (removes everything including images)
    status              Show status of all services
    db-shell            Open PostgreSQL shell
    db-migrate          Run database migrations
    db-reset            Reset database (drops and recreates)
    help                Show this help message

Examples:
    ./docker.sh start           # Start the application
    ./docker.sh logs-backend    # View backend logs
    ./docker.sh db-shell        # Connect to database

EOF
}

# Command handlers
cmd_start() {
    check_docker
    print_info "Starting all services..."
    docker compose up -d --build
    print_info "Services started successfully!"
    print_info "Frontend: http://localhost:5173"
    print_info "Backend API: http://localhost:5001"
    print_info "Mailpit UI: http://localhost:8025"
}

cmd_stop() {
    check_docker
    print_info "Stopping all services..."
    docker compose down
    print_info "Services stopped successfully!"
}

cmd_restart() {
    check_docker
    print_info "Restarting all services..."
    docker compose restart
    print_info "Services restarted successfully!"
}

cmd_rebuild() {
    check_docker
    print_info "Rebuilding and restarting all services..."
    docker compose up -d --build --force-recreate
    print_info "Services rebuilt and restarted successfully!"
}

cmd_logs() {
    check_docker
    docker compose logs -f
}

cmd_logs_frontend() {
    check_docker
    docker compose logs -f frontend
}

cmd_logs_backend() {
    check_docker
    docker compose logs -f backend
}

cmd_logs_db() {
    check_docker
    docker compose logs -f postgres
}

cmd_clean() {
    check_docker
    print_warning "This will stop all services and DELETE ALL DATA!"
    read -p "Are you sure? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Cleaning up..."
        docker compose down -v
        print_info "Cleanup complete!"
    else
        print_info "Cancelled."
    fi
}

cmd_reset() {
    check_docker
    print_warning "This will DELETE EVERYTHING (containers, volumes, images)!"
    read -p "Are you sure? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Resetting everything..."
        docker compose down -v --rmi all
        print_info "Reset complete!"
    else
        print_info "Cancelled."
    fi
}

cmd_status() {
    check_docker
    print_info "Service status:"
    docker compose ps
}

cmd_db_shell() {
    check_docker
    print_info "Opening PostgreSQL shell..."
    docker compose exec postgres psql -U postgres -d helpmotivateme
}

cmd_db_migrate() {
    check_docker
    print_info "Running database migrations..."
    docker compose exec backend dotnet ef database update
    print_info "Migrations complete!"
}

cmd_db_reset() {
    check_docker
    print_warning "This will DROP and RECREATE the database!"
    read -p "Are you sure? (y/N) " -n 1 -r
    echo
    if [[ $REPLY =~ ^[Yy]$ ]]; then
        print_info "Resetting database..."
        docker compose exec backend dotnet ef database drop --force
        docker compose exec backend dotnet ef database update
        print_info "Database reset complete!"
    else
        print_info "Cancelled."
    fi
}

# Main script
main() {
    case "${1:-}" in
        start)
            cmd_start
            ;;
        stop)
            cmd_stop
            ;;
        restart)
            cmd_restart
            ;;
        rebuild)
            cmd_rebuild
            ;;
        logs)
            cmd_logs
            ;;
        logs-frontend)
            cmd_logs_frontend
            ;;
        logs-backend)
            cmd_logs_backend
            ;;
        logs-db)
            cmd_logs_db
            ;;
        clean)
            cmd_clean
            ;;
        reset)
            cmd_reset
            ;;
        status)
            cmd_status
            ;;
        db-shell)
            cmd_db_shell
            ;;
        db-migrate)
            cmd_db_migrate
            ;;
        db-reset)
            cmd_db_reset
            ;;
        help|--help|-h|"")
            show_help
            ;;
        *)
            print_error "Unknown command: $1"
            echo
            show_help
            exit 1
            ;;
    esac
}

main "$@"
