interface PageHeaderProps {
  title: string;
  description?: string;
}

export function PageHeader({ title, description }: PageHeaderProps) {
  return (
    <header className="mb-6">
      <h1 className="text-3xl font-bold text-white">{title}</h1>

      {description && (
        <p className="mt-2 text-sm text-slate-400">{description}</p>
      )}
    </header>
  );
}