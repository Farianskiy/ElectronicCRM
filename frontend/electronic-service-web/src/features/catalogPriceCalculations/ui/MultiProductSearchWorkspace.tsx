"use client";

import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useMemo, useState, type FormEvent } from "react";
import { searchCatalogPriceCalculationProducts } from "../api/catalogPriceCalculationEditorApi";
import { catalogPriceCalculationQueryKeys } from "../model/queryKeys";
import type { CatalogPriceCalculationProductSearchItem } from "../model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { formatPrice } from "@/shared/lib/formatters";
import { AppButton } from "@/shared/ui/AppButton";
import { AppInput } from "@/shared/ui/AppInput";

const pageSize = 20;
const maximumWorkspaceCount = 10;
const bulkSearchConcurrency = 3;
const storageVersion = 1;

interface ProductSearchWorkspaceState {
  id: string;
  draftSearch: string;
  appliedSearch: string;
  page: number;
  collapsed: boolean;
}

interface StoredWorkspaceState {
  version: number;
  workspaces: ProductSearchWorkspaceState[];
}

interface MultiProductSearchWorkspaceProps {
  calculationId: string;
  currency: string;
  existingProductIds: ReadonlySet<string>;
  canCreateProducts: boolean;
  addingProductId: string | null;
  onAdd: (productId: string, quantity: number) => void;
  onCreateProduct: (search: string) => void;
}

function createWorkspace(
  initial?: Partial<Omit<ProductSearchWorkspaceState, "id">>,
): ProductSearchWorkspaceState {
  return {
    id:
      typeof crypto !== "undefined" && "randomUUID" in crypto
        ? crypto.randomUUID()
        : `search-${Date.now()}-${Math.random().toString(16).slice(2)}`,
    draftSearch: initial?.draftSearch ?? "",
    appliedSearch: initial?.appliedSearch ?? "",
    page: initial?.page ?? 1,
    collapsed: initial?.collapsed ?? false,
  };
}

function isWorkspaceState(value: unknown): value is ProductSearchWorkspaceState {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const candidate = value as Partial<ProductSearchWorkspaceState>;

  return (
    typeof candidate.id === "string" &&
    typeof candidate.draftSearch === "string" &&
    typeof candidate.appliedSearch === "string" &&
    typeof candidate.page === "number" &&
    Number.isInteger(candidate.page) &&
    candidate.page >= 1 &&
    typeof candidate.collapsed === "boolean"
  );
}

function parseStoredWorkspaces(value: string | null): ProductSearchWorkspaceState[] {
  if (!value) {
    return [];
  }

  try {
    const parsed = JSON.parse(value) as Partial<StoredWorkspaceState>;

    if (
      parsed.version !== storageVersion ||
      !Array.isArray(parsed.workspaces)
    ) {
      return [];
    }

    return parsed.workspaces
      .filter(isWorkspaceState)
      .slice(0, maximumWorkspaceCount);
  } catch {
    return [];
  }
}

function getWorkspaceTitle(
  workspace: ProductSearchWorkspaceState,
  index: number,
): string {
  const search = workspace.appliedSearch || workspace.draftSearch;
  return search.length > 0 ? search : `Поиск ${index + 1}`;
}

function ProductSearchResult({
  product,
  currency,
  alreadyAdded,
  addingProductId,
  onAdd,
}: {
  product: CatalogPriceCalculationProductSearchItem;
  currency: string;
  alreadyAdded: boolean;
  addingProductId: string | null;
  onAdd: (productId: string, quantity: number) => void;
}) {
  const [quantity, setQuantity] = useState("1");
  const parsedQuantity = Number(quantity.replace(",", "."));
  const hasPrice = product.priceStatus === "Available";
  const isAdding = addingProductId === product.productId;
  const unavailableReason =
    product.priceStatus === "ActivePriceAmbiguous"
      ? "Найдено несколько активных цен"
      : "Нет активной цены";

  return (
    <li className="rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
      <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
        <div className="min-w-0">
          <p className="font-medium text-[var(--app-text)]">{product.name}</p>
          <p className="mt-1 text-xs text-[var(--app-muted)]">
            {product.article} · {product.manufacturerName}
          </p>
          <div className="mt-2 flex flex-wrap gap-x-4 gap-y-1 text-xs text-[var(--app-muted)]">
            <span>
              Базовая цена: {product.basePriceAmount === null
                ? "—"
                : formatPrice(product.basePriceAmount, currency)}
            </span>
            <span>
              МРЦ: {product.mrcPriceAmount === null || product.mrcPriceAmount === undefined
                ? "—"
                : formatPrice(product.mrcPriceAmount, currency)}
            </span>
          </div>
          <p className="mt-1 text-xs text-[var(--app-muted)]">
            {product.priceListRowId
              ? "Цена из активного прайса"
              : "Цена из карточки каталога"}
          </p>
        </div>

        {alreadyAdded ? (
          <span className="inline-flex w-fit shrink-0 rounded-xl border border-[var(--app-success-border)] bg-[var(--app-success-soft)] px-3 py-2 text-xs font-medium text-[var(--app-success)]">
            Уже в расчёте
          </span>
        ) : hasPrice ? (
          <div className="flex shrink-0 items-center gap-2">
            <AppInput
              type="number"
              min="0.001"
              step="0.001"
              value={quantity}
              aria-label={`Количество ${product.name} в проекте`}
              disabled={addingProductId !== null}
              onChange={(event) => setQuantity(event.target.value)}
              className="w-24"
            />
            <AppButton
              type="button"
              variant="primary"
              size="sm"
              loading={isAdding}
              disabled={
                addingProductId !== null ||
                !Number.isFinite(parsedQuantity) ||
                parsedQuantity <= 0
              }
              onClick={() => onAdd(product.productId, parsedQuantity)}
            >
              Добавить
            </AppButton>
          </div>
        ) : (
          <span className="inline-flex w-fit max-w-56 shrink-0 rounded-xl border border-[var(--app-warning-border)] bg-[var(--app-warning-soft)] px-3 py-2 text-xs font-medium leading-5 text-[var(--app-warning)]">
            {unavailableReason}
          </span>
        )}
      </div>
    </li>
  );
}

function ProductSearchWorkspace({
  calculationId,
  currency,
  workspace,
  index,
  existingProductIds,
  addingProductId,
  searchesSuspended,
  canCreateProducts,
  onChange,
  onDuplicate,
  onClose,
  onAdd,
  onCreateProduct,
}: {
  calculationId: string;
  currency: string;
  workspace: ProductSearchWorkspaceState;
  index: number;
  existingProductIds: ReadonlySet<string>;
  addingProductId: string | null;
  searchesSuspended: boolean;
  canCreateProducts: boolean;
  onChange: (changes: Partial<Omit<ProductSearchWorkspaceState, "id">>) => void;
  onDuplicate: () => void;
  onClose: () => void;
  onAdd: (productId: string, quantity: number) => void;
  onCreateProduct: (search: string) => void;
}) {
  const [copyStatus, setCopyStatus] = useState<"idle" | "copied" | "failed">("idle");
  const query = useQuery({
    queryKey: catalogPriceCalculationQueryKeys.products(
      calculationId,
      workspace.appliedSearch,
      workspace.page,
      pageSize,
    ),
    queryFn: () =>
      searchCatalogPriceCalculationProducts({
        calculationId,
        search: workspace.appliedSearch,
        page: workspace.page,
        pageSize,
      }),
    enabled:
      !searchesSuspended &&
      !workspace.collapsed &&
      workspace.appliedSearch.length > 0,
    placeholderData: (previousData) => previousData,
    staleTime: 30_000,
  });
  const totalPages = Math.max(1, query.data?.totalPages ?? 0);
  const isDirty =
    workspace.draftSearch.trim() !== workspace.appliedSearch.trim();

  function handleSubmit(event: FormEvent<HTMLFormElement>): void {
    event.preventDefault();
    const normalizedSearch = workspace.draftSearch.trim();

    if (normalizedSearch.length === 0) {
      return;
    }

    onChange({ appliedSearch: normalizedSearch, page: 1, collapsed: false });
  }

  async function handleCopy(): Promise<void> {
    const value = workspace.draftSearch.trim();

    if (value.length === 0) {
      return;
    }

    try {
      await navigator.clipboard.writeText(value);
      setCopyStatus("copied");
    } catch {
      setCopyStatus("failed");
    }

    window.setTimeout(() => setCopyStatus("idle"), 1800);
  }

  return (
    <article className="min-w-0 rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-4 shadow-sm shadow-[var(--app-shadow)] sm:p-5">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <p className="text-xs font-medium uppercase tracking-wide text-[var(--app-muted)]">
            Поиск {index + 1}
          </p>
          <h3 className="mt-1 truncate font-semibold text-[var(--app-text)]">
            {getWorkspaceTitle(workspace, index)}
          </h3>
          {query.data && (
            <p className="mt-1 text-xs text-[var(--app-muted)]">
              Найдено товаров: {query.data.totalCount}
            </p>
          )}
        </div>

        <div className="flex flex-wrap gap-2">
          <AppButton
            type="button"
            variant="secondary"
            size="sm"
            onClick={() => onChange({ collapsed: !workspace.collapsed })}
          >
            {workspace.collapsed ? "Развернуть" : "Свернуть"}
          </AppButton>
          <AppButton type="button" variant="secondary" size="sm" onClick={onClose}>
            Закрыть
          </AppButton>
        </div>
      </div>

      {!workspace.collapsed && (
        <>
          <form onSubmit={handleSubmit} className="mt-4 grid gap-2 sm:grid-cols-[minmax(0,1fr)_auto]">
            <AppInput
              value={workspace.draftSearch}
              onChange={(event) => onChange({ draftSearch: event.target.value })}
              placeholder="Артикул или наименование товара"
            />
            <AppButton
              type="submit"
              variant="primary"
              disabled={workspace.draftSearch.trim().length === 0 || searchesSuspended}
            >
              Найти
            </AppButton>
          </form>

          <div className="mt-3 flex flex-wrap items-center gap-2">
            <AppButton
              type="button"
              variant="secondary"
              size="sm"
              disabled={workspace.draftSearch.trim().length === 0}
              onClick={() => void handleCopy()}
            >
              Копировать запрос
            </AppButton>
            <AppButton type="button" variant="secondary" size="sm" onClick={onDuplicate}>
              Дублировать
            </AppButton>
            <AppButton
              type="button"
              variant="secondary"
              size="sm"
              disabled={workspace.draftSearch.length === 0 && workspace.appliedSearch.length === 0}
              onClick={() =>
                onChange({ draftSearch: "", appliedSearch: "", page: 1 })
              }
            >
              Очистить
            </AppButton>
            {isDirty && workspace.appliedSearch.length > 0 && (
              <span className="text-xs text-[var(--app-warning)]">
                Запрос изменён — нажмите «Найти»
              </span>
            )}
            {copyStatus === "copied" && (
              <span className="text-xs text-[var(--app-success)]">Запрос скопирован</span>
            )}
            {copyStatus === "failed" && (
              <span className="text-xs text-[var(--app-danger)]">Не удалось скопировать</span>
            )}
          </div>

          {query.isError && (
            <div role="alert" className="mt-4 rounded-2xl border border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] p-4 text-sm text-[var(--app-danger)]">
              {getApiErrorMessage(query.error, "Не удалось выполнить поиск товаров.")}
              <AppButton
                type="button"
                variant="secondary"
                size="sm"
                className="mt-3"
                onClick={() => void query.refetch()}
              >
                Повторить
              </AppButton>
            </div>
          )}

          {query.isLoading && (
            <p role="status" className="mt-4 text-sm text-[var(--app-muted)]">
              Ищем товары...
            </p>
          )}

          {query.data && query.data.items.length > 0 && (
            <>
              <ul className="mt-4 grid max-h-[560px] gap-3 overflow-y-auto pr-1">
                {query.data.items.map((product) => (
                  <ProductSearchResult
                    key={product.productId}
                    product={product}
                    currency={currency}
                    alreadyAdded={existingProductIds.has(product.productId)}
                    addingProductId={addingProductId}
                    onAdd={onAdd}
                  />
                ))}
              </ul>

              <nav className="mt-4 flex items-center justify-between gap-3">
                <AppButton
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={workspace.page <= 1 || query.isFetching}
                  onClick={() => onChange({ page: Math.max(1, workspace.page - 1) })}
                >
                  Назад
                </AppButton>
                <span className="text-xs text-[var(--app-muted)]">
                  Страница {workspace.page} из {totalPages}
                </span>
                <AppButton
                  type="button"
                  variant="secondary"
                  size="sm"
                  disabled={workspace.page >= totalPages || query.isFetching}
                  onClick={() =>
                    onChange({ page: Math.min(totalPages, workspace.page + 1) })
                  }
                >
                  Вперёд
                </AppButton>
              </nav>
            </>
          )}

          {query.data &&
            query.data.items.length === 0 &&
            workspace.appliedSearch.length > 0 && (
              <div className="mt-4 rounded-2xl border border-dashed border-[var(--app-border-strong)] p-4">
                <p className="text-sm text-[var(--app-muted)]">
                  В каталоге подходящие товары не найдены.
                </p>
                {canCreateProducts && (
                  <AppButton
                    type="button"
                    variant="secondary"
                    size="sm"
                    className="mt-3"
                    onClick={() => onCreateProduct(workspace.appliedSearch)}
                  >
                    Добавить товар в каталог
                  </AppButton>
                )}
              </div>
            )}
        </>
      )}
    </article>
  );
}

export function MultiProductSearchWorkspace({
  calculationId,
  currency,
  existingProductIds,
  canCreateProducts,
  addingProductId,
  onAdd,
  onCreateProduct,
}: MultiProductSearchWorkspaceProps) {
  const queryClient = useQueryClient();
  const storageKey = `price-calculation-searches:${calculationId}`;
  const [workspaces, setWorkspaces] = useState<ProductSearchWorkspaceState[]>([
    createWorkspace(),
  ]);
  const [hydrated, setHydrated] = useState(false);
  const [showBulkInput, setShowBulkInput] = useState(false);
  const [bulkInput, setBulkInput] = useState("");
  const [bulkRunning, setBulkRunning] = useState(false);
  const [bulkFailedCount, setBulkFailedCount] = useState(0);

  useEffect(() => {
    let cancelled = false;
    const timeoutId = window.setTimeout(() => {
      if (cancelled) {
        return;
      }

      const stored = parseStoredWorkspaces(
        window.localStorage.getItem(storageKey),
      );
      setWorkspaces(stored.length > 0 ? stored : [createWorkspace()]);
      setHydrated(true);
    }, 0);

    return () => {
      cancelled = true;
      window.clearTimeout(timeoutId);
    };
  }, [storageKey]);

  useEffect(() => {
    if (!hydrated) {
      return;
    }

    const stored: StoredWorkspaceState = {
      version: storageVersion,
      workspaces,
    };
    window.localStorage.setItem(storageKey, JSON.stringify(stored));
  }, [hydrated, storageKey, workspaces]);

  const parsedBulkLines = useMemo(
    () =>
      bulkInput
        .split(/\r?\n/u)
        .map((line) => line.trim())
        .filter((line) => line.length > 0),
    [bulkInput],
  );
  const uniqueBulkLines = useMemo(
    () => Array.from(new Set(parsedBulkLines)),
    [parsedBulkLines],
  );
  const duplicateBulkLinesCount = parsedBulkLines.length - uniqueBulkLines.length;
  const canReuseOnlyWorkspace =
    workspaces.length === 1 &&
    workspaces[0].draftSearch.length === 0 &&
    workspaces[0].appliedSearch.length === 0;
  const availableWorkspaceSlots =
    maximumWorkspaceCount - workspaces.length + (canReuseOnlyWorkspace ? 1 : 0);

  function updateWorkspace(
    workspaceId: string,
    changes: Partial<Omit<ProductSearchWorkspaceState, "id">>,
  ): void {
    setWorkspaces((current) =>
      current.map((workspace) =>
        workspace.id === workspaceId ? { ...workspace, ...changes } : workspace,
      ),
    );
  }

  function addWorkspace(): void {
    if (workspaces.length >= maximumWorkspaceCount) {
      return;
    }

    setWorkspaces((current) => [...current, createWorkspace()]);
  }

  function duplicateWorkspace(workspaceId: string): void {
    if (workspaces.length >= maximumWorkspaceCount) {
      return;
    }

    setWorkspaces((current) => {
      const sourceIndex = current.findIndex((workspace) => workspace.id === workspaceId);
      const source = current[sourceIndex];

      if (!source) {
        return current;
      }

      const duplicate = createWorkspace({ draftSearch: source.draftSearch });
      const next = [...current];
      next.splice(sourceIndex + 1, 0, duplicate);
      return next;
    });
  }

  function closeWorkspace(workspaceId: string): void {
    const workspace = workspaces.find((item) => item.id === workspaceId);

    if (!workspace) {
      return;
    }

    const hasUnsavedText =
      workspace.draftSearch.trim().length > 0 &&
      workspace.draftSearch.trim() !== workspace.appliedSearch.trim();

    if (
      hasUnsavedText &&
      !window.confirm("Закрыть окно и потерять невыполненный запрос?")
    ) {
      return;
    }

    setWorkspaces((current) => {
      if (current.length === 1) {
        return [createWorkspace()];
      }

      return current.filter((item) => item.id !== workspaceId);
    });
  }

  function addBulkWorkspaces(): void {
    if (uniqueBulkLines.length === 0 || availableWorkspaceSlots <= 0) {
      return;
    }

    const lines = uniqueBulkLines.slice(0, availableWorkspaceSlots);

    setWorkspaces((current) => {
      const additions = lines.map((line) =>
        createWorkspace({ draftSearch: line, collapsed: true }),
      );

      return canReuseOnlyWorkspace ? additions : [...current, ...additions];
    });
    setBulkInput("");
    setShowBulkInput(false);
  }

  async function runAllSearches(): Promise<void> {
    const tasks = workspaces
      .map((workspace) => ({
        workspace,
        search: workspace.draftSearch.trim(),
      }))
      .filter((task) => task.search.length > 0);

    if (tasks.length === 0) {
      return;
    }

    setBulkRunning(true);
    setBulkFailedCount(0);
    setWorkspaces((current) =>
      current.map((workspace) => {
        const search = workspace.draftSearch.trim();
        return search.length > 0
          ? { ...workspace, appliedSearch: search, page: 1 }
          : workspace;
      }),
    );

    let nextTaskIndex = 0;
    let failedCount = 0;

    async function worker(): Promise<void> {
      while (nextTaskIndex < tasks.length) {
        const task = tasks[nextTaskIndex];
        nextTaskIndex += 1;

        try {
          await queryClient.fetchQuery({
            queryKey: catalogPriceCalculationQueryKeys.products(
              calculationId,
              task.search,
              1,
              pageSize,
            ),
            queryFn: () =>
              searchCatalogPriceCalculationProducts({
                calculationId,
                search: task.search,
                page: 1,
                pageSize,
              }),
            staleTime: 30_000,
          });
        } catch {
          failedCount += 1;
        }
      }
    }

    await Promise.all(
      Array.from(
        { length: Math.min(bulkSearchConcurrency, tasks.length) },
        () => worker(),
      ),
    );

    setBulkFailedCount(failedCount);
    setBulkRunning(false);
  }

  function clearAllWorkspaces(): void {
    const hasContent = workspaces.some(
      (workspace) =>
        workspace.draftSearch.length > 0 || workspace.appliedSearch.length > 0,
    );

    if (hasContent && !window.confirm("Очистить все поисковые окна?")) {
      return;
    }

    setWorkspaces([createWorkspace()]);
  }

  return (
    <section aria-labelledby="product-search-title" className="grid gap-4">
      <div className="rounded-3xl border border-[var(--app-border)] bg-[var(--app-panel)] p-5 sm:p-6">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <h2 id="product-search-title" className="text-xl font-semibold text-[var(--app-text)]">
              Поиск товаров
            </h2>
            <p className="mt-2 max-w-3xl text-sm leading-6 text-[var(--app-muted)]">
              Используйте несколько независимых окон, чтобы сравнивать товары и добавлять позиции в расчёт, не теряя предыдущие запросы.
            </p>
          </div>

          <div className="flex flex-wrap gap-2">
            <AppButton
              type="button"
              variant="primary"
              size="sm"
              disabled={workspaces.length >= maximumWorkspaceCount}
              onClick={addWorkspace}
            >
              Новый поиск
            </AppButton>
            <AppButton
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => setShowBulkInput((current) => !current)}
            >
              Вставить список
            </AppButton>
            <AppButton
              type="button"
              variant="secondary"
              size="sm"
              loading={bulkRunning}
              disabled={bulkRunning || !workspaces.some((workspace) => workspace.draftSearch.trim().length > 0)}
              onClick={() => void runAllSearches()}
            >
              Запустить все
            </AppButton>
            <AppButton type="button" variant="secondary" size="sm" onClick={clearAllWorkspaces}>
              Очистить все
            </AppButton>
          </div>
        </div>

        <div className="mt-4 flex flex-wrap gap-3 text-xs text-[var(--app-muted)]">
          <span>Открыто окон: {workspaces.length} из {maximumWorkspaceCount}</span>
          <span>Одновременно выполняется не более {bulkSearchConcurrency} запросов</span>
          {workspaces.length >= maximumWorkspaceCount && (
            <span className="text-[var(--app-warning)]">Достигнут лимит поисковых окон</span>
          )}
          {bulkFailedCount > 0 && (
            <span className="text-[var(--app-danger)]">
              Не удалось выполнить запросов: {bulkFailedCount}
            </span>
          )}
        </div>

        {showBulkInput && (
          <div className="mt-5 rounded-2xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
            <label className="grid gap-2">
              <span className="text-sm font-medium text-[var(--app-text)]">
                По одному запросу в строке
              </span>
              <textarea
                value={bulkInput}
                rows={7}
                onChange={(event) => setBulkInput(event.target.value)}
                placeholder={"mccb45-2000-1000v\nкорпус ВРУ 2000 800 600\nпереключатель для вольтметра"}
                className="w-full rounded-xl border border-[var(--app-input-border)] bg-[var(--app-input-bg)] px-4 py-3 text-sm text-[var(--app-text)] outline-none placeholder:text-[var(--app-muted)] focus:border-[var(--app-accent)]"
              />
            </label>
            <div className="mt-3 flex flex-wrap items-center justify-between gap-3">
              <p className="text-xs text-[var(--app-muted)]">
                Уникальных запросов: {uniqueBulkLines.length}. Доступно окон: {availableWorkspaceSlots}.
                {duplicateBulkLinesCount > 0 && ` Повторов пропущено: ${duplicateBulkLinesCount}.`}
              </p>
              <div className="flex gap-2">
                <AppButton type="button" variant="secondary" size="sm" onClick={() => setShowBulkInput(false)}>
                  Отмена
                </AppButton>
                <AppButton
                  type="button"
                  variant="primary"
                  size="sm"
                  disabled={uniqueBulkLines.length === 0 || availableWorkspaceSlots <= 0}
                  onClick={addBulkWorkspaces}
                >
                  Создать окна
                </AppButton>
              </div>
            </div>
          </div>
        )}
      </div>

      <div className="grid items-start gap-4 [grid-template-columns:repeat(auto-fit,minmax(min(100%,34rem),1fr))]">
        {workspaces.map((workspace, index) => (
          <ProductSearchWorkspace
            key={workspace.id}
            calculationId={calculationId}
            currency={currency}
            workspace={workspace}
            index={index}
            existingProductIds={existingProductIds}
            addingProductId={addingProductId}
            searchesSuspended={bulkRunning}
            canCreateProducts={canCreateProducts}
            onChange={(changes) => updateWorkspace(workspace.id, changes)}
            onDuplicate={() => duplicateWorkspace(workspace.id)}
            onClose={() => closeWorkspace(workspace.id)}
            onAdd={onAdd}
            onCreateProduct={onCreateProduct}
          />
        ))}
      </div>
    </section>
  );
}
