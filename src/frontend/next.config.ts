import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Imagen de producción mínima (Dockerfile.prod): server.js autocontenido.
  output: "standalone",
};

export default nextConfig;
