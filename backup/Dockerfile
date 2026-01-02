FROM alpine:3.19

# Install required packages
RUN apk add --no-cache \
    postgresql16-client \
    bash \
    curl \
    tar \
    gzip \
    openssh-client \
    rsync \
    tzdata \
    dcron \
    && rm -rf /var/cache/apk/*

# Create backup directory
RUN mkdir -p /backups /scripts

# Copy backup scripts
COPY scripts/*.sh /scripts/
RUN chmod +x /scripts/*.sh

# Create log directory
RUN mkdir -p /var/log && touch /var/log/backup.log

# Set working directory
WORKDIR /scripts

# Set timezone (can be overridden)
ENV TZ=UTC

# Run entrypoint script
CMD ["/scripts/entrypoint.sh"]
