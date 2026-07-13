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
    <main className="flex min-h-screen items-center justify-center bg-[#0f1115] px-4 text-slate-100">
      <section className="w-full max-w-md rounded-3xl border border-white/10 bg-white/[0.04] p-8 shadow-2xl">
        <h1 className="text-2xl font-bold text-white">Регистрация</h1>

        <p className="mt-2 text-sm text-slate-400">
          Создаётся обычный пользователь. Технического пользователя создавай
          через backend/Scalar.
        </p>

        <form onSubmit={handleSubmit} className="mt-6 grid gap-4">
          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">Имя</span>
            <input
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
              value={displayName}
              onChange={(event) => setDisplayName(event.target.value)}
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">Email</span>
            <input
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
              type="email"
              value={email}
              autoComplete="email"
              onChange={(event) => setEmail(event.target.value)}
            />
          </label>

          <label className="grid gap-2">
            <span className="text-sm font-medium text-slate-300">Пароль</span>
            <input
              className="rounded-2xl border border-white/10 bg-black/30 px-4 py-3 text-slate-100 outline-none placeholder:text-slate-600 focus:border-teal-400"
              type="password"
              value={password}
              autoComplete="new-password"
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>

          {registerMutation.isError && (
            <p className="rounded-2xl border border-red-500/30 bg-red-500/10 p-3 text-sm text-red-200">
              Не удалось зарегистрироваться. Возможно, email уже занят.
            </p>
          )}

          <button
            className="rounded-2xl bg-teal-500 px-4 py-3 font-medium text-white disabled:opacity-60"
            type="submit"
            disabled={registerMutation.isPending}
          >
            {registerMutation.isPending
              ? "Создаём аккаунт..."
              : "Зарегистрироваться"}
          </button>
        </form>

        <p className="mt-5 text-sm text-slate-400">
          Уже есть аккаунт?{" "}
          <Link href="/login" className="font-medium text-teal-300">
            Войти
          </Link>
        </p>
      </section>
    </main>
  );
}