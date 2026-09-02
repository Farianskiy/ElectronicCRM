"use client";

import { useRef, type KeyboardEvent } from "react";

export interface WorkspaceTabItem<TabId extends string = string> {
  id: TabId;
  label: string;
  count?: number;
  panelId?: string;
  disabled?: boolean;
}

export interface WorkspaceTabsProps<TabId extends string = string> {
  ariaLabel: string;
  tabs: readonly WorkspaceTabItem<TabId>[];
  activeTab: TabId;
  onTabChange: (tabId: TabId) => void;
}

export function WorkspaceTabs<TabId extends string>({
  ariaLabel,
  tabs,
  activeTab,
  onTabChange,
}: WorkspaceTabsProps<TabId>) {
  const buttonRefs = useRef<Array<HTMLButtonElement | null>>([]);

  function activateTab(index: number): void {
    const tab = tabs[index];

    if (!tab || tab.disabled) {
      return;
    }

    onTabChange(tab.id);
    buttonRefs.current[index]?.focus();
  }

  function findEnabledTabIndex(startIndex: number, direction: 1 | -1): number {
    if (tabs.length === 0) {
      return -1;
    }

    let candidateIndex = startIndex;

    for (let iteration = 0; iteration < tabs.length; iteration++) {
      candidateIndex = (candidateIndex + direction + tabs.length) % tabs.length;

      if (!tabs[candidateIndex]?.disabled) {
        return candidateIndex;
      }
    }

    return -1;
  }

  function findFirstEnabledTabIndex(): number {
    return tabs.findIndex((tab) => !tab.disabled);
  }

  function findLastEnabledTabIndex(): number {
    return tabs.findLastIndex((tab) => !tab.disabled);
  }

  function handleKeyDown(
    event: KeyboardEvent<HTMLButtonElement>,
    currentIndex: number,
  ): void {
    let nextIndex = -1;

    if (event.key === "ArrowRight") {
      nextIndex = findEnabledTabIndex(currentIndex, 1);
    } else if (event.key === "ArrowLeft") {
      nextIndex = findEnabledTabIndex(currentIndex, -1);
    } else if (event.key === "Home") {
      nextIndex = findFirstEnabledTabIndex();
    } else if (event.key === "End") {
      nextIndex = findLastEnabledTabIndex();
    } else {
      return;
    }

    event.preventDefault();

    if (nextIndex >= 0) {
      activateTab(nextIndex);
    }
  }

  return (
    <div className="min-w-0 max-w-full overflow-x-auto overflow-y-hidden">
      <div
        role="tablist"
        aria-label={ariaLabel}
        className="flex min-w-max items-center gap-1 p-1"
      >
        {tabs.map((tab, index) => {
          const active = tab.id === activeTab;

          return (
            <button
              key={tab.id}
              ref={(element) => {
                buttonRefs.current[index] = element;
              }}
              type="button"
              role="tab"
              id={`workspace-tab-${tab.id}`}
              aria-selected={active}
              aria-controls={tab.panelId}
              tabIndex={active ? 0 : -1}
              disabled={tab.disabled}
              onClick={() => onTabChange(tab.id)}
              onKeyDown={(event) => handleKeyDown(event, index)}
              className={[
                "group relative flex min-h-10 items-center gap-2",
                "rounded-xl px-3.5 py-2 text-sm font-medium",
                "outline-none transition",
                "focus-visible:ring-2 focus-visible:ring-[var(--app-accent)]",
                "disabled:cursor-not-allowed disabled:opacity-40",
                active
                  ? "bg-[var(--app-accent-soft)] text-[var(--app-accent)]"
                  : "text-[var(--app-muted)] hover:bg-[var(--app-surface-hover)] hover:text-[var(--app-text)]",
              ].join(" ")}
            >
              <span>{tab.label}</span>

              {tab.count !== undefined && (
                <span
                  className={
                    active
                      ? "rounded-full bg-[var(--app-accent-soft)] px-2 py-0.5 text-xs text-[var(--app-accent)]"
                      : "rounded-full bg-[var(--app-surface)] px-2 py-0.5 text-xs text-[var(--app-subtle)] group-hover:text-[var(--app-muted)]"
                  }
                >
                  {tab.count}
                </span>
              )}

              {active && (
                <span
                  aria-hidden="true"
                  className="absolute inset-x-3 bottom-0.5 h-0.5 rounded-full bg-[var(--app-accent)]"
                />
              )}
            </button>
          );
        })}
      </div>
    </div>
  );
}
