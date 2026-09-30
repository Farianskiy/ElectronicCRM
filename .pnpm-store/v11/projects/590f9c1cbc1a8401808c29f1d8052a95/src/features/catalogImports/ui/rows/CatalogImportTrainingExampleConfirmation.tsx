"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { CatalogImportTrainingExampleRevocation } from "./CatalogImportTrainingExampleRevocation";
import { confirmCatalogImportTrainingExample } from "../../api/confirmCatalogImportTrainingExample";
import { catalogImportConfirmedSpansQueryKey } from "../../api/getCatalogImportRowConfirmedSpans";
import type {
  CatalogImportFeedbackSpan,
  ConfirmCatalogImportTrainingExampleRequest,
} from "../../model/types";

interface CatalogImportTrainingExampleConfirmationProps {
  batchId: string;
  rowId: string;
  characteristicName: string;
  saved: CatalogImportFeedbackSpan;
  productName: string;
  manufacturerId: string;
  productTypeId: string;
  currentValue: string;
  hasDrafts: boolean;
  disabled: boolean;
}

export function CatalogImportTrainingExampleConfirmation({
  batchId,
  rowId,
  characteristicName,
  saved,
  productName,
  manufacturerId,
  productTypeId,
  currentValue,
  hasDrafts,
  disabled,
}: CatalogImportTrainingExampleConfirmationProps) {
  const queryClient = useQueryClient();
  const access = useCurrentUserAccess();
  const canConfirm =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");
  const trainingExample = saved.trainingExample;

  const confirmationMutation = useMutation({
    mutationFn: (request: ConfirmCatalogImportTrainingExampleRequest) =>
      confirmCatalogImportTrainingExample(
        batchId,
        rowId,
        saved.characteristicDefinitionId,
        request,
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: catalogImportConfirmedSpansQueryKey(batchId, rowId),
      });
    },
  });

  const formMatchesSaved =
    productName.trim() === saved.productName &&
    manufacturerId === saved.manufacturerId &&
    productTypeId === saved.productTypeId &&
    currentValue.trim() === saved.finalNormalizedValue;
  const hasSavedSpan =
    Boolean(saved.confirmedRawValue?.trim()) &&
    saved.confirmedSpanStart !== null &&
    saved.confirmedSpanLength !== null &&
    saved.confirmedSpanLength > 0;
  const needsSave = hasDrafts || !formMatchesSaved;
  const alreadyConfirmed = Boolean(trainingExample);
  const cannotConfirm =
    disabled ||
    !canConfirm ||
    needsSave ||
    !hasSavedSpan ||
    alreadyConfirmed ||
    confirmationMutation.isPending ||
    confirmationMutation.isSuccess;

  function confirm(): void {
    if (
      cannotConfirm ||
      !saved.manufacturerId ||
      !saved.finalNormalizedValue ||
      !saved.confirmedRawValue ||
      saved.confirmedSpanStart === null ||
      saved.confirmedSpanLength === null
    ) {
      return;
    }

    confirmationMutation.mutate({
      productName: saved.productName,
      manufacturerId: saved.manufacturerId,
      productTypeId: saved.productTypeId,
      normalizedValue: saved.finalNormalizedValue,
      rawValue: saved.confirmedRawValue,
      spanStart: saved.confirmedSpanStart,
      spanLength: saved.confirmedSpanLength,
    });
  }

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
      <div>
        <h4 className="font-semibold text-[var(--app-text)]">
          Учебный пример: {characteristicName}
        </h4>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Подтверждается только выбранная характеристика. Импорт и остальные
          характеристики не подтверждаются.
        </p>
      </div>

      {hasSavedSpan && (
        <div className="grid gap-1 text-sm">
          <p>Сохранённое название: {saved.productName}</p>
          <p>Фрагмент: «{saved.confirmedRawValue}»</p>
          <p>Правильное значение: {saved.finalNormalizedValue}</p>
        </div>
      )}

      {!hasSavedSpan && (
        <p className="text-sm text-[var(--app-muted)]">
          Сначала выделите соответствующий фрагмент, свяжите его с
          характеристикой и сохраните строку.
        </p>
      )}

      {needsSave && (
        <p role="status" className="text-sm text-[var(--app-muted)]">
          В форме есть несохранённая разметка или выбранная характеристика
          отличается от сохранённых данных. Сначала нажмите «Сохранить строку»,
          затем снова откройте редактор.
        </p>
      )}

      {trainingExample && (
        <div className="grid gap-1 rounded-xl border border-[var(--app-border)] p-3 text-sm">
          <p className="font-semibold">Учебный пример подтверждён</p>
          <p>Подтверждённое название: {trainingExample.productName}</p>
          <p>
            Подтверждённый фрагмент: «{trainingExample.rawValue}» →{" "}
            {trainingExample.normalizedValue}
          </p>
          <p className="text-[var(--app-muted)]">
            Дата:{" "}
            {new Date(trainingExample.confirmedAtUtc).toLocaleString("ru-RU")}
          </p>
          {!trainingExample.matchesSavedFeedback && (
            <p className="text-[var(--app-danger)]">
              Сохранённая разметка уже отличается от подтверждённого примера.
              Старый пример не заменён. Перед новым подтверждением потребуется
              его отзыв.
            </p>
          )}
        </div>
      )}

      {canConfirm && trainingExample && (
        <CatalogImportTrainingExampleRevocation
          key={trainingExample.exampleId}
          batchId={batchId}
          rowId={rowId}
          exampleId={trainingExample.exampleId}
          disabled={disabled || confirmationMutation.isPending}
        />
      )}

      {access.isLoading && (
        <p className="text-sm text-[var(--app-muted)]">
          Проверяем права доступа...
        </p>
      )}

      {access.isError && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          Не удалось проверить права. Подтверждение временно недоступно.
        </p>
      )}

      {!access.isLoading && !access.isError && !canConfirm && (
        <p className="text-sm text-[var(--app-muted)]">
          Подтверждать учебные примеры можно с правом «Управление
          справочниками».
        </p>
      )}

      {confirmationMutation.isError && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            confirmationMutation.error,
            "Не удалось подтвердить учебный пример.",
          )}
        </p>
      )}

      {confirmationMutation.isSuccess && (
        <p role="status" className="text-sm text-[var(--app-text)]">
          Сервер подтвердил учебный пример. Генератор правил пока не
          запускается.
        </p>
      )}

      {canConfirm && !alreadyConfirmed && (
        <AppButton
          type="button"
          variant="secondary"
          disabled={cannotConfirm}
          onClick={confirm}
        >
          {confirmationMutation.isPending
            ? "Подтверждаем..."
            : "Подтвердить для обучения"}
        </AppButton>
      )}
    </section>
  );
}
