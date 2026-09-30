"use client";

import { useQuery } from "@tanstack/react-query";
import { createContext, useContext, useMemo, type ReactNode } from "react";
import { getCurrentUserAccess } from "../api/getCurrentUserAccess";
import { useAuthSession } from "./useAuthSession";
import type {
  CurrentUserAccessResponse,
  UserPermissionCode,
} from "./userPermissions";

interface CurrentUserAccessContextValue {
  access: CurrentUserAccessResponse | null;
  allowedPermissions: ReadonlySet<UserPermissionCode>;
  isLoading: boolean;
  isError: boolean;
  hasPermission: (permission: UserPermissionCode) => boolean;
}

const CurrentUserAccessContext =
  createContext<CurrentUserAccessContextValue | null>(null);

export function CurrentUserAccessProvider({
  children,
}: {
  children: ReactNode;
}) {
  const session = useAuthSession();
  const accessQuery = useQuery({
    queryKey: ["current-user-access", session?.userId],
    queryFn: getCurrentUserAccess,
    enabled: session !== null,
    staleTime: 30_000,
  });
  const allowedPermissions = useMemo(
    () =>
      new Set(
        accessQuery.data?.permissions
          .filter((permission) => permission.isAllowed)
          .map((permission) => permission.code) ?? [],
      ),
    [accessQuery.data],
  );
  const value = useMemo<CurrentUserAccessContextValue>(
    () => ({
      access: accessQuery.data ?? null,
      allowedPermissions,
      isLoading: accessQuery.isLoading,
      isError: accessQuery.isError,
      hasPermission: (permission) => allowedPermissions.has(permission),
    }),
    [
      accessQuery.data,
      accessQuery.isError,
      accessQuery.isLoading,
      allowedPermissions,
    ],
  );

  return (
    <CurrentUserAccessContext.Provider value={value}>
      {children}
    </CurrentUserAccessContext.Provider>
  );
}

export function useCurrentUserAccess(): CurrentUserAccessContextValue {
  const context = useContext(CurrentUserAccessContext);

  if (!context) {
    throw new Error(
      "useCurrentUserAccess must be used inside CurrentUserAccessProvider.",
    );
  }

  return context;
}
