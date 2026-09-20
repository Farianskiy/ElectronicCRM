"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { recheckRecognitionIntegerDraft } from "../../api/recheckRecognitionIntegerDraft";

interface CatalogRecognitionIntegerDraftRecheckPanelProps {
  draftId: string;
}

export function CatalogRecognitionIntegerDraftRecheckPanel({
  draftId,
}: CatalogRecognitionIntegerDraftRecheckPanelProps) {
  const access = useCurrentUserAccess();
  const canCheck =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: () => recheckRecognitionIntegerDraft(draftId),
    retry: false,
  });

  function recheck(): void {
    if (!canCheck || mutation.isPending) {
      return;
    }

    mutation.mutate();
  }

  if (!canCheck) {
    return null;
  }

  const result = mutation.isSuccess ? mutation.data : null;

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3 text-sm">
      <AppButton
        type="button"
        variant="secondary"
        disabled={mutation.isPending}
        onClick={recheck}
      >
        {mutation.isPending
          ? "Перепроверяем шаблон..."
          : "Перепроверить числовой шаблон"}
      </AppButton>

      {mutation.isPending && (
        <p role="status">
          Проверяем сохранённый шаблон по действующим подтверждениям.
        </p>
      )}

      {mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось выполнить перепроверку числового шаблона.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-2">
          <p role="status" className="font-semibold">
            {result.passedCurrentExamples
              ? "Сохранённый шаблон прошёл проверку на текущих примерах."
              : "Успешность проверки не подтверждена — смотрите результаты ниже."}
          </p>

          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>
          <p>Текущая версия генератора: {result.currentGeneratorVersion}</p>
          <p>
            Загружено действующих подтверждений: {result.loadedExampleCount}
          </p>

          {!result.generatorVersionMatches && (
            <p className="text-[var(--app-danger)]">
              Версия генератора отличается от сохранённой. Получите новое
              предложение; старый черновик не изменён.
            </p>
          )}

          {!result.selectionComplete && (
            <p className="text-[var(--app-danger)]">
              Учебная выборка загружена не полностью. Проверка не считается
              успешной. Сравнение добавленных и выбывших подтверждений
              недоступно.
            </p>
          )}

          {result.selectionComplete && (
            <div className="grid gap-2">
              <p>
                {result.evidenceUnchanged
                  ? "Набор подтверждений совпадает с основанием проверки при сохранении."
                  : "Набор подтверждений изменился. Историческое основание черновика не перезаписано."}
              </p>

              <p>Новых подтверждений: {result.addedExampleIds.length}</p>
              <p>
                Больше не входят в действующую выборку:{" "}
                {result.missingExampleIds.length}
              </p>

              {result.addedExampleIds.length > 0 && (
                <details>
                  <summary className="cursor-pointer">
                    Идентификаторы новых подтверждений
                  </summary>
                  <p className="break-all text-[var(--app-muted)]">
                    {result.addedExampleIds.join(", ")}
                  </p>
                </details>
              )}

              {result.missingExampleIds.length > 0 && (
                <details>
                  <summary className="cursor-pointer">
                    Идентификаторы выбывших подтверждений
                  </summary>
                  <p className="break-all text-[var(--app-muted)]">
                    {result.missingExampleIds.join(", ")}
                  </p>
                </details>
              )}
            </div>
          )}

          {result.evaluation && (
            <div className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3">
              <p className="font-semibold">Результат сопоставления</p>
              <p>
                Названий с совпадением: {result.evaluation.matchedNameCount}
              </p>
              <p>
                Разных поддержанных значений:{" "}
                {result.evaluation.distinctValueCount}
              </p>
              <p>
                Поддерживающих подтверждений:{" "}
                {result.evaluation.supportingExampleIds.length}
              </p>
              <p>
                Конфликтующих подтверждений:{" "}
                {result.evaluation.conflictingExampleIds.length}
              </p>
            </div>
          )}

          {result.issues.map((issue, index) => (
            <div
              key={`${issue.code}-${index}`}
              className="grid gap-1 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="text-[var(--app-danger)]">{issue.message}</p>
              <p className="text-[var(--app-muted)]">
                Код: {issue.code}. Связанных подтверждений:{" "}
                {issue.exampleIds.length}.
              </p>

              {issue.exampleIds.length > 0 && (
                <details>
                  <summary className="cursor-pointer">
                    Показать идентификаторы подтверждений
                  </summary>
                  <p className="break-all">{issue.exampleIds.join(", ")}</p>
                </details>
              )}
            </div>
          ))}

          <p className="text-[var(--app-muted)]">
            Результат относится к моменту проверки и не обновляется
            автоматически. Черновик не изменён и не активирован. Успех на
            учебных примерах не доказывает правильность на новых названиях.
          </p>
        </div>
      )}
    </section>
  );
}
