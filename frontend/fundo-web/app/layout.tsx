import type { Metadata } from "next";
import "./styles.css";

export const metadata: Metadata = { title: "Fundo loan application", description: "Apply for a Fundo loan" };

export default function Layout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
