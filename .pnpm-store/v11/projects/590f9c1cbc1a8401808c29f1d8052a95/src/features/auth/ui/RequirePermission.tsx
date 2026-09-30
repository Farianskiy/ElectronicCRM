"use client";

import type { ReactNode } from "react";
import { useCurrentUserAccess } from "../model/CurrentUserAccessContext";
import type { UserPermissionCode } from "../model/userPermissions";

export function RequirePermission({
  children,
  permission,
}: {
  children: ReactNode;
  permission: UserPermissionCode;
}) {
  const { hasPermission, isError, isLoading } = useCurrentUserAccess();

  if (isLoading) {
    return (
      <p className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 text-sm text-[var(--app-muted)]">
        Проверяем права доступа...
      </p>
    );
  }

  if (isError || !hasPermission(permission)) {
    return (
      <p className="rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-5 text-sm text-[var(--app-danger)]">
        Для этого действия у вашей учётной записи нет необходимого разрешения.
      </p>
    );
  }

  return children;
}
