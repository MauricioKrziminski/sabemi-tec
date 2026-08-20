import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  // Gera o bundle mínimo usado pela imagem Docker do painel.
  output: "standalone",
  poweredByHeader: false,
};

export default nextConfig;
