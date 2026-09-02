import type { ReactNode } from "react";

export interface PageWorkspaceProps {
  eyebrow?: string;
  title: string;
  description?: string;
  status?: ReactNode;
  actions?: ReactNode;
  tabs?: ReactNode;
  children: ReactNode;
  contentClassName?: string;
}

export function PageWorkspace({
  eyebrow,
  title,
  description,
  status,
  actions,
  tabs,
  children,
  contentClassName = "grid gap-6",
}: PageWorkspaceProps) {
  return (
    <div className="grid gap-6">
      <header className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] shadow-sm shadow-[var(--app-shadow)]">
        <div className="flex flex-col justify-between gap-5 px-5 py-5 sm:px-6 lg:flex-row lg:items-start">
          <div className="min-w-0">
            {eyebrow && (
              <p className="text-xs font-semibold uppercase tracking-[0.16em] text-[var(--app-accent)]">
                {eyebrow}
              </p>
            )}

            <div className="mt-1 flex flex-wrap items-center gap-3">
              <h1 className="text-2xl font-bold tracking-tight text-[var(--app-text)] sm:text-3xl">
                {title}
              </h1>

              {status}
            </div>

            {description && (
              <p className="mt-2 max-w-4xl text-sm leading-6 text-[var(--app-muted)]">
                {description}
              </p>
            )}
          </div>

          {actions && (
            <div className="flex shrink-0 flex-wrap items-center gap-2">
              {actions}
            </div>
          )}
        </div>

        {tabs && (
          <div className="border-t border-[var(--app-border)] px-3 py-3 sm:px-4">
            {tabs}
          </div>
        )}
      </header>

      <div className={contentClassName}>{children}</div>
    </div>
  );
}
