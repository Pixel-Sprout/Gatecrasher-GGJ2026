# Instruction: building and pushing a production Docker image

# Docker file: Dockerfile

# Build image locally:

```bash
# from the Masquerade-GGJ-2026 directory
docker build -f Dockerfile -t <registry>/masquerade:1.0 .
# optional run/test
docker run --rm -p 8080:80 -e ASPNETCORE_ENVIRONMENT=Production <registry>/masquerade:1.0
```

# Push to registry:

```bash
docker tag <registry>/masquerade:1.0 <registry>/masquerade:1.0
docker push <registry>/masquerade:1.0
```
