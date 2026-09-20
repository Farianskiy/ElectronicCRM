"use client";

import { useEffect, useRef, useState } from "react";

interface SpeechRecognitionAlternativeLike {
  transcript: string;
}

interface SpeechRecognitionResultLike {
  [index: number]: SpeechRecognitionAlternativeLike;
}

interface SpeechRecognitionResultListLike {
  length: number;
  [index: number]: SpeechRecognitionResultLike;
}

interface SpeechRecognitionEventLike {
  results: SpeechRecognitionResultListLike;
}

interface SpeechRecognitionErrorEventLike {
  error: string;
}

interface SpeechRecognitionLike {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  maxAlternatives: number;

  onstart: (() => void) | null;
  onend: (() => void) | null;
  onresult: ((event: SpeechRecognitionEventLike) => void) | null;
  onerror: ((event: SpeechRecognitionErrorEventLike) => void) | null;

  start(): void;
  stop(): void;
  abort(): void;
}

interface SpeechRecognitionConstructorLike {
  new (): SpeechRecognitionLike;
}

type SpeechRecognitionWindow = Window & {
  SpeechRecognition?: SpeechRecognitionConstructorLike;
  webkitSpeechRecognition?: SpeechRecognitionConstructorLike;
};

interface VoiceInputButtonProps {
  onTranscript: (transcript: string) => void;
  disabled?: boolean;
}

function getRecognitionErrorMessage(error: string): string {
  switch (error) {
    case "not-allowed":
    case "service-not-allowed":
      return "Разрешите сайту доступ к микрофону.";

    case "no-speech":
      return "Речь не распознана. Попробуйте ещё раз.";

    case "audio-capture":
      return "Микрофон не найден или недоступен.";

    case "network":
      return "Не удалось подключиться к сервису распознавания.";

    default:
      return "Не удалось распознать речь.";
  }
}

export function VoiceInputButton({
  onTranscript,
  disabled = false,
}: VoiceInputButtonProps) {
  const recognitionRef = useRef<SpeechRecognitionLike | null>(null);

  const [isListening, setIsListening] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  useEffect(() => {
    return () => {
      recognitionRef.current?.abort();
      recognitionRef.current = null;
    };
  }, []);

  function handleClick() {
    if (isListening) {
      recognitionRef.current?.stop();
      return;
    }

    setErrorMessage(null);

    if (!window.isSecureContext) {
      setErrorMessage(
        "Голосовой ввод работает только через HTTPS или на localhost.",
      );
      return;
    }

    const speechWindow = window as SpeechRecognitionWindow;

    const SpeechRecognitionConstructor =
      speechWindow.SpeechRecognition ?? speechWindow.webkitSpeechRecognition;

    if (!SpeechRecognitionConstructor) {
      setErrorMessage("Этот браузер не поддерживает голосовой ввод.");
      return;
    }

    const recognition = new SpeechRecognitionConstructor();

    recognition.lang = "ru-RU";
    recognition.continuous = false;
    recognition.interimResults = false;
    recognition.maxAlternatives = 1;

    recognition.onstart = () => {
      setIsListening(true);
    };

    recognition.onresult = (event) => {
      const lastResult = event.results[event.results.length - 1];

      const transcript = lastResult?.[0]?.transcript.trim() ?? "";

      if (transcript.length > 0) {
        onTranscript(transcript);
      }
    };

    recognition.onerror = (event) => {
      setErrorMessage(getRecognitionErrorMessage(event.error));
    };

    recognition.onend = () => {
      setIsListening(false);
      recognitionRef.current = null;
    };

    recognitionRef.current = recognition;

    try {
      recognition.start();
    } catch {
      recognitionRef.current = null;
      setIsListening(false);
      setErrorMessage("Не удалось включить голосовой ввод.");
    }
  }

  return (
    <div className="grid gap-2">
      <button
        type="button"
        disabled={disabled}
        onClick={handleClick}
        aria-pressed={isListening}
        title={isListening ? "Остановить запись" : "Начать голосовой ввод"}
        className={[
          "inline-flex min-h-11 items-center justify-center gap-2",
          "rounded-xl border px-4 py-2 text-sm font-semibold",
          "transition-colors motion-reduce:transition-none",
          "focus-visible:outline-none focus-visible:ring-2",
          "focus-visible:ring-[var(--app-accent)]",
          "disabled:cursor-not-allowed disabled:opacity-50",
          isListening
            ? "border-[var(--app-danger-border)] bg-[var(--app-danger-soft)] text-[var(--app-danger)]"
            : "border-[var(--app-border)] bg-[var(--app-surface)] text-[var(--app-text)] hover:border-[var(--app-accent-border)] hover:text-[var(--app-accent)]",
        ].join(" ")}
      >
        <svg
          aria-hidden="true"
          viewBox="0 0 24 24"
          fill="none"
          className="h-5 w-5"
        >
          <rect
            x="9"
            y="3"
            width="6"
            height="11"
            rx="3"
            stroke="currentColor"
            strokeWidth="2"
          />

          <path
            d="M5 11a7 7 0 0 0 14 0M12 18v3M9 21h6"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
          />
        </svg>

        {isListening ? "Слушаю..." : "Говорить"}
      </button>

      {errorMessage && (
        <p
          role="alert"
          className="max-w-xs text-xs leading-5 text-[var(--app-danger)]"
        >
          {errorMessage}
        </p>
      )}
    </div>
  );
}
