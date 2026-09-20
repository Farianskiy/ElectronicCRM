"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { recognitionIntegerDraftsQueryRoot } from "../../api/getRecognitionIntegerDrafts";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { saveRecognitionIntegerDraft } from "../../api/saveRecognitionIntegerDraft";
import type { RecognitionTrainingScope } from "../../model/recognitionLiteralProposals";
import type { RecognitionIntegerPatternProposal } from "../../model/recognitionIntegerPatterns";

interface CatalogRecognitionIntegerDraftSaveButtonProps {
  scope: RecognitionTrainingScope;
  generatorVersion: string;
  proposal: RecognitionIntegerPatternProposal;
  disabled: boolean;
}

export function CatalogRecognitionIntegerDraftSaveButton({
  scope,
  generatorVersion,
  proposal,
  disabled,
}: CatalogRecognitionIntegerDraftSaveButtonProps) {
  const access = useCurrentUserAccess();
  const queryClient = useQueryClient();
  const canSave =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: () =>
      saveRecognitionIntegerDraft({
        manufacturerId: scope.manufacturerId,
        productTypeId: scope.productTypeId,
        characteristicDefinitionId: scope.characteristicDefinitionId,
        prefix: proposal.pattern.prefix,
        suffixes: proposal.pattern.suffixes,
        generatorVersion,
      }),
    retry: false,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: recognitionIntegerDraftsQueryRoot,
      });
    },
  });

  function save(): void {
    if (
      disabled ||
      !canSave ||
      !proposal.passedExamples ||
      mutation.isPending ||
      mutation.isSuccess
    ) {
      return;
    }

    mutation.mutate();
  }

  if (!canSave || !proposal.passedExamples) {
    return null;
  }

  return (
    <div className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3">
      {!mutation.isSuccess && (
        <AppButton
          type="button"
          variant="secondary"
          disabled={disabled || mutation.isPending}
          onClick={save}
        >
          {mutation.isPending
            ? "Перепроверяем и сохраняем..."
            : "Сохранить числовой шаблон"}
        </AppButton>
      )}

      {mutation.isPending && (
        <p role="status" className="text-sm text-[var(--app-muted)]">
          Сервер повторно проверяет шаблон на действующих учебных примерах.
        </p>
      )}

      {mutation.isError && (
        <div role="alert" className="grid gap-1 text-sm">
          <p className="text-[var(--app-danger)]">
            {getApiErrorMessage(
              mutation.error,
              "Не удалось сохранить числовой шаблон.",
            )}
          </p>
          <p className="text-[var(--app-muted)]">
            Если изменились учебные примеры или версия генератора, снова нажмите
            «Показать структурные предложения» и сохраните обновлённое
            предложение.
          </p>
        </div>
      )}

      {mutation.isSuccess && (
        <div role="status" className="grid gap-1 text-sm">
          <p className="font-semibold">Числовой шаблон сохранён как черновик</p>
          <p className="break-all text-[var(--app-muted)]">
            Идентификатор: {mutation.data.draftId}
          </p>
          <p className="text-[var(--app-muted)]">
            Сохранены шаблон и основания его проверки. Правило не активировано,
            строки импорта не изменены.
          </p>
        </div>
      )}
    </div>
  );
}
