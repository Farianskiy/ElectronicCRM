"use client";

import { useEffect, useId, useRef, useState } from "react";

export interface AppSelectOption {
  value: string;
  label: string;
  disabled?: boolean;
}

interface AppSelectProps {
  ariaLabel: string;
  value: string;
  options: readonly AppSelectOption[];
  onChange: (value: string) => void;
  disabled?: boolean;
}

export function AppSelect({
  ariaLabel,
  value,
  options,
  onChange,
  disabled = false,
}: AppSelectProps) {
  const rootRef = useRef<HTMLDivElement>(null);
  const listboxId = useId();

  const [isOpen, setIsOpen] = useState(false);
  const [activeIndex, setActiveIndex] = useState(-1);

  const selectedOption =
    options.find((option) => option.value === value) ?? null;

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    function handleOutsidePointerDown(event: PointerEvent) {
      const target = event.target;

      if (!(target instanceof Node)) {
        return;
      }

      if (!rootRef.current?.contains(target)) {
        setIsOpen(false);
      }
    }

    document.addEventListener("pointerdown", handleOutsidePointerDown);

    return () => {
      document.removeEventListener("pointerdown", handleOutsidePointerDown);
    };
  }, [isOpen]);

  function getInitialActiveIndex(): number {
    const selectedIndex = options.findIndex(
      (option) => option.value === value && !option.disabled,
    );

    if (selectedIndex >= 0) {
      return selectedIndex;
    }

    return options.findIndex((option) => !option.disabled);
  }

  function openList(): void {
    if (disabled) {
      return;
    }

    setActiveIndex(getInitialActiveIndex());
    setIsOpen(true);
  }

  function closeList(): void {
    setIsOpen(false);
  }

  function selectOption(index: number): void {
    const option = options[index];

    if (!option || option.disabled) {
      return;
    }

    onChange(option.value);
    setActiveIndex(index);
    closeList();
  }

  function moveActiveIndex(direction: 1 | -1): void {
    if (options.length === 0) {
      return;
    }

    let nextIndex = activeIndex;

    for (let iteration = 0; iteration < options.length; iteration++) {
      nextIndex = (nextIndex + direction + options.length) % options.length;

      if (!options[nextIndex]?.disabled) {
        setActiveIndex(nextIndex);
        return;
      }
    }
  }

  function handleKeyDown(event: React.KeyboardEvent<HTMLButtonElement>): void {
    if (disabled) {
      return;
    }

    if (!isOpen) {
      if (
        event.key === "Enter" ||
        event.key === " " ||
        event.key === "ArrowDown" ||
        event.key === "ArrowUp"
      ) {
        event.preventDefault();
        openList();
      }

      return;
    }

    switch (event.key) {
      case "ArrowDown":
        event.preventDefault();
        moveActiveIndex(1);
        break;

      case "ArrowUp":
        event.preventDefault();
        moveActiveIndex(-1);
        break;

      case "Home": {
        event.preventDefault();

        const firstEnabledIndex = options.findIndex(
          (option) => !option.disabled,
        );

        setActiveIndex(firstEnabledIndex);
        break;
      }

      case "End": {
        event.preventDefault();

        const lastEnabledIndex = options.findLastIndex(
          (option) => !option.disabled,
        );

        setActiveIndex(lastEnabledIndex);
        break;
      }

      case "Enter":
      case " ":
        event.preventDefault();
        selectOption(activeIndex);
        break;

      case "Escape":
        event.preventDefault();
        closeList();
        break;

      case "Tab":
        closeList();
        break;
    }
  }

  return (
    <div ref={rootRef} className="relative">
      <button
        type="button"
        role="combobox"
        aria-label={ariaLabel}
        aria-haspopup="listbox"
        aria-expanded={isOpen}
        aria-controls={listboxId}
        aria-activedescendant={
          isOpen && activeIndex >= 0
            ? `${listboxId}-option-${activeIndex}`
            : undefined
        }
        disabled={disabled}
        onClick={() => {
          if (isOpen) {
            closeList();
          } else {
            openList();
          }
        }}
        onKeyDown={handleKeyDown}
        className={[
          "flex w-full items-center justify-between",
          "rounded-2xl border px-4 py-3",
          "border-white/10 bg-black/30",
          "text-left text-sm text-slate-100",
          "outline-none transition",
          "hover:border-white/20 hover:bg-white/[0.04]",
          "focus:border-teal-400",
          "focus:ring-2 focus:ring-teal-400/20",
          "disabled:cursor-not-allowed",
          "disabled:opacity-60",
        ].join(" ")}
      >
        <span className="truncate">
          {selectedOption?.label ?? "Не выбрано"}
        </span>

        <svg
          aria-hidden="true"
          viewBox="0 0 20 20"
          fill="none"
          className={[
            "ml-3 size-4 shrink-0",
            "text-slate-400 transition-transform",
            isOpen ? "rotate-180" : "",
          ].join(" ")}
        >
          <path
            d="m6 8 4 4 4-4"
            stroke="currentColor"
            strokeWidth="1.75"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </button>

      {isOpen && (
        <ul
          id={listboxId}
          role="listbox"
          aria-label={ariaLabel}
          className={[
            "absolute left-0 right-0 top-full z-50",
            "mt-2 max-h-72 overflow-y-auto",
            "rounded-2xl border border-white/10",
            "bg-slate-950/95 p-1.5",
            "shadow-2xl shadow-black/60",
            "backdrop-blur-xl",
          ].join(" ")}
        >
          {options.map((option, index) => {
            const isSelected = option.value === value;

            const isActive = activeIndex === index;

            return (
              <li
                id={`${listboxId}-option-${index}`}
                key={`${option.value}-${index}`}
                role="option"
                aria-selected={isSelected}
                onMouseEnter={() => {
                  if (!option.disabled) {
                    setActiveIndex(index);
                  }
                }}
                onMouseDown={(event) => {
                  event.preventDefault();
                }}
                onClick={() => selectOption(index)}
                className={[
                  "flex cursor-pointer items-center",
                  "justify-between gap-3",
                  "rounded-xl px-3 py-2.5",
                  "text-sm transition",
                  isActive ? "bg-teal-500/15 text-teal-100" : "text-slate-300",
                  isSelected ? "font-medium" : "",
                  option.disabled
                    ? "pointer-events-none opacity-40"
                    : "hover:bg-white/[0.06] hover:text-white",
                ].join(" ")}
              >
                <span className="truncate">{option.label}</span>

                {isSelected && (
                  <svg
                    aria-hidden="true"
                    viewBox="0 0 20 20"
                    fill="none"
                    className="size-4 shrink-0 text-teal-400"
                  >
                    <path
                      d="m5 10 3 3 7-7"
                      stroke="currentColor"
                      strokeWidth="1.75"
                      strokeLinecap="round"
                      strokeLinejoin="round"
                    />
                  </svg>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
