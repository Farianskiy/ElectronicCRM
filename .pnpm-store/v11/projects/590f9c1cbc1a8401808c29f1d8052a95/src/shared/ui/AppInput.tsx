"use client";

import type { ComponentPropsWithRef } from "react";

export type AppInputProps = ComponentPropsWithRef<"input">;

export function AppInput({
  type = "text",
  className = "",
  "aria-invalid": ariaInvalid,
  ...props
}: AppInputProps) {
  const invalid =
    ariaInvalid !== undefined &&
    ariaInvalid !== false &&
    ariaInvalid !== "false";

  return (
    <input
      {...props}
      type={type}
      aria-invalid={ariaInvalid}
      className={[
        "min-h-11 w-full min-w-0 rounded-xl border px-4 py-3",
        "bg-[var(--app-surface)] text-sm text-[var(--app-text)]",
        "placeholder:text-[var(--app-muted)]",
        "transition-colors motion-reduce:transition-none",
        "focus:outline-none focus:ring-2",
        "disabled:cursor-not-allowed disabled:opacity-60",
        "read-only:bg-[var(--app-panel)]",
        invalid
          ? [
              "border-[var(--app-danger)]",
              "enabled:hover:border-[var(--app-danger)]",
              "focus:border-[var(--app-danger)]",
              "focus:ring-[var(--app-danger)]",
            ].join(" ")
          : [
              "border-[var(--app-border)]",
              "enabled:hover:border-[var(--app-border-strong)]",
              "focus:border-[var(--app-accent)]",
              "focus:ring-[var(--app-accent)]",
            ].join(" "),
        className,
      ].join(" ")}
    />
  );
}
