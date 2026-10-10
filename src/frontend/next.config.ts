import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Imagen de producción mínima (Dockerfile.prod): server.js autocontenido.
  output: "standalone",
  // Sin el botón "N" de Next en desarrollo: tapaba el avatar del menú. Los errores siguen
  // apareciendo en pantalla. En producción nunca se muestra.
  devIndicators: false,
};

export default nextConfig;
