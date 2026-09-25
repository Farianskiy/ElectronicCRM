"use client";
import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { Suspense, type ComponentProps } from "react";

type Props = Omit<ComponentProps<typeof Link>, "href"> & { href: string };
function ContextLink(props: Props) {
  const params = useSearchParams();
  const value = params.get("learningReturn");
  const returnTo = value?.startsWith("/catalog/recognition/learning?")
    ? value
    : null;
  const href = returnTo
    ? `${props.href}${props.href.includes("?") ? "&" : "?"}learningReturn=${encodeURIComponent(returnTo)}`
    : props.href;
  return <Link {...props} href={href} />;
}
export function LearningContextLink(props: Props) {
  return (
    <Suspense fallback={<Link {...props} />}>
      <ContextLink {...props} />
    </Suspense>
  );
}
function ReturnLink() {
  const params = useSearchParams();
  const value = params.get("learningReturn");

  return value?.startsWith("/catalog/recognition/learning?") ? (
    <Link href={value}>Вернуться к обучению выбранных товаров</Link>
  ) : null;
}
export function LearningReturnLink() {
  return (
    <Suspense>
      <ReturnLink />
    </Suspense>
  );
}
