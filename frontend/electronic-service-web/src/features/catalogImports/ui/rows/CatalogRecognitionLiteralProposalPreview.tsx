"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { previewRecognitionLiteralProposals } from "../../api/previewRecognitionLiteralProposals";
import { CatalogRecognitionLiteralDraftSaveButton } from "./CatalogRecognitionLiteralDraftSaveButton";
import { CatalogRecognitionLiteralDraftList } from "./CatalogRecognitionLiteralDraftList";
import { CatalogRecognitionIntegerPatternPreview } from "./CatalogRecognitionIntegerPatternPreview";

interface CatalogRecognitionLiteralProposalPreviewProps {
  batchId: string;
  manufacturerId: string;
  productTypeId: string;
  characteristicDefinitionId: string;
  characteristicName: string;
  disabled: boolean;
}

export function CatalogRecognitionLiteralProposalPreview({
  batchId,
  manufacturerId,
  productTypeId,
  characteristicDefinitionId,
  characteristicName,
  disabled,
}: CatalogRecognitionLiteralProposalPreviewProps) {
  const access = useCurrentUserAccess();
  const canPreview =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const preview = useMutation({
    mutationFn: () =>
      previewRecognitionLiteralProposals({
        manufacturerId,
        productTypeId,
        characteristicDefinitionId,
      }),
  });

  const result = preview.isSuccess ? preview.data : undefined;

  function loadPreview(): void {
    if (
      disabled ||
      !canPreview ||
      preview.isPending ||
      !manufacturerId ||
      !productTypeId ||
      !characteristicDefinitionId
    ) {
      return;
    }

    preview.mutate();
  }

  if (!canPreview) {
    return null;
  }

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] bg-[var(--app-surface)] p-4">
      <div>
        <h4 className="font-semibold text-[var(--app-text)]">
          Предложения правил: {characteristicName}
        </h4>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Используются действующие подтверждённые примеры из всех пакетов для
          этого производителя, типа товара и характеристики.
        </p>
        <p className="mt-1 text-sm text-[var(--app-muted)]">
          Просмотр ничего не изменяет. Прошедшее проверку предложение можно
          отдельно сохранить как черновик. Автоматической активации нет.
        </p>
      </div>

      <AppButton
        type="button"
        variant="secondary"
        disabled={disabled || preview.isPending}
        onClick={loadPreview}
      >
        {preview.isPending
          ? "Проверяем примеры..."
          : "Показать предложения правил"}
      </AppButton>

      {disabled && (
        <p className="text-sm text-[var(--app-muted)]">
          Завершите сохранение или подтверждение текущей разметки перед
          просмотром.
        </p>
      )}

      {preview.isError && (
        <p role="alert" className="text-sm text-[var(--app-danger)]">
          {getApiErrorMessage(
            preview.error,
            "Не удалось получить предложения правил.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-3">
          <p className="text-xs text-[var(--app-muted)]">
            Версия генератора: {result.generatorVersion}. Результат
            соответствует моменту последнего запроса.
          </p>

          {result.issues.length > 0 && (
            <div
              role="alert"
              className="grid gap-2 rounded-xl border border-[var(--app-border)] p-3"
            >
              <p className="font-semibold text-[var(--app-danger)]">
                Подготовка примеров не пройдена
              </p>
              {result.issues.map((issue, index) => (
                <div key={`${issue.code}-${index}`} className="text-sm">
                  <p>{issue.message}</p>
                  {issue.exampleIds.length > 0 && (
                    <details className="mt-1">
                      <summary className="cursor-pointer text-[var(--app-muted)]">
                        Идентификаторы примеров: {issue.exampleIds.length}
                      </summary>
                      <p className="mt-1 break-all">
                        {issue.exampleIds.join(", ")}
                      </p>
                    </details>
                  )}
                </div>
              ))}
            </div>
          )}

          {result.issues.length === 0 && result.proposals.length === 0 && (
            <p className="text-sm text-[var(--app-muted)]">
              Предложения не сформированы.
            </p>
          )}

          {result.proposals.map((proposal) => (
            <div
              key={JSON.stringify([proposal.literal, proposal.normalizedValue])}
              className="grid gap-2 rounded-xl border border-[var(--app-border)] p-3 text-sm"
            >
              <p className="font-semibold">
                «{proposal.literal}» → {proposal.normalizedValue}
              </p>
              <p
                className={
                  proposal.passedExamples
                    ? "text-[var(--app-text)]"
                    : "text-[var(--app-danger)]"
                }
              >
                {proposal.passedExamples
                  ? "Точное правило прошло проверку на имеющихся примерах"
                  : "Точное правило не прошло проверку. Это не означает, что человеческая разметка неверна."}
              </p>
              <p>Названий с совпадением: {proposal.matchedNameCount}</p>
              {proposal.matchedNameCount === 0 && (
                <p className="text-[var(--app-muted)]">
                  Фрагмент не найден как отдельное обозначение. Например, точный
                  поиск не принимает «32» внутри «32А». Такое выделение
                  проверяется структурным генератором ниже; изменять его только
                  ради точного правила не нужно.
                </p>
              )}
              <p>
                Поддерживающих подтверждений:{" "}
                {proposal.supportingExampleIds.length}
              </p>
              <p>
                Подтверждений, которые это точное правило не воспроизводит:{" "}
                {proposal.conflictingExampleIds.length}
              </p>

              <CatalogRecognitionLiteralDraftSaveButton
                scope={result.scope}
                generatorVersion={result.generatorVersion}
                proposal={proposal}
                disabled={
                  disabled || preview.isPending || result.issues.length > 0
                }
              />

              {proposal.conflictingExampleIds.length > 0 && (
                <details>
                  <summary className="cursor-pointer text-[var(--app-muted)]">
                    Показать идентификаторы конфликтующих примеров
                  </summary>
                  <p className="mt-1 break-all">
                    {proposal.conflictingExampleIds.join(", ")}
                  </p>
                </details>
              )}
            </div>
          ))}

          <p className="text-xs text-[var(--app-muted)]">
            Повторные подтверждения одинакового названия не являются
            независимыми примерами. Успешная проверка не гарантирует
            правильность на новой номенклатуре.
          </p>
        </div>
      )}

      <CatalogRecognitionIntegerPatternPreview
        key={JSON.stringify([
          "integer-patterns",
          batchId,
          manufacturerId,
          productTypeId,
          characteristicDefinitionId,
        ])}
        batchId={batchId}
        scope={{ manufacturerId, productTypeId, characteristicDefinitionId }}
        disabled={disabled}
      />

      <CatalogRecognitionLiteralDraftList
        key={JSON.stringify([
          "literal-drafts",
          batchId,
          manufacturerId,
          productTypeId,
          characteristicDefinitionId,
        ])}
        batchId={batchId}
        scope={{ manufacturerId, productTypeId, characteristicDefinitionId }}
      />
    </section>
  );
}
