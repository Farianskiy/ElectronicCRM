"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  useEffect,
  useRef,
  useState,
  useSyncExternalStore,
  type ReactNode,
} from "react";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import {
  clearAuthSession,
  isTechnicalUser,
  type AuthSession,
} from "@/shared/api/authToken";
import { useAppTheme, type AppThemeMode } from "@/shared/theme/useAppTheme";
import {
  useAppWorkspaceMode,
  type AppWorkspaceMode,
} from "@/shared/workspace/useAppWorkspaceMode";

interface AppShellProps {
  children: ReactNode;
}

type NavigationIconName =
  | "home"
  | "assistant"
  | "products"
  | "priceCalculations"
  | "imports"
  | "review"
  | "priceLists"
  | "types"
  | "characteristics"
  | "quality"
  | "suggestions";

interface NavigationItem {
  href: string;
  label: string;
  description: string;
  icon: NavigationIconName;
  technicalOnly?: boolean;
}

interface NavigationGroup {
  label: string;
  workspaceMode: AppWorkspaceMode;
  items: NavigationItem[];
}

type HeaderMenu = "workspace" | "notifications" | "user";

const navigationGroups: NavigationGroup[] = [
  {
    label: "Обзор",
    workspaceMode: "catalog",
    items: [
      {
        href: "/",
        label: "Главная",
        description: "Состояние CRM и основные действия",
        icon: "home",
      },
    ],
  },
  {
    label: "Работа",
    workspaceMode: "catalog",
    items: [
      {
        href: "/catalog/assistant",
        label: "Ассистент",
        description: "Поиск товаров обычным текстом",
        icon: "assistant",
      },
      {
        href: "/catalog/products",
        label: "Товары",
        description: "Каталог, цены и характеристики",
        icon: "products",
      },
      {
        href: "/catalog/price-calculations",
        label: "Расчёт цен",
        description: "Проектные скидки и расчёты",
        icon: "priceCalculations",
      },
      {
        href: "/catalog/imports",
        label: "Импорт",
        description: "Загрузка и обработка Excel",
        icon: "imports",
      },
    ],
  },
  {
    label: "Контроль",
    workspaceMode: "quality",
    items: [
      {
        href: "/catalog/import-reviews",
        label: "Очередь проверки",
        description: "Импорты, ожидающие решения",
        icon: "review",
        technicalOnly: true,
      },
      {
        href: "/catalog/price-lists",
        label: "Прайс-листы",
        description: "Загрузка, проверка и активация цен",
        icon: "priceLists",
        technicalOnly: true,
      },
    ],
  },
  {
    label: "Справочники",
    workspaceMode: "quality",
    items: [
      {
        href: "/catalog/product-types",
        label: "Типы товаров",
        description: "Схемы типов и их характеристики",
        icon: "types",
        technicalOnly: true,
      },
      {
        href: "/catalog/characteristics",
        label: "Характеристики",
        description: "Определения и правила значений",
        icon: "characteristics",
        technicalOnly: true,
      },
    ],
  },
  {
    label: "Качество данных",
    workspaceMode: "quality",
    items: [
      {
        href: "/catalog/recognition",
        label: "Распознавание",
        description: "Правила, профили и диагностика",
        icon: "quality",
        technicalOnly: true,
      },
      {
        href: "/catalog/assistant-suggestions",
        label: "Предложения словаря",
        description: "Неизвестные слова и исправления",
        icon: "suggestions",
        technicalOnly: true,
      },
    ],
  },
];

const workspaceOptions: Array<{
  id: AppWorkspaceMode;
  label: string;
  shortLabel: string;
  description: string;
}> = [
  {
    id: "catalog",
    label: "Работа с каталогом",
    shortLabel: "Каталог",
    description: "Ассистент, товары и импорт Excel",
  },
  {
    id: "quality",
    label: "Настройка и качество",
    shortLabel: "Качество",
    description: "Справочники, распознавание и проверка",
  },
];

const SIDEBAR_COLLAPSED_STORAGE_KEY = "electronic_crm_sidebar_collapsed";
const SIDEBAR_COLLAPSED_CHANGED_EVENT =
  "electronic_crm_sidebar_collapsed_changed";

function isNavigationItemActive(pathname: string, href: string): boolean {
  if (href === "/") {
    return pathname === "/";
  }

  return pathname === href || pathname.startsWith(`${href}/`);
}

function getPathnameWorkspaceMode(pathname: string): AppWorkspaceMode | null {
  for (const group of navigationGroups) {
    if (
      group.items.some((item) => isNavigationItemActive(pathname, item.href))
    ) {
      return group.workspaceMode;
    }
  }

  return null;
}

function getVisibleNavigationGroups(
  technical: boolean,
  workspaceMode: AppWorkspaceMode,
): NavigationGroup[] {
  return navigationGroups
    .filter((group) => group.workspaceMode === workspaceMode)
    .map((group) => ({
      ...group,
      items: group.items.filter((item) => !item.technicalOnly || technical),
    }))
    .filter((group) => group.items.length > 0);
}

function subscribeSidebarCollapsed(callback: () => void): () => void {
  if (typeof window === "undefined") {
    return () => {};
  }

  window.addEventListener("storage", callback);
  window.addEventListener(SIDEBAR_COLLAPSED_CHANGED_EVENT, callback);

  return () => {
    window.removeEventListener("storage", callback);
    window.removeEventListener(SIDEBAR_COLLAPSED_CHANGED_EVENT, callback);
  };
}

function getSidebarCollapsedSnapshot(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  return localStorage.getItem(SIDEBAR_COLLAPSED_STORAGE_KEY) === "true";
}

function getSidebarCollapsedServerSnapshot(): boolean {
  return false;
}

function setStoredSidebarCollapsed(collapsed: boolean): void {
  if (typeof window === "undefined") {
    return;
  }

  localStorage.setItem(SIDEBAR_COLLAPSED_STORAGE_KEY, String(collapsed));

  window.dispatchEvent(new Event(SIDEBAR_COLLAPSED_CHANGED_EVENT));
}

export function AppShell({ children }: AppShellProps) {
  const router = useRouter();
  const pathname = usePathname();
  const session = useAuthSession();
  const technical = isTechnicalUser(session);

  const { mode: themeMode, setMode: setThemeMode } = useAppTheme();

  const { mode: storedWorkspaceMode, setMode: setStoredWorkspaceMode } =
    useAppWorkspaceMode();

  const sidebarCollapsed = useSyncExternalStore(
    subscribeSidebarCollapsed,
    getSidebarCollapsedSnapshot,
    getSidebarCollapsedServerSnapshot,
  );

  const [mobileNavigationOpen, setMobileNavigationOpen] = useState(false);
  const [openHeaderMenu, setOpenHeaderMenu] = useState<HeaderMenu | null>(null);

  const headerMenusRef = useRef<HTMLDivElement>(null);
  const appShellRef = useRef<HTMLDivElement>(null);
  const appHeaderRef = useRef<HTMLElement>(null);

  const pathnameWorkspaceMode = getPathnameWorkspaceMode(pathname);

  const workspaceMode: AppWorkspaceMode = technical
    ? (pathnameWorkspaceMode ?? storedWorkspaceMode)
    : "catalog";

  const visibleNavigationGroups = getVisibleNavigationGroups(
    technical,
    workspaceMode,
  );

  const activeWorkspace =
    workspaceOptions.find((option) => option.id === workspaceMode) ??
    workspaceOptions[0];

  useEffect(() => {
    const shell = appShellRef.current;
    const header = appHeaderRef.current;

    if (!shell || !header) {
      return;
    }

    const updateHeaderHeight = () => {
      const height = header.getBoundingClientRect().height;

      shell.style.setProperty("--app-header-height", `${height}px`);
    };

    updateHeaderHeight();

    const resizeObserver = new ResizeObserver(updateHeaderHeight);
    resizeObserver.observe(header, { box: "border-box" });

    return () => {
      resizeObserver.disconnect();
      shell.style.removeProperty("--app-header-height");
    };
  }, []);

  useEffect(() => {
    if (!mobileNavigationOpen) {
      return;
    }

    const previousOverflow = document.body.style.overflow;

    document.body.style.overflow = "hidden";

    function handleKeyDown(event: KeyboardEvent): void {
      if (event.key === "Escape") {
        setMobileNavigationOpen(false);
      }
    }

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [mobileNavigationOpen]);

  useEffect(() => {
    if (openHeaderMenu === null) {
      return;
    }

    function handlePointerDown(event: PointerEvent): void {
      if (!(event.target instanceof Node)) {
        return;
      }

      if (!headerMenusRef.current?.contains(event.target)) {
        setOpenHeaderMenu(null);
      }
    }

    function handleKeyDown(event: KeyboardEvent): void {
      if (event.key === "Escape") {
        setOpenHeaderMenu(null);
      }
    }

    document.addEventListener("pointerdown", handlePointerDown);
    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.removeEventListener("pointerdown", handlePointerDown);
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [openHeaderMenu]);

  function toggleHeaderMenu(menu: HeaderMenu): void {
    setOpenHeaderMenu((current) => (current === menu ? null : menu));
  }

  function handleLogout(): void {
    setMobileNavigationOpen(false);
    setOpenHeaderMenu(null);
    clearAuthSession();
    router.push("/login");
  }

  function handleSidebarCollapsedChange(collapsed: boolean): void {
    setStoredSidebarCollapsed(collapsed);
  }

  function handleWorkspaceModeChange(mode: AppWorkspaceMode): void {
    setStoredWorkspaceMode(mode);
    setOpenHeaderMenu(null);
    setMobileNavigationOpen(false);

    router.push(mode === "catalog" ? "/" : "/catalog/import-reviews");
  }

  function handleNavigation(): void {
    setStoredWorkspaceMode(workspaceMode);
  }

  function handleMobileNavigation(): void {
    setStoredWorkspaceMode(workspaceMode);
    setMobileNavigationOpen(false);
  }

  function handleThemeToggle(): void {
    setThemeMode(themeMode === "dark" ? "light" : "dark");
  }

  return (
    <div
      ref={appShellRef}
      className="min-h-screen bg-[var(--app-background)] text-[var(--app-text)]"
    >
      <aside
        className={
          sidebarCollapsed
            ? "fixed inset-y-0 left-0 z-40 hidden w-20 overflow-hidden border-r border-[var(--app-border)] bg-[var(--app-sidebar)] transition-[width] duration-200 xl:flex"
            : "fixed inset-y-0 left-0 z-40 hidden w-72 overflow-hidden border-r border-[var(--app-border)] bg-[var(--app-sidebar)] transition-[width] duration-200 xl:flex"
        }
      >
        <NavigationPanel
          pathname={pathname}
          navigationGroups={visibleNavigationGroups}
          collapsed={sidebarCollapsed}
          showCollapseToggle
          onCollapsedChange={handleSidebarCollapsedChange}
          onNavigate={handleNavigation}
        />
      </aside>

      {mobileNavigationOpen && (
        <div className="fixed inset-0 z-50 xl:hidden">
          <button
            type="button"
            aria-label="Закрыть навигацию"
            onClick={() => setMobileNavigationOpen(false)}
            className="absolute inset-0 cursor-default bg-[var(--app-overlay)] backdrop-blur-sm"
          />

          <aside
            role="dialog"
            aria-modal="true"
            aria-label="Навигация Electronic CRM"
            className="relative h-full w-[min(88vw,320px)] overflow-hidden border-r border-[var(--app-border)] bg-[var(--app-sidebar)] shadow-2xl shadow-black/40"
          >
            <button
              type="button"
              aria-label="Закрыть меню"
              onClick={() => setMobileNavigationOpen(false)}
              className="absolute right-4 top-4 z-10 flex h-10 w-10 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)] transition hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
            >
              <svg
                aria-hidden="true"
                viewBox="0 0 24 24"
                className="h-5 w-5"
                fill="none"
                stroke="currentColor"
                strokeWidth="1.8"
                strokeLinecap="round"
              >
                <path d="M6 6l12 12M18 6L6 18" />
              </svg>
            </button>

            <NavigationPanel
              pathname={pathname}
              navigationGroups={visibleNavigationGroups}
              collapsed={false}
              onNavigate={handleMobileNavigation}
            />
          </aside>
        </div>
      )}

      <div
        className={
          sidebarCollapsed
            ? "min-h-screen transition-[padding] duration-200 xl:pl-20"
            : "min-h-screen transition-[padding] duration-200 xl:pl-72"
        }
      >
        <header
          ref={appHeaderRef}
          className="sticky top-0 z-30 border-b border-[var(--app-border)] bg-[var(--app-header)] backdrop-blur-xl"
        >
          <div
            ref={headerMenusRef}
            className="relative flex min-h-16 items-center justify-between gap-2 px-3 sm:gap-3 sm:px-6 lg:px-8"
          >
            <div className="flex min-w-0 flex-1 items-center gap-2">
              <button
                type="button"
                aria-label="Открыть навигацию"
                aria-expanded={mobileNavigationOpen}
                onClick={() => setMobileNavigationOpen(true)}
                className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)] transition hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)] xl:hidden"
              >
                <svg
                  aria-hidden="true"
                  viewBox="0 0 24 24"
                  className="h-5 w-5"
                  fill="none"
                  stroke="currentColor"
                  strokeWidth="1.8"
                  strokeLinecap="round"
                >
                  <path d="M4 7h16M4 12h16M4 17h16" />
                </svg>
              </button>

              {technical ? (
                <div className="min-w-0 sm:relative">
                  <button
                    type="button"
                    aria-label={
                      "Выбрать рабочий режим: " + activeWorkspace.label
                    }
                    title={activeWorkspace.label}
                    aria-haspopup="menu"
                    aria-expanded={openHeaderMenu === "workspace"}
                    onClick={() => toggleHeaderMenu("workspace")}
                    className="flex h-10 w-10 min-w-0 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)] transition hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)] sm:h-auto sm:min-h-10 sm:w-full sm:justify-start sm:gap-2 sm:px-3 sm:py-2"
                  >
                    <WorkspaceIcon mode={workspaceMode} />

                    <span className="hidden min-w-0 truncate text-sm font-semibold sm:block">
                      {activeWorkspace.label}
                    </span>

                    <span className="hidden shrink-0 sm:inline-flex">
                      <ChevronIcon expanded={openHeaderMenu === "workspace"} />
                    </span>
                  </button>

                  {openHeaderMenu === "workspace" && (
                    <div className="absolute inset-x-3 top-[calc(100%+0.5rem)] z-10 max-h-[calc(100dvh-6rem)] overflow-y-auto overscroll-contain rounded-2xl border border-[var(--app-border)] bg-[var(--app-sidebar)] p-2 shadow-[0_24px_70px_var(--app-shadow)] sm:left-0 sm:right-auto sm:top-[calc(100%+0.75rem)] sm:w-[360px]">
                      <p className="px-3 pb-2 pt-2 text-xs font-semibold uppercase tracking-[0.14em] text-[var(--app-subtle)]">
                        Рабочий режим
                      </p>

                      <div className="grid gap-1">
                        {workspaceOptions.map((option) => {
                          const selected = option.id === workspaceMode;

                          return (
                            <button
                              key={option.id}
                              type="button"
                              onClick={() =>
                                handleWorkspaceModeChange(option.id)
                              }
                              className={
                                selected
                                  ? "flex items-start gap-3 rounded-xl border border-teal-500/20 bg-[var(--app-accent-soft)] px-3 py-3 text-left text-[var(--app-accent)]"
                                  : "flex items-start gap-3 rounded-xl border border-transparent px-3 py-3 text-left text-[var(--app-muted)] transition hover:border-[var(--app-border)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
                              }
                            >
                              <span className="mt-0.5">
                                <WorkspaceIcon mode={option.id} />
                              </span>

                              <span className="min-w-0 flex-1">
                                <span className="block text-sm font-semibold">
                                  {option.label}
                                </span>

                                <span className="mt-1 block text-xs leading-5 opacity-70">
                                  {option.description}
                                </span>
                              </span>

                              {selected && <CheckIcon />}
                            </button>
                          );
                        })}
                      </div>
                    </div>
                  )}
                </div>
              ) : (
                <div className="hidden items-center gap-2 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-sm font-semibold text-[var(--app-muted)] sm:flex">
                  <WorkspaceIcon mode="catalog" />
                  Работа с каталогом
                </div>
              )}
            </div>

            <div className="flex shrink-0 items-center gap-2">
              <button
                type="button"
                aria-label={
                  themeMode === "dark"
                    ? "Включить светлую тему"
                    : "Включить тёмную тему"
                }
                title={
                  themeMode === "dark"
                    ? "Включить светлую тему"
                    : "Включить тёмную тему"
                }
                onClick={handleThemeToggle}
                className="flex h-10 w-10 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)] transition hover:border-[var(--app-border-strong)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]"
              >
                <ThemeIcon mode={themeMode} />
              </button>

              <div className="sm:relative">
                <button
                  type="button"
                  aria-label="Открыть уведомления"
                  title="Уведомления"
                  aria-haspopup="dialog"
                  aria-expanded={openHeaderMenu === "notifications"}
                  onClick={() => toggleHeaderMenu("notifications")}
                  className="flex h-10 w-10 items-center justify-center rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-muted)] transition hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
                >
                  <BellIcon />
                </button>

                {openHeaderMenu === "notifications" && (
                  <div
                    role="dialog"
                    aria-label="Уведомления"
                    className="absolute inset-x-3 top-[calc(100%+0.5rem)] z-10 max-h-[calc(100dvh-6rem)] overflow-y-auto overscroll-contain rounded-2xl border border-[var(--app-border)] bg-[var(--app-sidebar)] shadow-[0_24px_70px_var(--app-shadow)] sm:left-auto sm:right-0 sm:top-[calc(100%+0.75rem)] sm:w-[360px]"
                  >
                    <div className="border-b border-[var(--app-border)] px-4 py-4">
                      <p className="text-sm font-semibold text-[var(--app-text)]">
                        Уведомления
                      </p>

                      <p className="mt-1 text-xs text-[var(--app-muted)]">
                        События, требующие вашего внимания
                      </p>
                    </div>

                    <div className="flex flex-col items-center px-6 py-8 text-center">
                      <div className="flex h-12 w-12 items-center justify-center rounded-2xl bg-[var(--app-surface)] text-[var(--app-subtle)]">
                        <BellIcon />
                      </div>

                      <p className="mt-4 text-sm font-semibold text-[var(--app-text)]">
                        Уведомлений пока нет
                      </p>

                      <p className="mt-2 max-w-64 text-xs leading-5 text-[var(--app-muted)]">
                        Здесь появятся результаты импортов и события, требующие
                        решения пользователя.
                      </p>
                    </div>
                  </div>
                )}
              </div>

              <div className="sm:relative">
                <button
                  type="button"
                  aria-label="Открыть меню пользователя"
                  aria-haspopup="menu"
                  aria-expanded={openHeaderMenu === "user"}
                  onClick={() => toggleHeaderMenu("user")}
                  className="flex items-center gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-1.5 pr-2 transition hover:bg-[var(--app-surface-hover)] sm:pr-3"
                >
                  <UserAvatar session={session} />

                  <div className="hidden min-w-0 text-left md:block">
                    <p className="max-w-40 truncate text-sm font-semibold text-[var(--app-text)]">
                      {session?.displayName ?? "Пользователь"}
                    </p>

                    <p className="text-xs text-[var(--app-subtle)]">
                      {technical ? "Технический специалист" : "Пользователь"}
                    </p>
                  </div>

                  <ChevronIcon expanded={openHeaderMenu === "user"} />
                </button>

                {openHeaderMenu === "user" && (
                  <div
                    role="menu"
                    aria-label="Меню пользователя"
                    className="absolute inset-x-3 top-[calc(100%+0.5rem)] z-10 max-h-[calc(100dvh-6rem)] overflow-y-auto overscroll-contain rounded-2xl border border-[var(--app-border)] bg-[var(--app-sidebar)] shadow-[0_24px_70px_var(--app-shadow)] sm:left-auto sm:right-0 sm:top-[calc(100%+0.75rem)] sm:w-[300px]"
                  >
                    <div className="border-b border-[var(--app-border)] px-4 py-4">
                      <p className="truncate text-sm font-semibold text-[var(--app-text)]">
                        {session?.displayName ?? "Пользователь"}
                      </p>

                      <p className="mt-1 text-xs text-[var(--app-muted)]">
                        {technical ? "Технический специалист" : "Пользователь"}
                      </p>
                    </div>

                    <div className="p-2">
                      <Link
                        href="/profile"
                        role="menuitem"
                        onClick={() => setOpenHeaderMenu(null)}
                        className="flex items-center gap-3 rounded-xl px-3 py-2.5 text-sm font-medium text-[var(--app-muted)] transition hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
                      >
                        <svg
                          aria-hidden="true"
                          viewBox="0 0 24 24"
                          className="h-5 w-5 shrink-0"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="1.8"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        >
                          <path d="M12 12a4 4 0 100-8 4 4 0 000 8zM4 21a8 8 0 0116 0" />
                        </svg>
                        Профиль
                      </Link>
                    </div>

                    <div className="border-t border-[var(--app-border)] p-2">
                      <button
                        type="button"
                        role="menuitem"
                        onClick={handleLogout}
                        className="flex w-full items-center gap-3 rounded-xl px-3 py-2.5 text-left text-sm font-medium text-[var(--app-danger)] transition hover:bg-[var(--app-danger-soft)]"
                      >
                        <svg
                          aria-hidden="true"
                          viewBox="0 0 24 24"
                          className="h-5 w-5 shrink-0"
                          fill="none"
                          stroke="currentColor"
                          strokeWidth="1.8"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        >
                          <path d="M10 17l5-5-5-5M15 12H3M14 4h5a2 2 0 012 2v12a2 2 0 01-2 2h-5" />
                        </svg>
                        Выйти
                      </button>
                    </div>
                  </div>
                )}
              </div>
            </div>
          </div>
        </header>

        <main className="mx-auto w-full max-w-[1680px] px-4 py-5 sm:px-6 sm:py-6 lg:px-8">
          {children}
        </main>
      </div>
    </div>
  );
}

function NavigationPanel({
  pathname,
  navigationGroups,
  collapsed,
  showCollapseToggle = false,
  onCollapsedChange,
  onNavigate,
}: {
  pathname: string;
  navigationGroups: NavigationGroup[];
  collapsed: boolean;
  showCollapseToggle?: boolean;
  onCollapsedChange?: (collapsed: boolean) => void;
  onNavigate?: () => void;
}) {
  return (
    <div className="flex h-full min-w-0 w-full flex-col overflow-hidden">
      <div className="shrink-0 border-b border-[var(--app-border)] p-3">
        <Link
          href="/"
          onClick={onNavigate}
          title={collapsed ? "Electronic CRM" : undefined}
          className={
            collapsed
              ? "flex h-14 items-center justify-center rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] transition hover:bg-[var(--app-surface-hover)]"
              : "block rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4 transition hover:bg-[var(--app-surface-hover)]"
          }
        >
          <div
            className={
              collapsed
                ? "flex items-center justify-center"
                : "flex items-center gap-3"
            }
          >
            <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-2xl bg-gradient-to-br from-teal-400 to-cyan-600 text-sm font-black text-slate-950 shadow-lg shadow-teal-950/20">
              EC
            </div>

            {!collapsed && (
              <div className="min-w-0">
                <p className="truncate text-sm font-semibold text-[var(--app-text)]">
                  Electronic CRM
                </p>

                <p className="mt-0.5 truncate text-xs text-[var(--app-subtle)]">
                  Каталог и распознавание
                </p>
              </div>
            )}
          </div>
        </Link>

        {showCollapseToggle && onCollapsedChange && (
          <button
            type="button"
            aria-label={
              collapsed ? "Развернуть навигацию" : "Свернуть навигацию"
            }
            title={collapsed ? "Развернуть навигацию" : "Свернуть навигацию"}
            onClick={() => onCollapsedChange(!collapsed)}
            className={
              collapsed
                ? "mt-3 flex w-full items-center justify-center rounded-xl border border-transparent py-2.5 text-[var(--app-muted)] transition hover:border-[var(--app-border)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
                : "mt-3 flex w-full items-center gap-3 rounded-xl border border-transparent px-3 py-2.5 text-sm font-medium text-[var(--app-muted)] transition hover:border-[var(--app-border)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
            }
          >
            <SidebarCollapseIcon collapsed={collapsed} />

            {!collapsed && <span>Свернуть</span>}
          </button>
        )}
      </div>

      <nav
        aria-label="Основная навигация"
        className="min-h-0 min-w-0 flex-1 overflow-y-auto overflow-x-hidden px-3 py-4"
      >
        <div className="grid min-w-0 gap-5">
          {navigationGroups.map((group, groupIndex) => (
            <div key={group.label} className="min-w-0">
              {collapsed ? (
                groupIndex > 0 && (
                  <div className="mx-2 mb-3 border-t border-[var(--app-border)]" />
                )
              ) : (
                <p className="mb-2 px-3 text-[11px] font-semibold uppercase tracking-[0.16em] text-[var(--app-subtle)]">
                  {group.label}
                </p>
              )}

              <div className="grid min-w-0 gap-1">
                {group.items.map((item) => {
                  const active = isNavigationItemActive(pathname, item.href);

                  return (
                    <Link
                      key={item.href}
                      href={item.href}
                      onClick={onNavigate}
                      title={
                        collapsed
                          ? `${item.label} — ${item.description}`
                          : undefined
                      }
                      aria-current={active ? "page" : undefined}
                      className={
                        active
                          ? collapsed
                            ? "group flex items-center justify-center rounded-xl border border-teal-500/20 bg-[var(--app-accent-soft)] py-2.5 text-[var(--app-accent)]"
                            : "group flex min-w-0 items-center gap-3 rounded-xl border border-teal-500/20 bg-[var(--app-accent-soft)] px-3 py-2.5 text-[var(--app-accent)]"
                          : collapsed
                            ? "group flex items-center justify-center rounded-xl border border-transparent py-2.5 text-[var(--app-subtle)] transition hover:border-[var(--app-border)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
                            : "group flex min-w-0 items-center gap-3 rounded-xl border border-transparent px-3 py-2.5 text-[var(--app-muted)] transition hover:border-[var(--app-border)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]"
                      }
                    >
                      <span
                        className={
                          active
                            ? "flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[var(--app-accent-soft)] text-[var(--app-accent)]"
                            : "flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-[var(--app-surface)] text-[var(--app-subtle)] transition group-hover:bg-[var(--app-surface-hover)] group-hover:text-[var(--app-text)]"
                        }
                      >
                        <NavigationIcon name={item.icon} />
                      </span>

                      {!collapsed && (
                        <span className="min-w-0">
                          <span className="block truncate text-sm font-medium">
                            {item.label}
                          </span>

                          <span className="mt-0.5 block truncate text-[11px] opacity-60">
                            {item.description}
                          </span>
                        </span>
                      )}
                    </Link>
                  );
                })}
              </div>
            </div>
          ))}
        </div>
      </nav>
    </div>
  );
}

function SidebarCollapseIcon({ collapsed }: { collapsed: boolean }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-5 w-5 shrink-0"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M4 4h16v16H4V4zM9 4v16" />

      {collapsed ? <path d="M13 9l3 3-3 3" /> : <path d="M16 9l-3 3 3 3" />}
    </svg>
  );
}

function WorkspaceIcon({ mode }: { mode: AppWorkspaceMode }) {
  if (mode === "quality") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 24 24"
        className="h-5 w-5 shrink-0"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
        strokeLinejoin="round"
      >
        <path d="M12 3l7 3v5c0 4.5-2.7 8.1-7 10-4.3-1.9-7-5.5-7-10V6l7-3zM9 12l2 2 4-5" />
      </svg>
    );
  }

  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-5 w-5 shrink-0"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M4 7l8-4 8 4-8 4-8-4zM4 7v10l8 4 8-4V7M12 11v10" />
    </svg>
  );
}

function ThemeIcon({ mode }: { mode: AppThemeMode }) {
  if (mode === "light") {
    return (
      <svg
        aria-hidden="true"
        viewBox="0 0 24 24"
        className="h-5 w-5"
        fill="none"
        stroke="currentColor"
        strokeWidth="1.8"
        strokeLinecap="round"
      >
        <circle cx="12" cy="12" r="4" />
        <path d="M12 2v2M12 20v2M4.93 4.93l1.42 1.42M17.65 17.65l1.42 1.42M2 12h2M20 12h2M4.93 19.07l1.42-1.42M17.65 6.35l1.42-1.42" />
      </svg>
    );
  }

  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-5 w-5"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M21 14.5A9 9 0 119.5 3 7 7 0 0021 14.5z" />
    </svg>
  );
}

function BellIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-5 w-5"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M18 8a6 6 0 00-12 0c0 7-3 7-3 9h18c0-2-3-2-3-9M10 21h4" />
    </svg>
  );
}

function ChevronIcon({ expanded }: { expanded: boolean }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className={
        expanded
          ? "h-4 w-4 shrink-0 rotate-180 text-[var(--app-subtle)] transition-transform"
          : "h-4 w-4 shrink-0 text-[var(--app-subtle)] transition-transform"
      }
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M7 9l5 5 5-5" />
    </svg>
  );
}

function CheckIcon() {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="mt-0.5 h-5 w-5 shrink-0"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M5 12l4 4L19 6" />
    </svg>
  );
}

function UserAvatar({ session }: { session: AuthSession | null }) {
  return (
    <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-xl bg-gradient-to-br from-teal-400 to-cyan-600 text-sm font-bold text-slate-950">
      {session?.displayName?.[0]?.toUpperCase() ?? "U"}
    </div>
  );
}

function NavigationIcon({ name }: { name: NavigationIconName }) {
  if (name === "home") {
    return (
      <NavigationIconContainer>
        <path d="M4 10.5L12 4l8 6.5V20a1 1 0 01-1 1h-5v-6h-4v6H5a1 1 0 01-1-1v-9.5z" />
      </NavigationIconContainer>
    );
  }

  if (name === "assistant") {
    return (
      <NavigationIconContainer>
        <path d="M5 5h14a2 2 0 012 2v8a2 2 0 01-2 2h-7l-4.5 3v-3H5a2 2 0 01-2-2V7a2 2 0 012-2zM8 9h8M8 13h5" />
      </NavigationIconContainer>
    );
  }

  if (name === "products") {
    return (
      <NavigationIconContainer>
        <path d="M4 7l8-4 8 4-8 4-8-4zM4 7v10l8 4 8-4V7M12 11v10" />
      </NavigationIconContainer>
    );
  }

  if (name === "priceCalculations") {
    return (
      <NavigationIconContainer>
        <path d="M4 5h16v14H4V5zM8 9h8M8 13h3M15 13h1M8 16h3M15 16h1" />
      </NavigationIconContainer>
    );
  }

  if (name === "imports") {
    return (
      <NavigationIconContainer>
        <path d="M4 15v4a2 2 0 002 2h12a2 2 0 002-2v-4M12 3v13M7 11l5 5 5-5" />
      </NavigationIconContainer>
    );
  }

  if (name === "review") {
    return (
      <NavigationIconContainer>
        <path d="M9 5H5a2 2 0 00-2 2v12a2 2 0 002 2h14a2 2 0 002-2V7a2 2 0 00-2-2h-4M9 3h6v4H9V3zM7 12l3 3 6-6" />
      </NavigationIconContainer>
    );
  }

  if (name === "priceLists") {
    return (
      <NavigationIconContainer>
        <path d="M5 3h14v18H5V3zM8 7h8M8 11h8M8 15h4M15 15h1" />
      </NavigationIconContainer>
    );
  }

  if (name === "types") {
    return (
      <NavigationIconContainer>
        <path d="M4 4h6v6H4V4zM14 4h6v6h-6V4zM4 14h6v6H4v-6zM14 14h6v6h-6v-6z" />
      </NavigationIconContainer>
    );
  }

  if (name === "characteristics") {
    return (
      <NavigationIconContainer>
        <path d="M4 7h10M18 7h2M4 17h2M10 17h10M14 4v6M6 14v6" />
      </NavigationIconContainer>
    );
  }

  if (name === "quality") {
    return (
      <NavigationIconContainer>
        <path d="M12 3l7 3v5c0 4.5-2.7 8.1-7 10-4.3-1.9-7-5.5-7-10V6l7-3zM9 12l2 2 4-5" />
      </NavigationIconContainer>
    );
  }

  return (
    <NavigationIconContainer>
      <path d="M12 3l1.6 4.4L18 9l-4.4 1.6L12 15l-1.6-4.4L6 9l4.4-1.6L12 3zM18 15l.9 2.1L21 18l-2.1.9L18 21l-.9-2.1L15 18l2.1-.9L18 15z" />
    </NavigationIconContainer>
  );
}

function NavigationIconContainer({ children }: { children: ReactNode }) {
  return (
    <svg
      aria-hidden="true"
      viewBox="0 0 24 24"
      className="h-[18px] w-[18px]"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      {children}
    </svg>
  );
}
