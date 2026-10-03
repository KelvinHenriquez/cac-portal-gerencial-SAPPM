#!/bin/bash
# Certificado autofirmado SOLO para probar el despliegue en la laptop (deploy/certs, fuera de git).
set -e
cd "$(dirname "$0")/../deploy/certs"
openssl req -x509 -nodes -newkey rsa:2048 -days 30 -subj "/CN=localhost" \
  -keyout privkey.pem -out fullchain.pem
echo "Certificado de prueba en deploy/certs (válido 30 días)."
