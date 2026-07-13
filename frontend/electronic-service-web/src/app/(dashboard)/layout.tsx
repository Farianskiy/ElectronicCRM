"use client";

import type { ReactNode } from "react";
import { RequireAuth } from "@/features/auth/ui/RequireAuth";
import { AppShell } from "@/widgets/appShell/AppShell";

interface DashboardLayoutProps {
  children: ReactNode;
}

export default function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <RequireAuth>
      <AppShell>{children}</AppShell>
    </RequireAuth>
  );
}