"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { useAuthSession } from "@/features/auth/model/useAuthSession";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { recheckRecognitionLiteralDraft } from "../../api/recheckRecognitionLiteralDraft";
import type { RecognitionLiteralDraftRecheckResult } from "../../model/recognitionLiteralDraftRecheck";

interface CatalogRecognitionLiteralDraftRecheckPanelProps {
  draftId: string;
}

function getResultTitle(result: RecognitionLiteralDraftRecheckResult): string {
  if (!result.selectionComplete) {
    return "Проверка неполная: превышен лимит выборки примеров.";
  }

  if (result.issues.length > 0) {
    return "Проверка не пройдена: обнаружены проблемы в учебных данных.";
  }

  if (result.proposal === null) {
    return "Предложение больше не формируется из текущих подтверждений.";
  }

  if (!result.passedCurrentExamples) {
    return "Проверка не пройдена: предложение не прошло текущие примеры.";
  }

  if (!result.generatorVersionMatches) {
    return "Текущие примеры пройдены, но версия генератора изменилась.";
  }

  if (!result.evidenceUnchanged) {
    return "Текущие примеры пройдены, но состав подтверждений изменился.";
  }

  return "Проверка на текущих примерах пройдена. Состав подтверждений не изменился.";
}

export function CatalogRecognitionLiteralDraftRecheckPanel({
  draftId,
}: CatalogRecognitionLiteralDraftRecheckPanelProps) {
  const access = useCurrentUserAccess();
  const session = useAuthSession();
  const canRecheck =
    Boolean(session) &&
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: () => recheckRecognitionLiteralDraft(draftId),
    retry: false,
  });

  function recheck(): void {
    if (!canRecheck || mutation.isPending) {
      return;
    }

    mutation.mutate();
  }

  if (!canRecheck) {
    return null;
  }

  const result = mutation.isSuccess ? mutation.data : null;

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-3 text-sm">
      <AppButton
        type="button"
        variant="secondary"
        disabled={mutation.isPending}
        onClick={recheck}
      >
        {mutation.isPending ? "Проверяем..." : "Перепроверить черновик"}
      </AppButton>

      {mutation.isPending && (
        <p role="status">
          Сверяем подтверждения и повторно проверяем предложение.
        </p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось перепроверить черновик.",
          )}
        </p>
      )}

      {result && (
        <div role="status" className="grid gap-2">
          <p className="font-semibold">{getResultTitle(result)}</p>
          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>
          <p>Текущая версия генератора: {result.currentGeneratorVersion}</p>
          <p>Загружено активных подтверждений: {result.currentExampleCount}</p>

          {!result.generatorVersionMatches && (
            <p className="text-[var(--app-muted)]">
              Черновик сохранён другой версией генератора. Результат ниже
              получен текущей версией.
            </p>
          )}

          {result.selectionComplete && (
            <>
              <p>Новых подтверждений: {result.addedExampleIds.length}</p>
              <p>
                Больше не входят в активную выборку:{" "}
                {result.missingExampleIds.length}
              </p>
              {!result.evidenceUnchanged && (
                <p>
                  Сохранённое основание проверки отличается от текущего.
                  Черновик не перезаписан.
                </p>
              )}
            </>
          )}

          {!result.selectionComplete && (
            <p className="text-[var(--app-danger)]">
              Состав подтверждений нельзя достоверно сравнить: получена не вся
              выборка.
            </p>
          )}

          {result.issues.length > 0 && (
            <div className="grid gap-2">
              {result.issues.map((issue, index) => (
                <div
                  key={`${issue.code}-${index}`}
                  className="rounded-lg border border-[var(--app-border)] p-2"
                >
                  <p className="text-[var(--app-danger)]">{issue.message}</p>
                  <p className="text-xs text-[var(--app-muted)]">
                    Код: {issue.code}. Связанных примеров:{" "}
                    {issue.exampleIds.length}
                  </p>
                </div>
              ))}
            </div>
          )}

          {result.proposal && (
            <div className="grid gap-1 rounded-lg border border-[var(--app-border)] p-2">
              <p>
                «{result.proposal.literal}» → {result.proposal.normalizedValue}
              </p>
              <p>Названий с совпадением: {result.proposal.matchedNameCount}</p>
              <p>
                Поддерживающих подтверждений:{" "}
                {result.proposal.supportingExampleIds.length}
              </p>
              <p>
                Конфликтующих подтверждений:{" "}
                {result.proposal.conflictingExampleIds.length}
              </p>
            </div>
          )}

          <p className="text-[var(--app-muted)]">
            Результат относится к моменту проверки и не обновляется
            автоматически. Черновик не активирован. Успешная проверка не
            гарантирует правильность на новой номенклатуре.
          </p>
        </div>
      )}
    </section>
  );
}
