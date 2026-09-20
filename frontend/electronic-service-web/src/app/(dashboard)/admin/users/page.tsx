"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import axios from "axios";
import type { FormEvent } from "react";
import { useState } from "react";
import {
  createAdminUser,
  getAdminUsers,
  getUserAccess,
  updateUserAccess,
} from "@/features/adminUsers/api/adminUsersApi";
import type {
  AdminUserListItem,
  CreatableUserType,
  GetUserAccessResponse,
} from "@/features/adminUsers/model/types";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { RequirePermission } from "@/features/auth/ui/RequirePermission";
import { defaultPermissionsByUserType } from "@/features/auth/model/userPermissions";
import { isSystemDeveloper } from "@/shared/api/authToken";
import { AppSelect } from "@/shared/ui/AppSelect";
import { PageHeader } from "@/shared/ui/PageHeader";

const roleLabels = {
  Regular: "Пользователь",
  Manager: "Менеджер",
  Technical: "Технический специалист",
  Administrator: "Администратор",
  SystemDeveloper: "Разработчик системы",
  Admin: "Разработчик системы",
} as const;

const staffRoleOptions = [
  { value: "Manager", label: "Менеджер" },
  { value: "Technical", label: "Технический специалист" },
  { value: "Regular", label: "Пользователь" },
] as const;

const developerRoleOptions = [
  { value: "Administrator", label: "Администратор" },
  ...staffRoleOptions,
] as const;

const userTabs = [
  { value: "Administrator", label: "Администраторы" },
  { value: "Technical", label: "Технические специалисты" },
  { value: "Manager", label: "Менеджеры" },
  { value: "Regular", label: "Пользователи" },
] as const;

type UserTab = (typeof userTabs)[number]["value"];

function getErrorMessage(error: unknown): string {
  if (
    axios.isAxiosError(error) &&
    typeof error.response?.data?.detail === "string"
  ) {
    return error.response.data.detail;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Не удалось выполнить операцию.";
}

export default function AdminUsersPage() {
  return (
    <RequirePermission permission="UsersManage">
      <AdminUsersContent />
    </RequirePermission>
  );
}

function SystemDeveloperCard({ user }: { user?: AdminUserListItem }) {
  return (
    <section className="rounded-3xl border border-teal-500/20 bg-teal-500/[0.06] p-6">
      <p className="text-xs font-semibold uppercase tracking-[0.18em] text-teal-300">
        Разработчик системы
      </p>

      <div className="mt-3 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h2 className="text-xl font-semibold text-white">
            {user?.displayName ?? "Загрузка..."}
          </h2>

          <p className="mt-1 text-sm text-slate-400">
            {user?.email ?? "Единственная учётная запись с полным доступом"}
          </p>
        </div>

        <span className="rounded-full border border-teal-400/30 bg-teal-400/10 px-3 py-1 text-sm text-teal-200">
          Полный доступ
        </span>
      </div>
    </section>
  );
}

function AdminUsersContent() {
  const queryClient = useQueryClient();

  const session = useAuthSession();
  const systemDeveloper = isSystemDeveloper(session);

  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [userType, setUserType] = useState<CreatableUserType>("Manager");

  const [searchDraft, setSearchDraft] = useState("");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const [selectedTab, setSelectedTab] = useState<UserTab>("Administrator");
  const [selectedUser, setSelectedUser] = useState<AdminUserListItem | null>(
    null,
  );

  const [createdMessage, setCreatedMessage] = useState<string | null>(null);

  const usersQuery = useQuery({
    queryKey: ["admin-users", selectedTab, search, page],
    queryFn: () => getAdminUsers(search, page, selectedTab),
  });

  const developerQuery = useQuery({
    queryKey: ["system-developer"],
    queryFn: () => getAdminUsers("", 1, "SystemDeveloper"),
    enabled: systemDeveloper,
  });

  const createMutation = useMutation({
    mutationFn: createAdminUser,

    onSuccess: async () => {
      setDisplayName("");
      setEmail("");
      setPassword("");
      setCreatedMessage("Учётная запись создана.");

      await queryClient.invalidateQueries({
        queryKey: ["admin-users"],
      });
    },
  });

  function handleCreate(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setCreatedMessage(null);

    createMutation.mutate({
      displayName,
      email,
      password,
      userType,
    });
  }

  function handleSearch(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();

    setPage(1);
    setSearch(searchDraft.trim());
  }

  const totalPages = Math.max(
    1,
    Math.ceil(
      (usersQuery.data?.totalCount ?? 0) / (usersQuery.data?.pageSize ?? 25),
    ),
  );

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Пользователи"
        description="Создание учётных записей, роли и права доступа."
      />

      {systemDeveloper && (
        <SystemDeveloperCard user={developerQuery.data?.items[0]} />
      )}

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <h2 className="text-xl font-semibold text-white">
          Новая учётная запись
        </h2>

        <p className="mt-2 text-sm text-slate-400">
          Разработчик системы создаётся отдельно и не может быть заменён через
          эту форму.
        </p>

        <form
          onSubmit={handleCreate}
          className="mt-5 grid gap-4 lg:grid-cols-2"
        >
          <label className="grid gap-2 text-sm text-slate-300">
            Имя
            <input
              required
              minLength={2}
              maxLength={100}
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none focus:border-teal-400"
            />
          </label>

          <label className="grid gap-2 text-sm text-slate-300">
            Email
            <input
              required
              type="email"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none focus:border-teal-400"
            />
          </label>

          <label className="grid gap-2 text-sm text-slate-300">
            Пароль
            <input
              required
              type="password"
              minLength={8}
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none focus:border-teal-400"
            />
          </label>

          <label className="grid gap-2 text-sm text-slate-300">
            Роль
            <AppSelect
              ariaLabel="Роль пользователя"
              value={userType}
              options={
                systemDeveloper ? developerRoleOptions : staffRoleOptions
              }
              onChange={(value) => setUserType(value as CreatableUserType)}
            />
          </label>

          <div className="lg:col-span-2">
            <button
              type="submit"
              disabled={createMutation.isPending}
              className="rounded-2xl bg-teal-500 px-5 py-3 text-sm font-medium text-slate-950 transition hover:bg-teal-400 disabled:opacity-50"
            >
              {createMutation.isPending ? "Создаём..." : "Создать пользователя"}
            </button>
          </div>
        </form>

        {createdMessage && (
          <p className="mt-4 rounded-2xl border border-emerald-500/30 bg-emerald-500/10 p-4 text-sm text-emerald-200">
            {createdMessage}
          </p>
        )}

        {createMutation.isError && (
          <p className="mt-4 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-sm text-red-200">
            {getErrorMessage(createMutation.error)}
          </p>
        )}
      </section>

      <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
        <div className="mb-6 flex flex-wrap gap-2">
          {userTabs.map((tab) => (
            <button
              key={tab.value}
              type="button"
              onClick={() => {
                setSelectedTab(tab.value);
                setPage(1);
              }}
              className={
                selectedTab === tab.value
                  ? "rounded-xl bg-teal-500 px-4 py-2 text-sm font-medium text-slate-950"
                  : "rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300 hover:bg-white/[0.06]"
              }
            >
              {tab.label}
            </button>
          ))}
        </div>

        <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h2 className="text-xl font-semibold text-white">
              {userTabs.find((tab) => tab.value === selectedTab)?.label}
            </h2>

            <p className="mt-2 text-sm text-slate-400">
              Найдено: {usersQuery.data?.totalCount ?? 0}
            </p>
          </div>

          <form onSubmit={handleSearch} className="flex gap-3">
            <input
              value={searchDraft}
              onChange={(event) => setSearchDraft(event.target.value)}
              placeholder="Имя или email"
              className="min-w-0 rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-white outline-none placeholder:text-slate-600 focus:border-teal-400"
            />

            <button
              type="submit"
              disabled={usersQuery.isFetching}
              className="rounded-2xl border border-white/10 px-5 py-3 text-sm text-slate-200 transition hover:bg-white/[0.06] disabled:opacity-50"
            >
              Найти
            </button>
          </form>
        </div>

        {usersQuery.isLoading && (
          <p className="mt-6 text-slate-300">Загружаем пользователей...</p>
        )}

        {usersQuery.isError && (
          <p className="mt-6 text-red-300">
            {getErrorMessage(usersQuery.error)}
          </p>
        )}

        {usersQuery.isSuccess && (
          <div className="mt-6 overflow-x-auto">
            <table className="w-full min-w-[760px] text-left text-sm">
              <thead className="border-b border-white/10 text-slate-500">
                <tr>
                  <th className="px-3 py-3 font-medium">Пользователь</th>

                  <th className="px-3 py-3 font-medium">Email</th>

                  <th className="px-3 py-3 font-medium">Роль</th>

                  <th className="px-3 py-3 font-medium">Статус</th>

                  <th className="px-3 py-3 font-medium">Создан</th>

                  <th className="px-3 py-3 font-medium">Доступ</th>
                </tr>
              </thead>

              <tbody>
                {usersQuery.data.items.map((user) => (
                  <tr
                    key={user.id}
                    className="border-b border-white/5 text-slate-200"
                  >
                    <td className="px-3 py-4 font-medium text-white">
                      {user.displayName}
                    </td>

                    <td className="px-3 py-4">{user.email ?? "—"}</td>

                    <td className="px-3 py-4">{roleLabels[user.userType]}</td>

                    <td className="px-3 py-4">
                      <span
                        className={
                          user.status === "Active"
                            ? "rounded-full bg-emerald-500/10 px-3 py-1 text-emerald-300"
                            : "rounded-full bg-red-500/10 px-3 py-1 text-red-300"
                        }
                      >
                        {user.status === "Active" ? "Активен" : "Заблокирован"}
                      </span>
                    </td>

                    <td className="px-3 py-4 text-slate-400">
                      {new Date(user.createdAtUtc).toLocaleString("ru-RU")}
                    </td>

                    <td className="px-3 py-4">
                      <button
                        type="button"
                        onClick={() => setSelectedUser(user)}
                        className="rounded-xl border border-white/10 px-3 py-2 text-xs font-medium text-slate-200 transition hover:bg-white/[0.06]"
                      >
                        Настроить доступ
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {usersQuery.data.items.length === 0 && (
              <p className="py-8 text-center text-slate-400">
                Пользователи не найдены.
              </p>
            )}

            {totalPages > 1 && (
              <div className="mt-5 flex items-center justify-end gap-3">
                <button
                  type="button"
                  disabled={page === 1 || usersQuery.isFetching}
                  onClick={() =>
                    setPage((currentPage) => Math.max(1, currentPage - 1))
                  }
                  className="rounded-2xl border border-white/10 px-4 py-2 text-sm text-slate-200 transition hover:bg-white/[0.06] disabled:opacity-50"
                >
                  Назад
                </button>

                <span className="text-sm text-slate-400">
                  Страница {page} из {totalPages}
                </span>

                <button
                  type="button"
                  disabled={page >= totalPages || usersQuery.isFetching}
                  onClick={() => setPage((currentPage) => currentPage + 1)}
                  className="rounded-2xl border border-white/10 px-4 py-2 text-sm text-slate-200 transition hover:bg-white/[0.06] disabled:opacity-50"
                >
                  Далее
                </button>
              </div>
            )}
          </div>
        )}
      </section>

      {selectedUser && (
        <UserAccessEditor
          user={selectedUser}
          systemDeveloper={systemDeveloper}
          onClose={() => setSelectedUser(null)}
        />
      )}
    </div>
  );
}

function UserAccessEditor({
  user,
  systemDeveloper,
  onClose,
}: {
  user: AdminUserListItem;
  systemDeveloper: boolean;
  onClose: () => void;
}) {
  const accessQuery = useQuery({
    queryKey: ["user-access", user.id],
    queryFn: () => getUserAccess(user.id),
  });

  return (
    <section className="rounded-3xl border border-teal-500/20 bg-white/[0.05] p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-teal-300">
            Настройка доступа
          </p>

          <h2 className="mt-2 text-xl font-semibold text-white">
            {user.displayName}
          </h2>

          <p className="mt-1 text-sm text-slate-400">
            {user.email ?? "Email не указан"}
          </p>
        </div>

        <button
          type="button"
          onClick={onClose}
          className="rounded-xl border border-white/10 px-4 py-2 text-sm text-slate-300 transition hover:bg-white/[0.06]"
        >
          Закрыть
        </button>
      </div>

      {accessQuery.isLoading && (
        <p className="mt-6 text-slate-300">Загружаем права...</p>
      )}

      {accessQuery.isError && (
        <p className="mt-6 text-red-300">
          {getErrorMessage(accessQuery.error)}
        </p>
      )}

      {accessQuery.data && (
        <UserAccessForm
          key={`${user.id}-${accessQuery.data.userType}`}
          user={user}
          access={accessQuery.data}
          systemDeveloper={systemDeveloper}
        />
      )}
    </section>
  );
}

function UserAccessForm({
  user,
  access,
  systemDeveloper,
}: {
  user: AdminUserListItem;
  access: GetUserAccessResponse;
  systemDeveloper: boolean;
}) {
  const queryClient = useQueryClient();

  const [userType, setUserType] = useState<CreatableUserType>(
    access.userType as CreatableUserType,
  );

  const [allowedPermissions, setAllowedPermissions] = useState<Set<string>>(
    () =>
      new Set(
        access.permissions
          .filter((permission) => permission.isAllowed)
          .map((permission) => permission.code),
      ),
  );

  const updateMutation = useMutation({
    mutationFn: () =>
      updateUserAccess(user.id, {
        userType,
        allowedPermissions: [...allowedPermissions],
      }),

    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({
          queryKey: ["admin-users"],
        }),
        queryClient.invalidateQueries({
          queryKey: ["user-access", user.id],
        }),
        queryClient.invalidateQueries({
          queryKey: ["current-user-access"],
        }),
      ]);
    },
  });

  const roleOptions = systemDeveloper
    ? developerRoleOptions
    : user.userType === "Administrator"
      ? [
          {
            value: "Administrator",
            label: "Администратор",
            disabled: true,
          },
          ...staffRoleOptions,
        ]
      : staffRoleOptions;

  const groups = access.permissions.reduce<
    Record<string, GetUserAccessResponse["permissions"]>
  >((result, permission) => {
    (result[permission.group] ??= []).push(permission);

    return result;
  }, {});

  function togglePermission(code: string): void {
    setAllowedPermissions((current) => {
      const next = new Set(current);

      if (next.has(code)) {
        next.delete(code);
      } else {
        next.add(code);
      }

      return next;
    });
  }

  function resetCurrentRoleDefaults(): void {
    setAllowedPermissions(new Set(defaultPermissionsByUserType[userType]));
  }

  return (
    <div className="mt-6 grid gap-6">
      <label className="grid max-w-xl gap-2 text-sm text-slate-300">
        Роль
        <AppSelect
          ariaLabel="Новая роль пользователя"
          value={userType}
          options={roleOptions}
          onChange={(value) => {
            const nextUserType = value as CreatableUserType;
            setUserType(nextUserType);
            setAllowedPermissions(
              new Set(defaultPermissionsByUserType[nextUserType]),
            );
          }}
        />
      </label>

      <p className="text-xs text-slate-500">
        Роль задаёт стандартные права. Переключатели ниже сохраняются как
        индивидуальные исключения.
      </p>

      {Object.entries(groups).map(([group, permissions]) => (
        <div key={group}>
          <h3 className="text-sm font-semibold text-white">{group}</h3>

          <div className="mt-3 grid gap-3 md:grid-cols-2">
            {permissions.map((permission) => (
              <label
                key={permission.code}
                className="flex cursor-pointer items-start gap-3 rounded-2xl border border-white/10 bg-black/20 p-4"
              >
                <input
                  type="checkbox"
                  checked={allowedPermissions.has(permission.code)}
                  onChange={() => togglePermission(permission.code)}
                  className="mt-1 h-4 w-4 accent-teal-500"
                />

                <span>
                  <span className="block text-sm font-medium text-slate-100">
                    {permission.label}
                  </span>

                  {permission.isOverridden && (
                    <span className="mt-1 block text-xs text-amber-300">
                      Индивидуальная настройка
                    </span>
                  )}
                </span>
              </label>
            ))}
          </div>
        </div>
      ))}

      <div className="flex flex-wrap gap-3">
        <button
          type="button"
          onClick={() => updateMutation.mutate()}
          disabled={updateMutation.isPending}
          className="rounded-xl bg-teal-500 px-5 py-3 text-sm font-medium text-slate-950 transition hover:bg-teal-400 disabled:opacity-50"
        >
          {updateMutation.isPending ? "Сохраняем..." : "Сохранить доступ"}
        </button>

        <button
          type="button"
          onClick={resetCurrentRoleDefaults}
          className="rounded-xl border border-white/10 px-5 py-3 text-sm text-slate-300 transition hover:bg-white/[0.06] disabled:opacity-40"
        >
          Вернуть права роли
        </button>
      </div>

      {updateMutation.isSuccess && (
        <p className="text-sm text-emerald-300">
          Права пользователя сохранены.
        </p>
      )}

      {updateMutation.isError && (
        <p className="text-sm text-red-300">
          {getErrorMessage(updateMutation.error)}
        </p>
      )}
    </div>
  );
}
