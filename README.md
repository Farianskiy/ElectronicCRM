# ElectronicCRM

ElectronicCRM — веб-приложение для управления электронным каталогом товаров, характеристиками, типами продукции и импортом данных из Excel.

> Текущая версия: `0.1.0-beta.1`  
> Статус: первая тестовая beta-версия.

## Возможности

- регистрация и авторизация пользователей через JWT;
- управление профилем пользователя;
- каталог товаров;
- управление типами товаров;
- управление определениями характеристик;
- настройка характеристик для типов товаров;
- импорт товарного каталога из XLSX;
- сопоставление колонок импортируемого файла;
- исправление ошибок импорта;
- очередь проверки импортов;
- формирование XLSX-отчёта с ошибками;
- словарь нормализации значений;
- страницы помощника каталога и предложений;
- автоматические миграции и начальное заполнение PostgreSQL;
- health checks для приложения и базы данных;
- запуск полного стека через Docker Compose.

## Технологии

### Backend

- .NET 10;
- ASP.NET Core;
- Entity Framework Core;
- PostgreSQL;
- Npgsql;
- Dapper;
- FluentValidation;
- ClosedXML;
- JWT Bearer Authentication;
- Scalar и OpenAPI;
- C# nullable reference types;
- централизованное управление версиями NuGet-пакетов;
- Roslynator, Meziantou, AsyncFixer и SonarAnalyzer.

### Frontend

- Next.js 16;
- React 19;
- TypeScript;
- Tailwind CSS 4;
- TanStack Query;
- Axios;
- React Hook Form;
- Zod.

### Infrastructure

- Docker;
- Docker Compose;
- PostgreSQL 18;
- production multi-stage images;
- запуск контейнеров без root-прав;
- Docker health checks;
- именованные volumes для PostgreSQL и ASP.NET Core Data Protection.

## Архитектура

```text
ElectronicCRM
├── frontend
│   └── electronic-service-web
│       └── Next.js
│
├── backend
│   └── ElectronicService
│       ├── ElectronicService.Domain
│       ├── ElectronicService.Contracts
│       ├── ElectronicService.Core
│       ├── ElectronicService.Infrastructure.Postgres
│       └── ElectronicService.Web
│
└── docker-compose.yml
    ├── postgres
    ├── api
    └── web
```

Зависимости backend направлены от внешних слоёв к внутренним:

```text
Web
 ├── Core
 ├── Contracts
 ├── Domain
 └── Infrastructure.Postgres
```

## Быстрый запуск через Docker

### Требования

- Git;
- Docker Desktop;
- Linux containers;
- Docker Compose v2.

Проверьте окружение:

```powershell
docker version
docker compose version
docker info --format "{{.OSType}}"
```

Последняя команда должна вывести:

```text
linux
```

### Клонирование

```powershell
git clone https://github.com/Farianskiy/ElectronicCRM.git
cd ElectronicCRM
git checkout release/v0.1.0-beta.1
```

### Настройка окружения

Создайте локальный `.env`:

```powershell
Copy-Item .\.env.example .\.env
notepad .\.env
```

Обязательно замените:

```dotenv
POSTGRES_PASSWORD=replace-with-strong-password
JWT_SECRET_KEY=replace-with-random-secret-at-least-32-characters
```

JWT-секрет должен содержать не менее 32 символов.

Если используется уже существующий PostgreSQL volume, пароль в `.env` должен совпадать с паролем пользователя, сохранённым внутри базы.

### PostgreSQL volume

Корневой Compose использует внешний именованный volume:

```text
backend_postgres_data
```

Создайте его перед первым запуском:

```powershell
docker volume create backend_postgres_data
```

Если volume уже существует, Docker просто вернёт его имя.

### Запуск

```powershell
docker compose up --build -d
```

Проверка контейнеров:

```powershell
docker compose ps
```

Ожидаемое состояние:

```text
electroniccrm-postgres   Up (healthy)
electroniccrm-api        Up (healthy)
electroniccrm-web        Up (healthy)
```

## Адреса приложения

| Компонент | Адрес |
|---|---|
| Frontend | http://localhost:3000 |
| Backend API | http://localhost:8080 |
| Liveness | http://localhost:8080/health/live |
| Readiness | http://localhost:8080/health/ready |
| PostgreSQL | localhost:5434 |

OpenAPI и Scalar доступны только при запуске backend не в `Production`.

## Проверка приложения

```powershell
Invoke-WebRequest http://localhost:8080/health/live
Invoke-WebRequest http://localhost:8080/health/ready
Invoke-WebRequest http://localhost:3000/login
```

Каждый запрос должен вернуть HTTP `200`.

## Работа с Docker Compose

### Просмотр логов

```powershell
docker compose logs --tail 100
```

Логи конкретного сервиса:

```powershell
docker compose logs --follow postgres
docker compose logs --follow api
docker compose logs --follow web
```

### Остановка

```powershell
docker compose stop
```

### Повторный запуск

```powershell
docker compose start
```

### Удаление контейнеров с сохранением данных

```powershell
docker compose down
```

### Пересборка всего приложения

```powershell
docker compose up --build -d
```

### Пересборка только backend

```powershell
docker compose up --build -d api
```

### Пересборка только frontend

```powershell
docker compose up --build -d web
```

После изменения `NEXT_PUBLIC_API_BASE_URL` frontend необходимо пересобрать, поскольку переменные `NEXT_PUBLIC_*` встраиваются во время `next build`.

## Важное предупреждение о данных

Не используйте без необходимости:

```powershell
docker compose down --volumes
```

Команда удаляет volumes, которыми управляет Compose. PostgreSQL volume настроен как внешний, но полагаться на случайную защиту данных вместо резервной копии не следует.

## Локальная разработка

### Только PostgreSQL в Docker

```powershell
cd backend
docker compose up -d
```

Этот Compose поднимает PostgreSQL на порту `5434`.

Не запускайте одновременно локальный backend Compose и корневой Compose: оба используют одинаковый порт PostgreSQL.

### Backend

Из корня репозитория:

```powershell
dotnet run `
  --project .\backend\ElectronicService\src\ElectronicService.Web\ElectronicService.Web.csproj
```

### Frontend

```powershell
cd .\frontend\electronic-service-web

npm.cmd install
npm.cmd run dev
```

Для локального frontend создайте `.env.local`:

```dotenv
NEXT_PUBLIC_API_BASE_URL=http://localhost:8080
```

## Проверки перед коммитом

### Backend

```powershell
dotnet build `
  .\backend\ElectronicService\src\ElectronicService.Web\ElectronicService.Web.csproj `
  --configuration Release
```

### Frontend

```powershell
cd .\frontend\electronic-service-web

npm.cmd run lint -- --max-warnings=0
npm.cmd run build
```

### Docker Compose

```powershell
cd F:\Projects\ElectronicCRM

docker compose config --quiet
docker compose up --build -d
docker compose ps
```

## Переменные окружения

| Переменная | Назначение |
|---|---|
| `APP_VERSION` | Тег Docker-образов приложения |
| `POSTGRES_IMAGE` | Образ PostgreSQL |
| `POSTGRES_DB` | Имя базы данных |
| `POSTGRES_USER` | Пользователь PostgreSQL |
| `POSTGRES_PASSWORD` | Пароль PostgreSQL |
| `POSTGRES_PORT` | Порт PostgreSQL на хосте |
| `POSTGRES_VOLUME_NAME` | Имя PostgreSQL volume |
| `JWT_ISSUER` | Издатель JWT |
| `JWT_AUDIENCE` | Аудитория JWT |
| `JWT_SECRET_KEY` | Ключ подписи JWT |
| `JWT_EXPIRATION_MINUTES` | Время жизни JWT |
| `BACKEND_PORT` | Порт backend на хосте |
| `FRONTEND_PORT` | Порт frontend на хосте |
| `FRONTEND_ORIGIN` | Разрешённый CORS origin |
| `NEXT_PUBLIC_API_BASE_URL` | URL API для браузерного frontend |
| `CATALOG_IMPORT_RETENTION_DAYS` | Срок хранения незавершённых импортов |

## Ограничения beta-версии

- версия предназначена для тестирования;
- автоматизированные интеграционные тесты импорта отложены;
- HTTPS и reverse proxy не входят в Docker Compose;
- резервное копирование PostgreSQL пока не автоматизировано;
- публикация Docker-образов в registry пока не настроена;
- CI/CD пока не настроен.

## Версия

```text
0.1.0-beta.1
```