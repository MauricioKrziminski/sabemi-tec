import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import { MotionConfig } from "motion/react";
import { QueryProvider } from "@/lib/query-provider";
import "./globals.css";

const sans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const mono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "Sabemi | Painel de liquidações",
  description:
    "Painel administrativo das notificações de pagamento recebidas do banco parceiro.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="pt-BR" className={`${sans.variable} ${mono.variable}`}>
      <body className="ambient-glow min-h-dvh antialiased">
        <QueryProvider>
          {/* Respeita quem pediu menos movimento no sistema operacional. */}
          <MotionConfig reducedMotion="user">{children}</MotionConfig>
        </QueryProvider>
      </body>
    </html>
  );
}
