"use client";

import type { ButtonHTMLAttributes } from "react";

type AppButtonVariant =
  | "primary"
  | "secondary"
  | "ghost"
  | "danger"
  | "dangerSolid"
  | "warning";
type AppButtonSize = "sm" | "md";

export interface AppButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: AppButtonVariant;
  size?: AppButtonSize;
  loading?: boolean;
}

const variantClasses: Record<AppButtonVariant, string> = {
  primary: [
    "border-[var(--app-button-primary-border)]",
    "bg-[var(--app-button-primary-bg)]",
    "text-[var(--app-button-primary-text)]",
    "enabled:hover:bg-[var(--app-button-primary-hover-bg)]",
    "enabled:hover:border-[var(--app-button-primary-hover-border)]",
  ].join(" "),

  secondary: [
    "border-[var(--app-button-secondary-border)]",
    "bg-[var(--app-button-secondary-bg)]",
    "text-[var(--app-text)]",
    "enabled:hover:bg-[var(--app-surface-hover)]",
    "enabled:hover:border-[var(--app-border-strong)]",
  ].join(" "),

  ghost: [
    "border-transparent",
    "bg-transparent",
    "text-[var(--app-muted)]",
    "enabled:hover:bg-[var(--app-surface-hover)]",
    "enabled:hover:text-[var(--app-text)]",
  ].join(" "),

  danger: [
    "border-[var(--app-button-danger-border)]",
    "bg-[var(--app-button-danger-bg)]",
    "text-[var(--app-danger)]",
    "enabled:hover:border-[var(--app-danger)]",
    "enabled:hover:bg-[var(--app-button-danger-hover-bg)]",
  ].join(" "),

  dangerSolid: [
    "border-[var(--app-button-danger-solid-bg)]",
    "bg-[var(--app-button-danger-solid-bg)]",
    "text-[var(--app-button-danger-solid-text)]",
    "enabled:hover:border-[var(--app-button-danger-solid-hover-bg)]",
    "enabled:hover:bg-[var(--app-button-danger-solid-hover-bg)]",
  ].join(" "),

  warning: [
    "border-[var(--app-button-warning-border)]",
    "bg-[var(--app-button-warning-bg)]",
    "text-[var(--app-button-warning-text)]",
    "enabled:hover:bg-[var(--app-button-warning-hover-bg)]",
    "enabled:hover:border-[var(--app-button-warning-hover-border)]",
  ].join(" "),
};

const sizeClasses: Record<AppButtonSize, string> = {
  sm: "min-h-10 px-3 py-2 text-xs",
  md: "min-h-11 px-4 py-2.5 text-sm",
};

export function AppButton({
  variant = "secondary",
  size = "md",
  loading = false,
  disabled = false,
  type = "button",
  className = "",
  children,
  ...props
}: AppButtonProps) {
  return (
    <button
      {...props}
      type={type}
      disabled={disabled || loading}
      aria-busy={loading ? true : props["aria-busy"]}
      className={[
        "inline-flex shrink-0 items-center justify-center gap-2",
        "rounded-xl border font-semibold",
        "transition-colors motion-reduce:transition-none",
        "focus-visible:outline-none",
        "focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]",
        "disabled:cursor-not-allowed disabled:opacity-50",
        variantClasses[variant],
        sizeClasses[size],
        className,
      ].join(" ")}
    >
      {loading && (
        <svg
          aria-hidden="true"
          focusable="false"
          viewBox="0 0 24 24"
          fill="none"
          className="h-4 w-4 shrink-0 animate-spin motion-reduce:animate-none"
        >
          <circle
            cx="12"
            cy="12"
            r="9"
            stroke="currentColor"
            strokeWidth="3"
            className="opacity-25"
          />
          <path
            d="M12 3a9 9 0 019 9"
            stroke="currentColor"
            strokeWidth="3"
            strokeLinecap="round"
          />
        </svg>
      )}

      {children}
    </button>
  );
}
