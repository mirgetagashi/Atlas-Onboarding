#!/usr/bin/env bash
# Starts everything: infrastructure in Docker, the four .NET processes on the host.
#   ./run.sh        (Ctrl+C stops all services)
set -euo pipefail
cd "$(dirname "$0")"

echo "1/3 Starting infrastructure (SQL Server, Seq, RabbitMQ, Azurite)..."
docker compose -f platform/docker-compose.yml up -d --wait

echo "2/3 Building the solution..."
dotnet build Atlas.sln

echo "3/3 Starting services..."
pids=()
for service in \
  src/Providers/Atlas.Providers.Mock \
  src/Onboarding/Atlas.Onboarding.Api \
  src/Verification/Atlas.Verification.Worker \
  src/Backoffice/Atlas.Backoffice.Api; do
  dotnet run --no-build --project "$service" &
  pids+=($!)
done

trap 'kill "${pids[@]}" 2>/dev/null' INT TERM EXIT

echo
echo "Onboarding API   http://localhost:5100/swagger"
echo "Backoffice API   http://localhost:5200/swagger"
echo "Providers mock   http://localhost:5300/swagger"
echo "Seq (logs)       http://localhost:5341"
echo "RabbitMQ UI      http://localhost:15672  (guest/guest)"
wait
