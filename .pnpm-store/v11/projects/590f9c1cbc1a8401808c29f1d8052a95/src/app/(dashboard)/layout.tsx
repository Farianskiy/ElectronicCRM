"use client";

import type { ReactNode } from "react";
import { CurrentUserAccessProvider } from "@/features/auth/model/CurrentUserAccessContext";
import { RequireAuth } from "@/features/auth/ui/RequireAuth";
import { RequirePathPermission } from "@/features/auth/ui/RequirePathPermission";
import { AppShell } from "@/widgets/appShell/AppShell";

interface DashboardLayoutProps {
  children: ReactNode;
}

export default function DashboardLayout({ children }: DashboardLayoutProps) {
  return (
    <RequireAuth>
      <CurrentUserAccessProvider>
        <AppShell>
          <RequirePathPermission>{children}</RequirePathPermission>
        </AppShell>
      </CurrentUserAccessProvider>
    </RequireAuth>
  );
}
