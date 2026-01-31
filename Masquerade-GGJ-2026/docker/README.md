Instrukcja: budowa i push produkcyjnego obrazu Docker

Plik Docker: Dockerfile.prod

Budowa obrazu lokalnie:

```bash
# z katalogu Masquerade-GGJ-2026
docker build -f Dockerfile.prod -t <registry>/masquerade:1.0 .
# opcjonalnie test uruchomienia
docker run --rm -p 8080:80 -e ASPNETCORE_ENVIRONMENT=Production <registry>/masquerade:1.0
```

Push do registry:

```bash
docker tag <registry>/masquerade:1.0 <registry>/masquerade:1.0
docker push <registry>/masquerade:1.0
```

Zalecenia przed wdrożeniem:
- Zarządzaj sekretami poza obrazem (Vault, Kubernetes Secrets, Docker secrets)
- Upewnij się, że TLS obsługiwany jest przez load balancer/ingress
- Przygotuj politykę CORS dla produkcji (nie otwieraj na *), aktualizuj w `Program.cs`
- Centralne logowanie i monitoring (Application Insights, Prometheus)
- Skanuj obraz (Trivy/Snyk)

Healthcheck:
- Dockerfile zawiera prosty healthcheck GET /. Rozważ dodanie endpointów /health/ready i /health/live w aplikacji.

