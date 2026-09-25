"use client";

import { useState } from "react";
import { AppButton } from "@/shared/ui/AppButton";
import { EvaluationReportPanel } from "./EvaluationReportPanel";

export function EvaluationReportLookup() {
  const [value, setValue] = useState("");
  const [reportId, setReportId] = useState<string | null>(null);
  const [kind, setKind] = useState<"rules" | "dictionary">("rules");
  const valid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.trim());
  return <section className="grid gap-3 rounded border border-[var(--app-border)] p-4">
    <h2 className="font-semibold">Сохранённый отчёт оценки</h2>
    <p>Отчёт доступен автору по номеру, в том числе после удаления импорта. Оценка правил выполняется на странице импорта, оценка словаря — на странице предложений.</p>
    <form className="flex flex-wrap gap-2" onSubmit={event => { event.preventDefault(); if (valid) setReportId(value.trim()); }}>
      <label>Тип отчёта <select value={kind} onChange={event => { setKind(event.target.value as "rules" | "dictionary"); setReportId(null); }} className="rounded border bg-transparent p-2"><option value="rules">Версия правил</option><option value="dictionary">Словарное предложение</option></select></label>
      <label>Номер отчёта <input value={value} onChange={event => setValue(event.target.value)} className="rounded border border-[var(--app-border)] bg-transparent p-2" /></label>
      <AppButton type="submit" disabled={!valid}>Открыть отчёт</AppButton>
    </form>
    {reportId && <EvaluationReportPanel key={`${kind}-${reportId}`} reportId={reportId} kind={kind} />}
  </section>;
}
