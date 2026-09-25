import { Suspense } from "react";
import { LearningWorkspace } from "@/features/catalogRecognition/ui/LearningWorkspace";

export default function LearningPage() {
  return <Suspense fallback={<p>Загрузка рабочего пространства…</p>}><LearningWorkspace /></Suspense>;
}
