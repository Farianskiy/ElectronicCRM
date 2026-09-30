"use client";

import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { revokeCatalogImportTrainingExample } from "../../api/revokeCatalogImportTrainingExample";
import { catalogImportConfirmedSpansQueryKey } from "../../api/getCatalogImportRowConfirmedSpans";
import type { CatalogImportFeedbackSpan } from "../../model/types";

interface CatalogImportTrainingExampleRevocationProps {
  batchId: string;
  rowId: string;
  exampleId: string;
  disabled: boolean;
}

export function CatalogImportTrainingExampleRevocation({
  batchId,
  rowId,
  exampleId,
  disabled,
}: CatalogImportTrainingExampleRevocationProps) {
  const queryClient = useQueryClient();
  const [isConfirming, setIsConfirming] = useState(false);
  const [reason, setReason] = useState("");

  const mutation = useMutation({
    mutationFn: () =>
      revokeCatalogImportTrainingExample(batchId, rowId, exampleId, reason.trim()),
    onSuccess: async () => {
      const queryKey = catalogImportConfirmedSpansQueryKey(batchId, rowId);

      queryClient.setQueryData<CatalogImportFeedbackSpan[]>(
        queryKey,
        (entries) =>
          entries?.map((entry) =>
            entry.trainingExample?.exampleId === exampleId
              ? { ...entry, trainingExample: null }
              : entry,
          ),
      );

      await queryClient.invalidateQueries({ queryKey });
      await queryClient.invalidateQueries({ queryKey: ["training-examples"] });
    },
  });

  function revoke(): void {
    if (disabled || mutation.isPending || mutation.isSuccess || !reason.trim()) {
      return;
    }

    mutation.mutate();
  }

  return (
    <div className="grid gap-3 rounded-xl border border-[var(--app-border)] p-3">
      {!isConfirming && (
        <AppButton
          type="button"
          variant="secondary"
          disabled={disabled}
          onClick={() => setIsConfirming(true)}
        >
          Отозвать подтверждение примера
        </AppButton>
      )}

      {isConfirming && (
        <>
          <p className="text-sm text-[var(--app-muted)]">
            Отозвать этот учебный пример? Запись останется в истории, но
            перестанет быть действующим подтверждённым примером. Разметка строки
            не изменится. Проверенная обратная связь и действующие правила сохранятся;
            изменение правил требует отдельной проверки и переключения.
          </p>
          <label>Причина отзыва
            <textarea className="block w-full rounded border border-[var(--app-border)] bg-transparent p-2" maxLength={1000} value={reason} onChange={event => setReason(event.target.value)} />
          </label>
          <div className="flex flex-wrap gap-2">
            <AppButton
              type="button"
              variant="secondary"
              disabled={disabled || mutation.isPending || mutation.isSuccess || !reason.trim()}
              onClick={revoke}
            >
              {mutation.isPending ? "Отзываем..." : "Да, отозвать"}
            </AppButton>
            <AppButton
              type="button"
              variant="secondary"
              disabled={mutation.isPending}
              onClick={() => {
                setIsConfirming(false);
                mutation.reset();
              }}
            >
              Отмена
            </AppButton>
          </div>
        </>
      )}

      {mutation.isError && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось отозвать подтверждение.",
          )}
        </p>
      )}
    </div>
  );
}
