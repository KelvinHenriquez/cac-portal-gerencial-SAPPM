#!/bin/bash
# Certificado AUTOFIRMADO temporal para el servidor, mientras TI entrega el definitivo.
# Uso (Git Bash, en la carpeta del portal):  bash scripts/certificado_autofirmado.sh 172.25.10.112 [nombre.dns]
# Deja deploy/certs/fullchain.pem y privkey.pem (fuera de git). El navegador mostrará "no seguro"
# hasta que se ponga el certificado de la CA de CAC con el mismo nombre de archivo.
set -e
IP="${1:?Indique la IP del servidor, p. ej. 172.25.10.112}"
NOMBRE="${2:-}"
SAN="IP:${IP}"
[ -n "$NOMBRE" ] && SAN="${SAN},DNS:${NOMBRE}"
cd "$(dirname "$0")/../deploy/certs"
MSYS_NO_PATHCONV=1 openssl req -x509 -nodes -newkey rsa:2048 -days 365 \
  -subj "/CN=${NOMBRE:-$IP}" -addext "subjectAltName=${SAN}" \
  -keyout privkey.pem -out fullchain.pem
echo "Certificado autofirmado en deploy/certs para ${SAN} (válido 1 año)."
