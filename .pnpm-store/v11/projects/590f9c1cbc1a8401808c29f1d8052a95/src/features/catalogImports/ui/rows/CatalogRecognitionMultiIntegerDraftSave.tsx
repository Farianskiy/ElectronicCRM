"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { recognitionMultiIntegerDraftsQueryRoot } from "../../api/getRecognitionMultiIntegerDrafts";
import { AppButton } from "@/shared/ui/AppButton";
import {
  saveRecognitionMultiIntegerDraft,
  type SaveRecognitionMultiIntegerDraftRequest,
} from "../../api/saveRecognitionMultiIntegerDraft";
import type {
  RecognitionMultiIntegerProposal,
  RecognitionMultiIntegerRequest,
} from "../../model/recognitionMultiIntegerProposals";

interface CatalogRecognitionMultiIntegerDraftSaveProps {
  trainingRequest: RecognitionMultiIntegerRequest;
  generatorVersion: string;
  proposal: RecognitionMultiIntegerProposal;
  disabled: boolean;
}

export function CatalogRecognitionMultiIntegerDraftSave({
  trainingRequest,
  generatorVersion,
  proposal,
  disabled,
}: CatalogRecognitionMultiIntegerDraftSaveProps) {
  const access = useCurrentUserAccess();
  const queryClient = useQueryClient();

  const canSave =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: (request: SaveRecognitionMultiIntegerDraftRequest) =>
      saveRecognitionMultiIntegerDraft(request),
    retry: false,
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: recognitionMultiIntegerDraftsQueryRoot,
      });
    },
  });

  function save(): void {
    if (
      !canSave ||
      disabled ||
      !proposal.passedExamples ||
      mutation.isPending ||
      mutation.isSuccess
    ) {
      return;
    }

    mutation.mutate({
      manufacturerId: trainingRequest.manufacturerId,
      productTypeId: trainingRequest.productTypeId,
      characteristicDefinitionIds: [
        ...trainingRequest.characteristicDefinitionIds,
      ],
      productNames: [...trainingRequest.productNames],
      generatorVersion,
      parts: proposal.pattern.parts.map((part) => ({
        literal: part.literal,
        characteristicDefinitionId: part.characteristicDefinitionId,
      })),
    });
  }

  if (!canSave || !proposal.passedExamples) {
    return null;
  }

  return (
    <section className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3">
      <AppButton
        type="button"
        variant="secondary"
        disabled={disabled || mutation.isPending || mutation.isSuccess}
        onClick={save}
      >
        {mutation.isPending
          ? "Проверяем и сохраняем..."
          : mutation.isSuccess
            ? "Черновик сохранён"
            : "Сохранить шаблон"}
      </AppButton>

      <p className="text-[var(--app-muted)]">
        Перед сохранением сервер повторно проверит выбранную учебную подборку.
        Сохраняется черновик: правило не активируется, строки импорта не
        меняются.
      </p>

      {mutation.isPending && (
        <p role="status">
          Проверяем действующие подтверждения и сохраняем основание шаблона…
        </p>
      )}

      {mutation.isError && (
        <div role="alert" className="grid gap-1 text-[var(--app-danger)]">
          <p>
            {getApiErrorMessage(
              mutation.error,
              "Не удалось сохранить составной шаблон.",
            )}
          </p>
          <p>
            Если изменились подтверждения или версия генератора, заново
            постройте составные предложения.
          </p>
        </div>
      )}

      {mutation.isSuccess && (
        <div role="status" className="grid gap-1">
          <p className="font-semibold">
            Черновик сохранён или уже существовал с теми же основаниями.
          </p>
          <p className="break-all text-[var(--app-muted)]">
            Идентификатор: {mutation.data.id}
          </p>
          <p>Шаблон пока не применяется при распознавании.</p>
        </div>
      )}
    </section>
  );
}
