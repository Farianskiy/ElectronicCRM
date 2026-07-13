"use client";

import { useMutation } from "@tanstack/react-query";
import Link from "next/link";
import { useRouter } from "next/navigation";
import type { FormEvent } from "react";
import { useState } from "react";
import { registerRegularUser } from "@/features/auth/api/registerRegularUser";

export default function RegisterPage() {
  const router = useRouter();

  const [displayName, setDisplayName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const registerMutation = useMutation({
    mutationFn: registerRegularUser,
    onSuccess: () => {
      router.push("/login");
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    registerMutation.mutate({
      displayName,
      email,
      password,
    });
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-100 px-4">
      <section className="w-full max-w-md rounded-2xl bg-white p-8 shadow-lg">
        <h1 className="mb-6 text-2xl font-bold text-slate-900">
          Регистрация
        </h1>

        <form onSubmit={handleSubmit} className="grid gap-4">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-700">Имя</span>
            <input
              className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-blue-500"
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-700">Email</span>
            <input
              className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-blue-500"
              type="email"
              value={email}
              autoComplete="email"
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-700">Пароль</span>
            <input
              className="rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-blue-500"
              type="password"
              value={password}
              autoComplete="new-password"
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>

          {registerMutation.isError && (
            <p className="text-sm text-red-600">
              Не удалось зарегистрироваться. Возможно, email уже занят.
            </p>
          )}

          <button
            className="rounded-lg bg-blue-600 px-4 py-2 font-medium text-white disabled:opacity-60"
            type="submit"
            disabled={registerMutation.isPending}
          >
            {registerMutation.isPending
              ? "Создаём аккаунт..."
              : "Зарегистрироваться"}
          </button>
        </form>

        <p className="mt-4 text-sm text-slate-600">
          Уже есть аккаунт?{" "}
          <Link className="font-medium text-blue-600" href="/login">
            Войти
          </Link>
        </p>
      </section>
    </main>
  );
}