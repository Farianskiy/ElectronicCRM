"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { AppButton } from "@/shared/ui/AppButton";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { previewRecognitionIntegerPatterns } from "../../api/previewRecognitionIntegerPatterns";
import type { RecognitionTrainingScope } from "../../model/recognitionLiteralProposals";
import { CatalogRecognitionIntegerBatchPreviewPanel } from "./CatalogRecognitionIntegerBatchPreviewPanel";
import { CatalogRecognitionIntegerDraftSaveButton } from "./CatalogRecognitionIntegerDraftSaveButton";
import { CatalogRecognitionIntegerDraftList } from "./CatalogRecognitionIntegerDraftList";

interface CatalogRecognitionIntegerPatternPreviewProps {
  batchId: string;
  scope: RecognitionTrainingScope;
  disabled: boolean;
}

export function CatalogRecognitionIntegerPatternPreview({
  batchId,
  scope,
  disabled,
}: CatalogRecognitionIntegerPatternPreviewProps) {
  const access = useCurrentUserAccess();
  const canPreview =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: () => previewRecognitionIntegerPatterns(scope),
    retry: false,
  });

  function preview(): void {
    if (!canPreview || disabled || mutation.isPending) {
      return;
    }

    mutation.mutate();
  }

  if (!canPreview) {
    return null;
  }

  const result = mutation.isSuccess && !disabled ? mutation.data : null;

  return (
    <section className="grid gap-3 rounded-xl border border-[var(--app-border)] p-4 text-sm">
      <h4 className="font-semibold">Структурные предложения: целое число</h4>
      <p className="text-[var(--app-muted)]">
        Выберите числовую характеристику и выделяйте только целое число. Начало
        названия перед числом должно совпадать. Окончания могут отличаться:
        генератор перечислит варианты из подтверждённых примеров. Требуются
        минимум два разных подтверждённых значения. Дроби и преобразование
        единиц пока не поддерживаются.
      </p>

      <AppButton
        type="button"
        variant="secondary"
        disabled={disabled || mutation.isPending}
        onClick={preview}
      >
        {mutation.isPending
          ? "Проверяем структуру..."
          : "Показать структурные предложения"}
      </AppButton>

      {disabled && (
        <p>Сначала завершите сохранение и подтверждение разметки.</p>
      )}
      {mutation.isPending && (
        <p role="status">Сопоставляем подтверждённые примеры...</p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось получить структурные предложения.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-3">
          <p className="text-[var(--app-muted)]">
            Версия генератора: {result.generatorVersion}. Результат
            соответствует последнему запросу.
          </p>

          {result.issues.map((issue, index) => (
            <div
              key={`${issue.code}-${index}`}
              className="rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="text-[var(--app-danger)]">{issue.message}</p>
              <p>
                Код: {issue.code}. Связанных примеров: {issue.exampleIds.length}
                .
              </p>
            </div>
          ))}

          {result.issues.length === 0 && result.proposals.length === 0 && (
            <p>
              Подходящие предложения не найдены. Нужны минимум два разных
              подтверждённых целых числа с одинаковым началом названия перед
              выделением. Правильное значение должно соответствовать выделенному
              числу.
            </p>
          )}

          {result.proposals.map((proposal) => (
            <div
              key={JSON.stringify(proposal.pattern)}
              className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3"
            >
              <div className="grid gap-2">
                <p className="font-semibold">
                  Допустимые варианты полного названия:
                </p>
                {proposal.pattern.suffixes.map((suffix) => (
                  <p
                    key={suffix}
                    className="whitespace-pre-wrap break-all font-mono"
                  >
                    {proposal.pattern.prefix}
                    <span className="font-bold">{"{число}"}</span>
                    {suffix}
                  </p>
                ))}
                <p className="text-[var(--app-muted)]">
                  Один вариант должен совпасть целиком. Неизвестные окончания
                  этот шаблон не принимает.
                </p>
              </div>
              <p>
                {proposal.passedExamples
                  ? "Прошло проверку на имеющихся примерах"
                  : "Не прошло проверку — обнаружены расхождения"}
              </p>
              <p>Названий с совпадением: {proposal.matchedNameCount}</p>
              <p>Разных поддержанных значений: {proposal.distinctValueCount}</p>
              <p>
                Поддерживающих подтверждений:{" "}
                {proposal.supportingExampleIds.length}
              </p>
              <p>
                Конфликтующих подтверждений:{" "}
                {proposal.conflictingExampleIds.length}
              </p>

              {batchId && proposal.passedExamples && (
                <CatalogRecognitionIntegerBatchPreviewPanel
                  key={JSON.stringify([
                    batchId,
                    result.scope,
                    result.generatorVersion,
                    proposal.pattern,
                  ])}
                  batchId={batchId}
                  scope={result.scope}
                  generatorVersion={result.generatorVersion}
                  pattern={proposal.pattern}
                  disabled={disabled || mutation.isPending}
                />
              )}

              {proposal.passedExamples && (
                <CatalogRecognitionIntegerDraftSaveButton
                  key={JSON.stringify([
                    "integer-draft-save",
                    result.scope,
                    result.generatorVersion,
                    proposal.pattern,
                    proposal.supportingExampleIds,
                  ])}
                  scope={result.scope}
                  generatorVersion={result.generatorVersion}
                  proposal={proposal}
                  disabled={disabled || mutation.isPending}
                />
              )}

              {proposal.conflictingExampleIds.length > 0 && (
                <details>
                  <summary className="cursor-pointer">
                    Идентификаторы конфликтующих примеров
                  </summary>
                  <p className="break-all">
                    {proposal.conflictingExampleIds.join(", ")}
                  </p>
                </details>
              )}
            </div>
          ))}

          <p className="text-[var(--app-muted)]">
            Предварительный просмотр ничего не сохраняет. Прошедший проверку
            шаблон можно отдельно сохранить как черновик кнопкой «Сохранить
            числовой шаблон». Сохранение не активирует правило. Разрешённые
            окончания взяты из примеров, но новые сочетания числа и окончания
            ещё требуют проверки. Прохождение учебных примеров не доказывает
            правильность на новых строках.
          </p>
        </div>
      )}

      <CatalogRecognitionIntegerDraftList
        key={JSON.stringify([
          "integer-draft-list",
          batchId,
          scope.manufacturerId,
          scope.productTypeId,
          scope.characteristicDefinitionId,
        ])}
        batchId={batchId}
        scope={scope}
      />
    </section>
  );
}
