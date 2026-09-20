"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { useCurrentUserAccess } from "../model/CurrentUserAccessContext";
import type { UserPermissionCode } from "../model/userPermissions";

const protectedPaths: ReadonlyArray<{
  path: string;
  permissions: readonly UserPermissionCode[];
  exact?: boolean;
}> = [
  { path: "/admin/users", permissions: ["UsersManage"] },
  {
    path: "/catalog/assistant-suggestions",
    permissions: ["DictionariesManage"],
  },
  { path: "/catalog/import-reviews", permissions: ["CatalogImportsReview"] },
  { path: "/catalog/price-lists", permissions: ["PriceListsManage"] },
  { path: "/catalog/product-types", permissions: ["DictionariesManage"] },
  { path: "/catalog/characteristics", permissions: ["DictionariesManage"] },
  { path: "/catalog/recognition", permissions: ["DictionariesManage"] },
  {
    path: "/catalog/price-calculations",
    permissions: ["PriceCalculationsManage"],
  },
  { path: "/catalog/imports/new", permissions: ["CatalogImportsCreate"] },
  {
    path: "/catalog/imports",
    permissions: ["CatalogImportsCreate"],
    exact: true,
  },
  {
    path: "/catalog/imports",
    permissions: ["CatalogImportsCreate", "CatalogImportsReview"],
  },
  { path: "/catalog/assistant", permissions: ["AssistantUse"] },
  { path: "/catalog/products", permissions: ["ProductsView"] },
];

export function RequirePathPermission({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const { hasPermission, isError, isLoading } = useCurrentUserAccess();
  const requiredPermissions = protectedPaths.find(
    ({ path, exact }) =>
      pathname === path || (!exact && pathname.startsWith(`${path}/`)),
  )?.permissions;

  if (isLoading) {
    return <AccessState message="Проверяем права доступа..." />;
  }

  if (isError) {
    return (
      <AccessState message="Не удалось загрузить права доступа. Обновите страницу или войдите заново." />
    );
  }

  if (requiredPermissions && !requiredPermissions.some(hasPermission)) {
    return <AccessDenied />;
  }

  return children;
}

function AccessState({ message }: { message: string }) {
  return (
    <section className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-6 text-sm text-[var(--app-muted)]">
      {message}
    </section>
  );
}

function AccessDenied() {
  return (
    <section className="rounded-3xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-6">
      <h1 className="text-xl font-semibold text-[var(--app-danger)]">
        Нет доступа
      </h1>
      <p className="mt-2 text-sm text-[var(--app-muted)]">
        Для этого раздела у вашей учётной записи нет необходимого разрешения.
      </p>
      <Link
        href="/"
        className="mt-5 inline-flex rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-4 py-2 text-sm font-medium text-[var(--app-text)]"
      >
        Вернуться на главную
      </Link>
    </section>
  );
}
