"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { recognitionLiteralDraftsQueryRoot } from "../../api/getRecognitionLiteralDrafts";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { saveRecognitionLiteralDraft } from "../../api/saveRecognitionLiteralDraft";
import type {
  RecognitionLiteralProposal,
  RecognitionTrainingScope,
} from "../../model/recognitionLiteralProposals";

interface CatalogRecognitionLiteralDraftSaveButtonProps {
  scope: RecognitionTrainingScope;
  generatorVersion: string;
  proposal: RecognitionLiteralProposal;
  disabled: boolean;
}

export function CatalogRecognitionLiteralDraftSaveButton({
  scope,
  generatorVersion,
  proposal,
  disabled,
}: CatalogRecognitionLiteralDraftSaveButtonProps) {
  const access = useCurrentUserAccess();
  const queryClient = useQueryClient();
  const canSave =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: () =>
      saveRecognitionLiteralDraft({
        manufacturerId: scope.manufacturerId,
        productTypeId: scope.productTypeId,
        characteristicDefinitionId: scope.characteristicDefinitionId,
        literal: proposal.literal,
        normalizedValue: proposal.normalizedValue,
        generatorVersion,
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: recognitionLiteralDraftsQueryRoot,
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
    <div className="grid gap-2">
      {!mutation.isSuccess && (
        <AppButton
          type="button"
          variant="secondary"
          disabled={disabled || mutation.isPending}
          onClick={save}
        >
          {mutation.isPending
            ? "Проверяем и сохраняем..."
            : "Сохранить предложение"}
        </AppButton>
      )}

      {mutation.isError && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось сохранить предложение.",
          )}
        </p>
      )}

      {mutation.isSuccess && (
        <div role="status" className="grid gap-1 text-sm">
          <p className="font-semibold">Черновик сохранён</p>
          <p className="break-all text-[var(--app-muted)]">
            Идентификатор: {mutation.data.draftId}
          </p>
          <p className="text-[var(--app-muted)]">
            Правило не активировано и пока не влияет на распознавание.
          </p>
        </div>
      )}
    </div>
  );
}
