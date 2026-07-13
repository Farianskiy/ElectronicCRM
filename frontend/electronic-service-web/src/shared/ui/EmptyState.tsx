interface EmptyStateProps {
  title: string;
  description?: string;
}

export function EmptyState({ title, description }: EmptyStateProps) {
  return (
    <section className="rounded-3xl border border-white/10 bg-white/[0.04] p-6">
      <h2 className="text-xl font-semibold text-white">{title}</h2>

      {description && (
        <p className="mt-2 text-sm text-slate-400">{description}</p>
      )}
    </section>
  );
}