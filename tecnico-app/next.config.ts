import type { NextConfig } from "next";

const nextConfig: NextConfig = {
  output: 'standalone',
  // Default bottom-left dev indicator overlaps the sidebar's user/logout section,
  // reading as a stray "N" badge glued to real UI — dev-only, never ships to prod.
  devIndicators: false,
};

export default nextConfig;
