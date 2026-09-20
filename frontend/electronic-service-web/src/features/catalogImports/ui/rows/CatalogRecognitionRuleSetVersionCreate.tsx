"use client";

import { useRef, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import {
  createRecognitionRuleSetVersion,
  type CreateRecognitionRuleSetVersionRequest,
} from "../../api/createRecognitionRuleSetVersion";
import { getRecognitionLiteralDrafts } from "../../api/getRecognitionLiteralDrafts";
import { getRecognitionIntegerDrafts } from "../../api/getRecognitionIntegerDrafts";
import { getRecognitionMultiIntegerDrafts } from "../../api/getRecognitionMultiIntegerDrafts";
import { recognitionRuleSetVersionsQueryRoot } from "../../api/getRecognitionRuleSetVersions";

interface CatalogRecognitionRuleSetVersionCreateProps {
  manufacturerId: string;
  productTypeId: string;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  disabled: boolean;
}

type RuleKind = 1 | 2 | 3;

interface DraftOption {
  id: string;
  kind: RuleKind;
  description: string;
  createdAtUtc: string;
}

interface DraftOptionsPage {
  items: DraftOption[];
  hasMore: boolean;
}

function draftKey(draft: DraftOption): string {
  return `${draft.kind}:${draft.id}`;
}

function kindLabel(kind: RuleKind): string {
  switch (kind) {
    case 1:
      return "Точное соответствие";
    case 2:
      return "Одиночный числовой шаблон";
    case 3:
      return "Составной числовой шаблон";
  }
}

export function CatalogRecognitionRuleSetVersionCreate({
  manufacturerId,
  productTypeId,
  characteristics,
  disabled,
}: CatalogRecognitionRuleSetVersionCreateProps) {
  const [name, setName] = useState("");
  const [kind, setKind] = useState<RuleKind>(3);
  const [characteristicId, setCharacteristicId] = useState("");
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<DraftOption[]>([]);
  const [validationError, setValidationError] = useState<string | null>(null);
  const submitted = useRef(false);

  const access = useCurrentUserAccess();
  const session = useAuthSession();
  const queryClient = useQueryClient();

  const canCreate =
    Boolean(session?.userId) &&
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const selectedCharacteristic = characteristics.find(
    (item) => item.id === characteristicId,
  );

  const canLoad = kind === 3 || Boolean(selectedCharacteristic);

  const drafts = useQuery({
    queryKey: [
      "recognition-version-draft-picker",
      session?.userId,
      manufacturerId,
      productTypeId,
      kind,
      characteristicId,
      characteristics.map((item) => [item.id, item.name]),
      page,
    ],
    queryFn: async (): Promise<DraftOptionsPage> => {
      if (kind === 3) {
        const result = await getRecognitionMultiIntegerDrafts(
          manufacturerId,
          productTypeId,
          page,
        );

        return {
          hasMore: result.hasMore,
          items: result.items.map((draft) => ({
            id: draft.id,
            kind: 3,
            createdAtUtc: draft.createdAtUtc,
            description: draft.parts
              .map((part) => {
                if (!part.characteristicDefinitionId) {
                  return part.literal ?? "";
                }

                const definition = characteristics.find(
                  (item) => item.id === part.characteristicDefinitionId,
                );

                return `{${definition?.name ?? part.characteristicDefinitionId}}`;
              })
              .join(""),
          })),
        };
      }

      if (!selectedCharacteristic) {
        return { items: [], hasMore: false };
      }

      const scope = {
        manufacturerId,
        productTypeId,
        characteristicDefinitionId: characteristicId,
      };

      if (kind === 1) {
        const result = await getRecognitionLiteralDrafts(scope, page);

        return {
          hasMore: result.hasMore,
          items: result.items.map((draft) => ({
            id: draft.id,
            kind: 1,
            createdAtUtc: draft.createdAtUtc,
            description:
              `${selectedCharacteristic.name}: ` +
              `«${draft.literal}» → ${draft.normalizedValue}`,
          })),
        };
      }

      const result = await getRecognitionIntegerDrafts(scope, page);

      return {
        hasMore: result.hasMore,
        items: result.items.map((draft) => ({
          id: draft.id,
          kind: 2,
          createdAtUtc: draft.createdAtUtc,
          description:
            `${selectedCharacteristic.name}:\n` +
            draft.suffixes
              .map((suffix) => `${draft.prefix}{число}${suffix}`)
              .join("\n"),
        })),
      };
    },
    enabled: canCreate && !disabled && canLoad,
    staleTime: 0,
    retry: false,
  });

  const creation = useMutation({
    mutationFn: (request: CreateRecognitionRuleSetVersionRequest) =>
      createRecognitionRuleSetVersion(request),
    retry: false,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: recognitionRuleSetVersionsQueryRoot,
      });
    },
  });

  const locked = disabled || !creation.isIdle;

  function toggle(draft: DraftOption): void {
    if (locked || submitted.current) {
      return;
    }

    setSelected((current) => {
      const key = draftKey(draft);

      if (current.some((item) => draftKey(item) === key)) {
        return current.filter((item) => draftKey(item) !== key);
      }

      return current.length >= 100 ? current : [...current, draft];
    });

    setValidationError(null);
  }

  function create(): void {
    if (!canCreate || locked || submitted.current) {
      return;
    }

    const versionName = name.trim();

    if (versionName.length === 0 || versionName.length > 200) {
      setValidationError("Укажите название версии от 1 до 200 символов.");
      return;
    }

    if (selected.length === 0 || selected.length > 100) {
      setValidationError("Выберите от 1 до 100 черновиков.");
      return;
    }

    setValidationError(null);
    submitted.current = true;

    creation.mutate({
      manufacturerId,
      productTypeId,
      name: versionName,
      entries: selected.map((draft) => ({
        kind: draft.kind,
        draftId: draft.id,
      })),
    });
  }

  if (!canCreate) {
    return null;
  }

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4 text-sm">
      <h4 className="font-semibold">Создать версию набора правил</h4>

      <p className="text-[var(--app-muted)]">
        Можно объединить черновики всех трёх видов. Выбранные элементы
        сохраняются при переключении вида, характеристики и страницы. Создание
        версии не проверяет совместную работу правил и не активирует их.
      </p>

      <label className="grid gap-2">
        <span>Название версии</span>
        <input
          value={name}
          maxLength={200}
          disabled={locked}
          onChange={(event) => {
            setName(event.target.value);
            setValidationError(null);
          }}
          className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-[var(--app-text)]"
        />
      </label>

      <label className="grid gap-2">
        <span>Вид черновиков</span>
        <select
          value={kind}
          disabled={locked}
          onChange={(event) => {
            const value = Number(event.target.value);

            if (value === 1 || value === 2 || value === 3) {
              setKind(value);
              setPage(1);
            }
          }}
          className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-[var(--app-text)]"
        >
          <option value={1}>Точные соответствия</option>
          <option value={2}>Одиночные числовые шаблоны</option>
          <option value={3}>Составные числовые шаблоны</option>
        </select>
      </label>

      {kind !== 3 && (
        <label className="grid gap-2">
          <span>Характеристика</span>
          <select
            value={characteristicId}
            disabled={locked}
            onChange={(event) => {
              setCharacteristicId(event.target.value);
              setPage(1);
            }}
            className="rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] px-3 py-2 text-[var(--app-text)]"
          >
            <option value="">Выберите характеристику</option>
            {characteristics.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </label>
      )}

      {!disabled && canLoad && (
        <>
          <AppButton
            type="button"
            variant="secondary"
            disabled={locked || drafts.isFetching}
            onClick={() => void drafts.refetch()}
          >
            Обновить доступные черновики
          </AppButton>

          {drafts.isFetching && <p role="status">Загружаем черновики...</p>}

          {drafts.isError && (
            <p role="alert" className="text-[var(--app-danger)]">
              {getApiErrorMessage(
                drafts.error,
                "Не удалось загрузить черновики.",
              )}
            </p>
          )}

          {drafts.isSuccess && !drafts.isFetching && (
            <fieldset disabled={locked} className="grid gap-2">
              <legend className="mb-2">Доступные черновики</legend>

              {drafts.data.items.length === 0 && (
                <p>На этой странице нет сохранённых черновиков.</p>
              )}

              {drafts.data.items.map((draft) => {
                const checked = selected.some(
                  (item) => draftKey(item) === draftKey(draft),
                );

                return (
                  <label
                    key={draftKey(draft)}
                    className="flex items-start gap-2 rounded-lg border border-[var(--app-border)] p-3"
                  >
                    <input
                      type="checkbox"
                      checked={checked}
                      disabled={locked || (!checked && selected.length >= 100)}
                      onChange={() => toggle(draft)}
                    />
                    <span className="grid min-w-0 gap-1">
                      <span className="whitespace-pre-wrap break-all">
                        {draft.description}
                      </span>
                      <span className="text-[var(--app-muted)]">
                        {new Date(draft.createdAtUtc).toLocaleString("ru-RU")}
                      </span>
                      <span className="break-all text-[var(--app-muted)]">
                        {draft.id}
                      </span>
                    </span>
                  </label>
                );
              })}
            </fieldset>
          )}

          <div className="flex items-center gap-2">
            <AppButton
              type="button"
              variant="secondary"
              disabled={locked || drafts.isFetching || page <= 1}
              onClick={() => setPage((current) => current - 1)}
            >
              Назад
            </AppButton>

            <span>Страница {page}</span>

            <AppButton
              type="button"
              variant="secondary"
              disabled={
                locked ||
                drafts.isFetching ||
                !drafts.isSuccess ||
                !drafts.data.hasMore ||
                page >= 10000
              }
              onClick={() => setPage((current) => current + 1)}
            >
              Далее
            </AppButton>
          </div>
        </>
      )}

      {!canLoad && (
        <p className="text-[var(--app-muted)]">
          Выберите характеристику для загрузки её черновиков.
        </p>
      )}

      <p className="font-medium">Выбрано черновиков: {selected.length} / 100</p>

      {selected.map((draft) => (
        <div
          key={draftKey(draft)}
          className="grid gap-2 rounded-lg border border-[var(--app-border)] p-2"
        >
          <p className="font-medium">{kindLabel(draft.kind)}</p>
          <p className="whitespace-pre-wrap break-all">{draft.description}</p>
          <AppButton
            type="button"
            variant="secondary"
            disabled={locked}
            onClick={() => toggle(draft)}
          >
            Убрать из состава
          </AppButton>
        </div>
      ))}

      {validationError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {validationError}
        </p>
      )}

      <AppButton
        type="button"
        variant="secondary"
        disabled={locked || selected.length === 0 || name.trim().length === 0}
        onClick={create}
      >
        {creation.isPending
          ? "Сохраняем версию..."
          : "Создать неактивную версию"}
      </AppButton>

      {creation.isError && (
        <div role="alert" className="grid gap-2 text-[var(--app-danger)]">
          <p>
            {getApiErrorMessage(
              creation.error,
              "Не удалось получить подтверждение создания версии.",
            )}
          </p>
          <p>
            Повторная отправка заблокирована: сервер мог успеть сохранить
            версию. Обновите список версий и проверьте название и состав. Если
            версия не создана, заново откройте редактор для новой попытки.
          </p>
        </div>
      )}

      {creation.isSuccess && (
        <div role="status" className="grid gap-1">
          <p className="font-semibold">
            Создана версия {creation.data.versionNumber}.
          </p>
          <p className="break-all">Идентификатор: {creation.data.id}</p>
          <p>Версия не активирована. Данные импорта не изменены.</p>
        </div>
      )}
    </section>
  );
}
