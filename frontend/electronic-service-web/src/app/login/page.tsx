"use client";

import { useMutation } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import type { FormEvent } from "react";
import { useState } from "react";
import { login } from "@/features/auth/api/login";
import { setAuthSession } from "@/shared/api/authToken";
import Link from "next/link";

export default function LoginPage() {
  const router = useRouter();

  const [email, setEmail] = useState("admin@test.local");
  const [password, setPassword] = useState("Admin12345!");

  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: (data) => {
      setAuthSession({
        accessToken: data.accessToken,
        userId: data.userId,
        userType: data.userType,
        displayName: data.displayName,
      });

      router.push("/");
    },
  });

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    loginMutation.mutate({
      email,
      password,
    });
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-100 px-4">
      <section className="w-full max-w-md rounded-2xl bg-white p-8 shadow-lg">
        <h1 className="mb-6 text-2xl font-bold text-slate-900">
          Вход в Electronic Service
        </h1>

        <form onSubmit={handleSubmit} className="grid gap-4">
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
              autoComplete="current-password"
              onChange={(event) => setPassword(event.target.value)}
            />
          </label>

          {loginMutation.isError && (
            <p className="text-sm text-red-600">
              Не удалось войти. Проверь email и пароль.
            </p>
          )}

          <button
            className="rounded-lg bg-blue-600 px-4 py-2 font-medium text-white disabled:opacity-60"
            type="submit"
            disabled={loginMutation.isPending}
          >
            {loginMutation.isPending ? "Входим..." : "Войти"}
          </button>
        </form>
        <p className="mt-4 text-sm text-slate-600">
        Нет аккаунта?{" "}
        <Link className="font-medium text-blue-600" href="/register">
            Зарегистрироваться
        </Link>
        </p>
      </section>
    </main>
  );
}