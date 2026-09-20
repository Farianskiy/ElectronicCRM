"use client";

import { useMutation } from "@tanstack/react-query";
import { useCurrentUserAccess } from "@/features/auth/model/CurrentUserAccessContext";
import type { CatalogProductTypeCharacteristicMetadata } from "@/features/catalogMetadata/model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";
import { recheckRecognitionMultiIntegerDraft } from "../../api/recheckRecognitionMultiIntegerDraft";

interface CatalogRecognitionMultiIntegerDraftRecheckPanelProps {
  draftId: string;
  characteristics: CatalogProductTypeCharacteristicMetadata[];
  disabled: boolean;
}

export function CatalogRecognitionMultiIntegerDraftRecheckPanel({
  draftId,
  characteristics,
  disabled,
}: CatalogRecognitionMultiIntegerDraftRecheckPanelProps) {
  const access = useCurrentUserAccess();

  const canCheck =
    !access.isLoading &&
    !access.isError &&
    access.hasPermission("DictionariesManage");

  const mutation = useMutation({
    mutationFn: (id: string) => recheckRecognitionMultiIntegerDraft(id),
    retry: false,
  });

  const result = mutation.isSuccess && !disabled ? mutation.data : null;

  function characteristicName(id: string): string {
    return characteristics.find((item) => item.id === id)?.name ?? id;
  }

  function recheck(): void {
    if (!canCheck || disabled || mutation.isPending) {
      return;
    }

    mutation.mutate(draftId);
  }

  if (!canCheck) {
    return null;
  }

  return (
    <section className="grid gap-3 rounded-lg border border-[var(--app-border)] p-3">
      <AppButton
        type="button"
        variant="secondary"
        disabled={disabled || mutation.isPending}
        onClick={recheck}
      >
        {mutation.isPending ? "Перепроверяем..." : "Перепроверить черновик"}
      </AppButton>

      <p className="text-[var(--app-muted)]">
        Проверяем сохранённый шаблон по действующим подтверждениям исходной
        подборки названий. Черновик не перезаписывается и не активируется.
      </p>

      {mutation.isPending && (
        <p role="status">
          Загружаем действующие подтверждения и проверяем шаблон...
        </p>
      )}

      {!disabled && mutation.isError && (
        <p role="alert" className="text-[var(--app-danger)]">
          {getApiErrorMessage(
            mutation.error,
            "Не удалось выполнить перепроверку черновика.",
          )}
        </p>
      )}

      {result && (
        <div className="grid gap-2">
          <p role="status" className="font-semibold">
            {result.passed
              ? "Сохранённый шаблон прошёл проверку исходной учебной подборки."
              : "Проверка не пройдена — требуется разбор причин."}
          </p>

          <p className="text-[var(--app-muted)]">
            Проверено: {new Date(result.checkedAtUtc).toLocaleString("ru-RU")}
          </p>

          <p>Текущая версия генератора: {result.currentGeneratorVersion}</p>

          {!result.selectionComplete && (
            <p className="text-[var(--app-danger)]">
              Полная проверка подборки не завершена. Состав подтверждений нельзя
              считать неизменным.
            </p>
          )}

          {result.selectionComplete && result.evidenceUnchanged === true && (
            <p>
              Состав подтверждений совпадает с основанием сохранённого
              черновика.
            </p>
          )}

          {result.selectionComplete && result.evidenceUnchanged === false && (
            <p className="text-[var(--app-muted)]">
              Состав подтверждений изменился. Результат ниже относится к текущим
              данным; исторические основания черновика сохранены.
            </p>
          )}

          {result.selectionComplete && result.evidenceUnchanged !== null && (
            <>
              <p>Новых подтверждений: {result.addedExampleIds.length}</p>
              <p>
                Больше не входят в действующую подборку:{" "}
                {result.missingExampleIds.length}
              </p>

              {(result.addedExampleIds.length > 0 ||
                result.missingExampleIds.length > 0) && (
                <details>
                  <summary>Показать изменившиеся подтверждения</summary>
                  <div className="grid gap-2 pt-2">
                    {result.addedExampleIds.length > 0 && (
                      <p className="break-all">
                        Добавлены: {result.addedExampleIds.join(", ")}
                      </p>
                    )}
                    {result.missingExampleIds.length > 0 && (
                      <p className="break-all">
                        Выбыли: {result.missingExampleIds.join(", ")}
                      </p>
                    )}
                  </div>
                </details>
              )}
            </>
          )}

          {result.evaluation && (
            <div className="grid gap-2 rounded-lg border border-[var(--app-border)] p-3">
              <p>
                Названий с совпадением: {result.evaluation.matchedNameCount}
              </p>
              <p>
                Полностью поддерживающих названий:{" "}
                {result.evaluation.supportingNameCount}
              </p>
              <p>
                Поддерживающих подтверждений:{" "}
                {result.evaluation.supportingExampleIds.length}
              </p>
              <p>
                Конфликтующих подтверждений:{" "}
                {result.evaluation.conflictingExampleIds.length}
              </p>

              {result.evaluation.fields.map((coverage) => (
                <p key={coverage.characteristicDefinitionId}>
                  {characteristicName(coverage.characteristicDefinitionId)}:{" "}
                  разных подтверждённых значений — {coverage.distinctValueCount}
                </p>
              ))}
            </div>
          )}

          {result.issues.map((issue, index) => (
            <div
              key={`${issue.code}-${index}`}
              className="grid gap-1 rounded-lg border border-[var(--app-border)] p-3"
            >
              <p className="text-[var(--app-danger)]">{issue.message}</p>
              <p className="text-[var(--app-muted)]">Код: {issue.code}</p>

              {issue.exampleIds.length > 0 && (
                <details>
                  <summary>Связанные подтверждения</summary>
                  <p className="break-all">{issue.exampleIds.join(", ")}</p>
                </details>
              )}
            </div>
          ))}

          <p className="text-[var(--app-muted)]">
            Результат относится к моменту запроса и не обновляется
            автоматически. Успешная проверка учебных примеров не доказывает
            правильность на новых товарах и не означает активацию.
          </p>
        </div>
      )}
    </section>
  );
}
