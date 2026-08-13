# PostgreSQL database container for the water controller server.
# Build:  docker build -t water-controller-db ./database
# Run:    docker run -d --name water-controller-db -p 5432:5432 \
#           -e POSTGRES_PASSWORD=postgres \
#           -v water-controller-db-data:/var/lib/postgresql/data \
#           water-controller-db
FROM postgres:17-alpine

# Init scripts in this directory run automatically on first startup
# (only when the data volume is empty).
COPY init.sql /docker-entrypoint-initdb.d/01-init.sql

HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD pg_isready -U "${POSTGRES_USER:-postgres}" -d "${POSTGRES_DB:-postgres}"
