import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import StoreProvider from "@/store/StoreProvider";
import { ThemeProvider } from "@/context/ThemeContext";
import SiteNavigation from "@/components/navigation/SiteNavigation";

const geistSans = Geist({
  variable: "--font-geist-sans",
  subsets: ["latin"],
});

const geistMono = Geist_Mono({
  variable: "--font-geist-mono",
  subsets: ["latin"],
});

export const metadata: Metadata = {
  title: "BeeCloud",
  description: "Monitor and manage BeeCloud compute infrastructure.",
};

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="en" className={`${geistSans.variable} ${geistMono.variable}`}>
      <body>
        <ThemeProvider>
          <StoreProvider>
            <SiteNavigation />
            {children}
          </StoreProvider>
        </ThemeProvider>
      </body>
    </html>
  );
}
