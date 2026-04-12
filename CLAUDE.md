# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Project Overview

**TécnicoApp** — SaaS para eletricistas e técnicos de AVAC em Portugal. Permite criar orçamentos, gerir clientes/equipamentos e agendar manutenções. Target: técnico individual ou empresa 1-10 pessoas, €20-60/mês.

Monorepo com backend .NET 10 (Clean Architecture + CQRS) e frontend Next.js 14.

## Development Commands

### Backend

```bash
# Iniciar API com hot reload
dotnet watch run --project src/TecnicoApp.API

# Build da solução
dotnet build TecnicoApp.slnx

# Testes
dotnet test

# Migrations EF Core (executar a partir da raiz)
dotnet ef migrations add <NomeMigracao> --project src/TecnicoApp.Infrastructure --startup-project src/TecnicoApp.API
dotnet ef database update --project src/TecnicoApp.Infrastructure --startup-project src/TecnicoApp.API
```

### Frontend

```bash
cd tecnico-app
npm run dev       # Next.js dev server em :3000
npm run build     # Build de produção
npm run lint
npm run type-check
```

### Infraestrutura local (Docker)

```bash
# Iniciar PostgreSQL
docker compose up -d

# Parar
docker compose down
```

## Architecture

### Backend — Clean Architecture

```
src/
  TecnicoApp.Domain/          # Entidades, Enums, Value Objects (sem dependências)
  TecnicoApp.Application/     # MediatR handlers, FluentValidation, interfaces, DTOs
  TecnicoApp.Infrastructure/  # EF Core + Npgsql, TokenService, CurrentUserService
  TecnicoApp.API/             # ASP.NET 10 — Controllers, Middleware, Program.cs
```

**Direção de dependências:** Domain ← Application ← Infrastructure ← API

**Fluxo de request:** HTTP → Controller (thin) → MediatR → Handler → Domain/Infrastructure

### Convenções críticas

- **Controllers são thin** — recebem request, fazem `mediator.Send(command)`, devolvem resultado. Nunca lógica de negócio nos controllers.
- **UserId vem sempre do JWT** via `ICurrentUserService` — nunca do body do request.
- **Soft delete** via `IsDeleted` em `BaseEntity` — query filters em todas as entidades.
- **Result<T>** (Ardalis.Result) para erros de negócio; exceções apenas para situações inesperadas.
- **AsNoTracking()** em todas as queries de leitura.
- **CancellationToken** em todos os handlers e repositórios.

### Auth

JWT Bearer com refresh token (30 dias). Endpoints públicos: `POST /api/v1/auth/register`, `POST /api/v1/auth/login`, `POST /api/v1/auth/refresh`.

### EF Core

- Configurações em `IEntityTypeConfiguration<T>` — nunca Data Annotations nas entidades.
- Migrations com nomes descritivos: `AddEquipmentPhotosColumn`, não `Migration20250412`.
- `Photos` e `Materials` persistidos como `jsonb` no Postgres.

## Configuration

**Backend** — `appsettings.Development.json` tem credenciais de dev (Postgres local). Em produção usar env vars ou Azure Key Vault.

**Secrets/env vars necessários:**
- `ConnectionStrings__DefaultConnection`
- `Jwt__Secret` (mínimo 32 caracteres)
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiryMinutes`
- `Cors__AllowedOrigins`

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | Next.js 14 + Tailwind CSS + shadcn/ui |
| Backend | ASP.NET Core 10 Web API |
| Base de dados | PostgreSQL 16 |
| ORM | Entity Framework Core 8 |
| Auth | JWT Bearer (sem Identity) |
| Validação | FluentValidation 11 |
| CQRS | MediatR 12 |
| PDF | QuestPDF (a adicionar) |
| Jobs | Hangfire (a adicionar) |
| Pagamentos | Stripe (a adicionar) |

## Módulos (por ordem de desenvolvimento)

1. ✅ **Setup** — solução .NET, Next.js, Docker
2. ✅ **Auth** — registo, login, JWT, refresh token
3. ⬜ **Clientes** — CRUD completo
4. ⬜ **Orçamentos** — criação, linhas, estados, numeração automática
5. ⬜ **PDF** — QuestPDF, envio por email
6. ⬜ **Dashboard** — métricas básicas
7. ⬜ **Equipamentos + Alertas** — Hangfire, email 7 dias antes
8. ⬜ **Intervenções / Ordens de Serviço**
9. ⬜ **Assinatura digital** (Pro)
10. ⬜ **Stripe + Planos**
