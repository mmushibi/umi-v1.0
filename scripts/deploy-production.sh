#!/bin/bash

# Production Deployment Script for Umi Health POS
# Domain: umihealth.zm

set -e

# Configuration
DOMAIN="umihealth.zm"
PROJECT_DIR="/var/www/umihealth.zm"
BACKUP_DIR="/var/backups/umihealth.zm"
DOCKER_REGISTRY="ghcr.io/mmushibi/umi-v1.0"
NGINX_CONFIG="/etc/nginx/sites-available/umihealth.zm"

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Logging function
log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')] $1${NC}"
}

error() {
    echo -e "${RED}[$(date +'%Y-%m-%d %H:%M:%S')] ERROR: $1${NC}"
    exit 1
}

warning() {
    echo -e "${YELLOW}[$(date +'%Y-%m-%d %H:%M:%S')] WARNING: $1${NC}"
}

# Check if running as root
if [[ $EUID -ne 0 ]]; then
   error "This script must be run as root"
fi

# Create backup
create_backup() {
    log "Creating backup..."
    mkdir -p $BACKUP_DIR
    tar -czf "$BACKUP_DIR/backup-$(date +%Y%m%d-%H%M%S).tar.gz" -C "$PROJECT_DIR" .
    log "Backup created successfully"
}

# Update application code
update_code() {
    log "Updating application code..."
    cd $PROJECT_DIR
    
    # Pull latest changes
    git pull origin master
    
    # Install dependencies
    if [ -f "package.json" ]; then
        npm ci --production
    fi
    
    # Build frontend
    npm run build-css-prod
    
    log "Application code updated"
}

# Build and deploy Docker containers
deploy_docker() {
    log "Building and deploying Docker containers..."
    
    # Stop existing containers
    docker-compose down || true
    
    # Build new images
    docker-compose build --no-cache
    
    # Start containers
    docker-compose up -d
    
    # Wait for containers to be healthy
    sleep 10
    
    # Check container health
    if ! docker-compose ps | grep -q "Up"; then
        error "Docker containers failed to start"
    fi
    
    log "Docker containers deployed successfully"
}

# Update Nginx configuration
update_nginx() {
    log "Updating Nginx configuration..."
    
    # Copy Nginx config
    cp nginx/umihealth.zm.conf $NGINX_CONFIG
    
    # Test Nginx configuration
    nginx -t
    
    # Reload Nginx
    systemctl reload nginx
    
    log "Nginx configuration updated"
}

# Update SSL certificates (Let's Encrypt)
update_ssl() {
    log "Updating SSL certificates..."
    
    # Request certificate if not exists
    if [ ! -f "/etc/ssl/certs/umihealth.zm.crt" ]; then
        certbot --nginx -d $DOMAIN -d www.$DOMAIN --non-interactive --agree-tos --email admin@$DOMAIN
    else
        # Renew existing certificate
        certbot renew
    fi
    
    log "SSL certificates updated"
}

# Database migrations with AI schema
run_migrations() {
    log "Running database migrations..."
    
    # Run migrations using Docker
    docker-compose exec backend dotnet ef database update
    
    # Apply AI learning schema if exists
    if [ -f "backend/Data/Migrations/AI_Learning_Schema.sql" ]; then
        log "Applying AI learning schema..."
        docker-compose exec postgres psql -U ${POSTGRES_USER:-umihealth_user} -d ${POSTGRES_DB:-umi_health_pos_prod} -f /docker-entrypoint-initdb.d/AI_Learning_Schema.sql
    fi
    
    log "Database migrations completed"
}

# Initialize AI services
initialize_ai_services() {
    log "Initializing AI services..."
    
    # Wait for backend to be ready
    until curl -f -s http://localhost:8080/health > /dev/null; do
        log "Waiting for backend to be ready..."
        sleep 5
    done
    
    # Seed AI knowledge base if needed
    log "Seeding AI knowledge base..."
    docker-compose exec backend dotnet run --project . --seed-ai-data || log "AI data seeding optional"
    
    # Pre-warm AI cache
    log "Pre-warming AI cache..."
    curl -X POST http://localhost:8080/api/sepioai/warmup -H "Content-Type: application/json" -d '{}' || log "AI cache warmup optional"
    
    log "AI services initialized"
}

# Health check
health_check() {
    log "Performing health check..."
    
    # Check if backend is responding
    if curl -f -s http://localhost:8080/health > /dev/null; then
        log "Backend health check passed"
    else
        error "Backend health check failed"
    fi
    
    # Check if frontend is accessible
    if curl -f -s https://$DOMAIN > /dev/null; then
        log "Frontend health check passed"
    else
        warning "Frontend health check failed - may need SSL setup"
    fi
}

# Cleanup
cleanup() {
    log "Cleaning up..."
    
    # Remove old Docker images
    docker image prune -f
    
    # Remove old backups (keep last 7 days)
    find $BACKUP_DIR -name "backup-*.tar.gz" -mtime +7 -delete
    
    log "Cleanup completed"
}

# Main deployment process
main() {
    log "Starting deployment for $DOMAIN"
    
    # Pre-deployment checks
    if [ ! -d "$PROJECT_DIR" ]; then
        error "Project directory $PROJECT_DIR does not exist"
    fi
    
    # Deployment steps
    create_backup
    update_code
    deploy_docker
    update_nginx
    update_ssl
    run_migrations
    initialize_ai_services
    health_check
    cleanup
    
    log "Deployment completed successfully for $DOMAIN"
    log "Application is available at: https://$DOMAIN"
}

# Handle script arguments
case "${1:-deploy}" in
    deploy)
        main
        ;;
    backup)
        create_backup
        ;;
    health)
        health_check
        ;;
    ssl)
        update_ssl
        ;;
    cleanup)
        cleanup
        ;;
    *)
        echo "Usage: $0 {deploy|backup|health|ssl|cleanup}"
        exit 1
        ;;
esac
