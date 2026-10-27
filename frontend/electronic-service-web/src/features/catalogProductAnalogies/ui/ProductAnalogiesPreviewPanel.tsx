"use client";

import { useMutation } from "@tanstack/react-query";
import { useState } from "react";
import { previewCatalogProductAnalogies } from "../api/previewCatalogProductAnalogies";
import type {
  CatalogProductAnalogyCharacteristicProposal,
  CatalogProductAnalogiesPreview,
  CatalogProductAnalogyMatch,
  CatalogProductAnalogyProductTypeProposal,
} from "../model/types";
import { getApiErrorMessage } from "@/shared/api/getApiErrorMessage";
import { AppButton } from "@/shared/ui/AppButton";

interface ProductAnalogiesPreviewPanelProps {
  productName: string;
  article?: string | null;
  manufacturerId: string | null;
  productTypeId: string | null;
  knownCharacteristics: Record<string, string>;
  importBatchId?: string;
  importRowId?: string;
  onApplyCharacteristicProposal?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
  ) => void;
  onApplyProductTypeProposal?: (
    proposal: CatalogProductAnalogyProductTypeProposal,
  ) => void;
  onRejectCharacteristicProposal?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
    reason: string,
  ) => void;
}

export function ProductAnalogiesPreviewPanel({
  productName,
  article,
  manufacturerId,
  productTypeId,
  knownCharacteristics,
  importBatchId,
  importRowId,
  onApplyCharacteristicProposal,
  onApplyProductTypeProposal,
  onRejectCharacteristicProposal,
}: ProductAnalogiesPreviewPanelProps) {
  const previewMutation = useMutation({
    mutationFn: previewCatalogProductAnalogies,
  });

  const canPreview =
    productName.trim().length > 0 &&
    Boolean(manufacturerId);

  function handlePreview(): void {
    if (!manufacturerId) {
      return;
    }

    previewMutation.mutate({
      productName,
      article: article || null,
      manufacturerId,
      productTypeId: productTypeId || null,
      knownCharacteristics,
      maximumResults: 10,
      importBatchId,
      importRowId,
    });
  }

  return (
    <section className="rounded-3xl border border-cyan-500/20 bg-cyan-500/[0.05] p-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-semibold uppercase tracking-[0.18em] text-cyan-300">
            Аналоги из каталога
          </p>

          <h2 className="mt-2 text-xl font-semibold text-white">
            Найти похожие товары с уже заполненными характеристиками
          </h2>

          <p className="mt-2 max-w-4xl text-sm leading-6 text-slate-400">
            Поиск использует выбранного производителя и тип товара, совпадения
            в наименовании и уже распознанные значения. Результат показывает
            доказательства сходства и противоречия.
          </p>
        </div>

        <AppButton
          variant="secondary"
          loading={previewMutation.isPending}
          disabled={!canPreview}
          onClick={handlePreview}
        >
          Найти аналоги
        </AppButton>
      </div>

      <div className="mt-4 rounded-2xl border border-blue-500/25 bg-blue-500/[0.08] p-4 text-sm text-blue-100/85">
        {onApplyCharacteristicProposal || onRejectCharacteristicProposal
          ? "Решение будет записано только вместе с обычным сохранением строки. До сохранения данные импорта не меняются."
          : "Это диагностический этап. Найденные значения не записываются в товар, импорт, правила или словарь."}
      </div>

      {!canPreview && (
        <p className="mt-4 text-sm text-amber-200">
          Для поиска сначала выберите производителя и запустите распознавание
          наименования. Тип товара можно оставить неопределённым.
        </p>
      )}

      {previewMutation.isError && (
        <div className="mt-5 rounded-2xl border border-red-500/30 bg-red-500/10 p-4 text-red-200">
          <p className="font-medium">Не удалось найти похожие товары</p>
          <p className="mt-2 text-sm">
            {getApiErrorMessage(
              previewMutation.error,
              "Сервис поиска аналогов не смог обработать запрос.",
            )}
          </p>
        </div>
      )}

      {previewMutation.data && (
        <ProductAnalogiesResult
          preview={previewMutation.data}
          onApplyCharacteristicProposal={onApplyCharacteristicProposal}
          onApplyProductTypeProposal={onApplyProductTypeProposal}
          onRejectCharacteristicProposal={onRejectCharacteristicProposal}
        />
      )}
    </section>
  );
}

function ProductAnalogiesResult({
  preview,
  onApplyCharacteristicProposal,
  onApplyProductTypeProposal,
  onRejectCharacteristicProposal,
}: {
  preview: CatalogProductAnalogiesPreview;
  onApplyCharacteristicProposal?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
  ) => void;
  onApplyProductTypeProposal?: (
    proposal: CatalogProductAnalogyProductTypeProposal,
  ) => void;
  onRejectCharacteristicProposal?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
    reason: string,
  ) => void;
}) {
  return (
    <div className="mt-6 border-t border-white/10 pt-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h3 className="font-semibold text-white">Результат поиска</h3>
          <p className="mt-1 text-sm text-slate-400">
            Проверено товаров в выбранной области: {preview.evaluatedCandidatesCount}.
          </p>
          <p className="mt-1 text-xs text-slate-500">
            Снимок каталога: {preview.candidateSnapshotId} · {new Date(
              preview.candidateSnapshotCreatedAtUtc,
            ).toLocaleString("ru-RU")}
          </p>
        </div>

        <span className="rounded-full border border-cyan-500/25 bg-cyan-500/10 px-3 py-1 text-xs text-cyan-100">
          Найдено: {preview.matches.length}
        </span>
      </div>

      {preview.matches.length === 0 ? (
        <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-300">
          Надёжных совпадений не найдено. Система не будет переносить значения
          только из-за одинакового производителя и типа товара.
        </div>
      ) : (
        <div className="mt-4 grid gap-4">
          {preview.matches.map((match) => (
            <ProductAnalogyCard key={match.productId} match={match} />
          ))}
        </div>
      )}

      {preview.excludedCandidatesCount > 0 && (
        <details className="mt-4 rounded-2xl border border-amber-500/20 bg-amber-500/[0.05] p-4">
          <summary className="cursor-pointer text-sm font-medium text-amber-100">
            Почему исключены кандидаты · {preview.excludedCandidatesCount}
          </summary>

          <p className="mt-2 text-xs leading-5 text-slate-400">
            Здесь показана ограниченная диагностическая выборка. Исключённые
            товары не участвуют в предложениях и автозаполнении.
          </p>

          <div className="mt-3 grid gap-3">
            {preview.excludedCandidates.map((candidate) => (
              <article
                key={`${candidate.productId}:${candidate.reasonCode}`}
                className="rounded-xl border border-white/10 bg-black/20 p-3"
              >
                <p className="text-sm font-medium text-slate-200">
                  {candidate.name}
                </p>
                <p className="mt-1 font-mono text-xs text-slate-500">
                  {candidate.article}
                </p>
                <p className="mt-2 text-sm text-amber-100/85">
                  {candidate.reason}
                </p>

                {candidate.candidateFamilyAnchors.length > 0 && (
                  <p className="mt-2 text-xs text-slate-500">
                    Обозначения кандидата: {candidate.candidateFamilyAnchors.join(", ")}
                  </p>
                )}

                {candidate.sharedNameTokens.length > 0 && (
                  <p className="mt-1 text-xs text-slate-500">
                    Общие слова: {candidate.sharedNameTokens.join(", ")}
                  </p>
                )}
              </article>
            ))}
          </div>

          {preview.exclusionDiagnosticsTruncated && (
            <p className="mt-3 text-xs text-slate-500">
              Показаны первые {preview.excludedCandidates.length} кандидатов.
            </p>
          )}
        </details>
      )}

      <ProductTypeProposals
        proposals={preview.productTypeProposals}
        onApply={onApplyProductTypeProposal}
      />

      <CharacteristicProposals
        proposals={preview.characteristicProposals}
        onApply={onApplyCharacteristicProposal}
        onReject={onRejectCharacteristicProposal}
      />
    </div>
  );
}

function ProductTypeProposals({
  proposals,
  onApply,
}: {
  proposals: CatalogProductAnalogyProductTypeProposal[];
  onApply?: (proposal: CatalogProductAnalogyProductTypeProposal) => void;
}) {
  if (proposals.length === 0) {
    return null;
  }

  return (
    <div className="mt-6 border-t border-white/10 pt-6">
      <h3 className="font-semibold text-white">Предложение типа товара</h3>
      <p className="mt-2 text-sm leading-6 text-slate-400">
        Этот расчёт появляется, когда строгий тип ещё не выбран. Он использует
        распределение типов среди подходящих аналогов и требует минимум три
        подтверждения и 80% согласия.
      </p>

      <div className="mt-4 grid gap-4 lg:grid-cols-2">
        {proposals.map((proposal) => (
          <article
            key={proposal.productTypeId}
            className={`rounded-2xl border p-5 ${
              proposal.isSuggested
                ? "border-violet-500/30 bg-violet-500/[0.08]"
                : "border-white/10 bg-black/20"
            }`}
          >
            <div className="flex flex-wrap items-start justify-between gap-3">
              <div>
                <p className="font-semibold text-white">
                  {proposal.productTypeName}
                </p>
                <p className="mt-1 font-mono text-xs text-violet-300">
                  {proposal.productTypeCode}
                </p>
              </div>
              <span className="rounded-full border border-white/10 bg-white/[0.04] px-3 py-1 text-xs text-slate-300">
                {proposal.supportingProductCount} из {proposal.eligibleProductCount}
                {" · "}
                {formatPercent(proposal.supportRatio)}
              </span>
            </div>

            <p className="mt-3 text-sm text-slate-300">
              {proposal.isSuggested
                ? "Тип достаточно подтверждён аналогами и может быть проверен пользователем."
                : "Оснований недостаточно для предложения типа."}
            </p>

            {proposal.blockingReasons.length > 0 && (
              <ul className="mt-3 grid gap-1 text-xs text-amber-200">
                {proposal.blockingReasons.map((reason) => (
                  <li key={reason}>• {reason}</li>
                ))}
              </ul>
            )}

            <p className="mt-3 text-xs text-slate-500">
              Тип не назначается автоматически.
            </p>

            {proposal.isSuggested && onApply && (
              <AppButton
                className="mt-4 w-full"
                variant="secondary"
                onClick={() => onApply(proposal)}
              >
                Подставить тип в форму
              </AppButton>
            )}
          </article>
        ))}
      </div>
    </div>
  );
}

function CharacteristicProposals({
  proposals,
  onApply,
  onReject,
}: {
  proposals: CatalogProductAnalogyCharacteristicProposal[];
  onApply?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
  ) => void;
  onReject?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
    reason: string,
  ) => void;
}) {
  const suggestedProposals = proposals.filter(
    (proposal) => proposal.isSuggested,
  );
  const blockedProposals = proposals.filter(
    (proposal) => !proposal.isSuggested,
  );

  return (
    <div className="mt-6 border-t border-white/10 pt-6">
      <h3 className="font-semibold text-white">
        Предложения для отсутствующих характеристик
      </h3>
      <p className="mt-2 text-sm leading-6 text-slate-400">
        Значение считается предложением только при поддержке минимум трёх
        подходящих товаров и согласии не ниже 80%. Это наблюдение по каталогу,
        а не подтверждённое правило.
      </p>

      {suggestedProposals.length === 0 ? (
        <div className="mt-4 rounded-2xl border border-white/10 bg-black/20 p-5 text-sm text-slate-300">
          Достаточно согласованных данных для предложения пока нет.
        </div>
      ) : (
        <div className="mt-4 grid gap-4 lg:grid-cols-2">
          {suggestedProposals.map((proposal) => (
            <CharacteristicProposalCard
              key={proposal.characteristicCode}
              proposal={proposal}
              onApply={onApply}
              onReject={onReject}
            />
          ))}
        </div>
      )}

      {blockedProposals.length > 0 && (
        <details className="mt-4 rounded-2xl border border-amber-500/20 bg-amber-500/[0.05] p-4">
          <summary className="cursor-pointer text-sm font-medium text-amber-100">
            Показать неподтверждённые варианты ({blockedProposals.length})
          </summary>
          <div className="mt-4 grid gap-4 lg:grid-cols-2">
            {blockedProposals.map((proposal) => (
              <CharacteristicProposalCard
                key={proposal.characteristicCode}
                proposal={proposal}
                onApply={onApply}
                onReject={onReject}
              />
            ))}
          </div>
        </details>
      )}
    </div>
  );
}

function CharacteristicProposalCard({
  proposal,
  onApply,
  onReject,
}: {
  proposal: CatalogProductAnalogyCharacteristicProposal;
  onApply?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
  ) => void;
  onReject?: (
    proposal: CatalogProductAnalogyCharacteristicProposal,
    reason: string,
  ) => void;
}) {
  const [rejectionReason, setRejectionReason] = useState("");
  const [decisionMessage, setDecisionMessage] = useState<string | null>(null);
  const alternatives = Object.entries(proposal.alternativeValues).sort(
    ([leftValue, leftCount], [rightValue, rightCount]) =>
      rightCount - leftCount || leftValue.localeCompare(rightValue),
  );

  return (
    <article
      className={`rounded-2xl border p-5 ${
        proposal.isSuggested
          ? "border-emerald-500/25 bg-emerald-500/[0.07]"
          : "border-amber-500/20 bg-black/20"
      }`}
    >
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <p className="font-mono text-xs text-slate-500">
            {proposal.characteristicCode}
          </p>
          <p className="mt-1 text-lg font-semibold text-white">
            {proposal.proposedValue}
          </p>
        </div>
        <span
          className={`rounded-full border px-3 py-1 text-xs ${
            proposal.isSuggested
              ? "border-emerald-500/25 bg-emerald-500/10 text-emerald-200"
              : "border-amber-500/25 bg-amber-500/10 text-amber-200"
          }`}
        >
          {proposal.isSuggested ? "Можно рассмотреть" : "Недостаточно оснований"}
        </span>
      </div>

      <p className="mt-3 text-sm text-slate-300">
        Поддержка: {proposal.supportingProductCount} из {proposal.productsWithCharacteristicCount}
        {" · "}
        {formatPercent(proposal.supportRatio)}
      </p>

      <p className="mt-2 text-xs text-slate-400">
        Режим: {formatAnalogyMode(proposal.mode)}
        {proposal.isAutoFillAllowed
          ? " · разрешено безопасное автозаполнение"
          : " · автоматическое заполнение не выполняется"}
      </p>

      {proposal.autoFillBlockingReason && (
        <p className="mt-2 text-xs text-amber-200">
          {proposal.autoFillBlockingReason}
        </p>
      )}

      {alternatives.length > 1 && (
        <p className="mt-2 text-xs text-slate-400">
          Варианты в аналогах: {alternatives.map(([value, count]) => `${value} — ${count}`).join("; ")}
        </p>
      )}

      {proposal.blockingReasons.length > 0 && (
        <ul className="mt-3 grid gap-1 text-xs text-amber-200">
          {proposal.blockingReasons.map((reason) => (
            <li key={reason}>• {reason}</li>
          ))}
        </ul>
      )}

      <p className="mt-3 text-xs text-slate-500">
        {onApply
          ? "Подстановка меняет только поле формы. Для записи строки нажмите обычную кнопку сохранения."
          : "Значение показано только для проверки и не сохраняется."}
      </p>

      {proposal.isSuggested && onApply && (
        <AppButton
          className="mt-4 w-full"
          variant="secondary"
          onClick={() => {
            onApply(proposal);
            setDecisionMessage("Предложение принято и подставлено в форму.");
          }}
        >
          Подставить значение в форму
        </AppButton>
      )}

      {proposal.isSuggested && onReject && (
        <div className="mt-4 grid gap-2">
          <label className="grid gap-2 text-xs text-slate-300">
            Причина отклонения
            <input
              value={rejectionReason}
              onChange={(event) => setRejectionReason(event.target.value)}
              placeholder="Почему значение не подходит"
              className="rounded-xl border border-white/10 bg-black/30 px-3 py-2 text-sm text-white outline-none focus:border-amber-400"
            />
          </label>
          <AppButton
            variant="ghost"
            disabled={!rejectionReason.trim()}
            onClick={() => {
              onReject(proposal, rejectionReason.trim());
              setDecisionMessage("Отклонение будет сохранено вместе со строкой.");
            }}
          >
            Отклонить предложение
          </AppButton>
        </div>
      )}

      {decisionMessage && (
        <p className="mt-3 text-xs text-teal-200">{decisionMessage}</p>
      )}
    </article>
  );
}

function ProductAnalogyCard({ match }: { match: CatalogProductAnalogyMatch }) {
  const characteristicEntries = Object.entries(match.characteristics).sort(
    ([leftCode], [rightCode]) => leftCode.localeCompare(rightCode),
  );

  return (
    <article className="rounded-2xl border border-white/10 bg-black/20 p-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h4 className="font-semibold text-white">{match.name}</h4>
          <p className="mt-1 text-sm text-slate-400">
            {match.article || "Без артикула"} · {match.productTypeName}
          </p>
          {match.matchedName !== match.name && (
            <p className="mt-1 text-xs text-slate-500">
              Совпавший псевдоним: {match.matchedName}
            </p>
          )}
        </div>

        <span className="rounded-full border border-cyan-500/25 bg-cyan-500/10 px-3 py-1 text-xs text-cyan-100">
          Совпало токенов: {match.sharedNameTokenCount}
        </span>
      </div>

      <div className="mt-4 flex flex-wrap gap-2 text-xs">
        {match.exactArticleMatch && (
          <EvidenceBadge label="Точный артикул" tone="positive" />
        )}
        {match.exactNameMatch && (
          <EvidenceBadge label="Точное наименование" tone="positive" />
        )}
        {match.matchedFamilyAnchor && (
          <EvidenceBadge
            label={`Семейство: ${match.matchedFamilyAnchor}`}
            tone="positive"
          />
        )}
        <EvidenceBadge
          label={`Покрытие слов: ${formatPercent(match.queryNameTokenCoverage)}`}
          tone="neutral"
        />
        <EvidenceBadge
          label={`Совпало значений: ${match.matchingCharacteristicCount}`}
          tone="positive"
        />
        <EvidenceBadge
          label={`Противоречий: ${match.conflictingCharacteristicCount}`}
          tone={match.conflictingCharacteristicCount > 0 ? "negative" : "neutral"}
        />
      </div>

      {match.sharedNameTokens.length > 0 && (
        <p className="mt-4 text-sm text-slate-300">
          Общие части наименования: {match.sharedNameTokens.join(", ")}
        </p>
      )}

      {!match.matchedFamilyAnchor && match.candidateFamilyAnchors.length > 0 && (
        <p className="mt-2 text-xs text-slate-500">
          Обозначения кандидата: {match.candidateFamilyAnchors.join(", ")}. Совпавшее семейство не найдено.
        </p>
      )}

      {match.conflictingCharacteristicCodes.length > 0 && (
        <p className="mt-2 text-sm text-red-200">
          Противоречащие характеристики: {match.conflictingCharacteristicCodes.join(", ")}
        </p>
      )}

      <details className="mt-4 rounded-xl border border-white/10 bg-white/[0.03] p-4">
        <summary className="cursor-pointer text-sm font-medium text-slate-200">
          Показать заполненные характеристики ({characteristicEntries.length})
        </summary>

        {characteristicEntries.length === 0 ? (
          <p className="mt-3 text-sm text-slate-400">
            У похожего товара нет заполненных характеристик.
          </p>
        ) : (
          <dl className="mt-4 grid gap-3 sm:grid-cols-2">
            {characteristicEntries.map(([code, value]) => (
              <div key={code} className="rounded-lg border border-white/10 bg-black/20 p-3">
                <dt className="font-mono text-xs text-slate-500">{code}</dt>
                <dd className="mt-1 break-words text-sm text-slate-200">{value}</dd>
              </div>
            ))}
          </dl>
        )}
      </details>
    </article>
  );
}

function EvidenceBadge({
  label,
  tone,
}: {
  label: string;
  tone: "positive" | "negative" | "neutral";
}) {
  const className =
    tone === "positive"
      ? "border-emerald-500/25 bg-emerald-500/10 text-emerald-200"
      : tone === "negative"
        ? "border-red-500/25 bg-red-500/10 text-red-200"
        : "border-white/10 bg-white/[0.04] text-slate-300";

  return (
    <span className={`rounded-full border px-3 py-1 ${className}`}>
      {label}
    </span>
  );
}

function formatPercent(value: number): string {
  return `${Math.round(value * 100)}%`;
}

function formatAnalogyMode(
  mode: CatalogProductAnalogyCharacteristicProposal["mode"],
): string {
  switch (mode) {
    case "Off":
      return "выключено";
    case "Shadow":
      return "только наблюдение";
    case "AutoFill":
      return "автозаполнение";
    default:
      return "предложение пользователю";
  }
}
