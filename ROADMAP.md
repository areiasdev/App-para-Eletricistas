# TécnicoApp — Roadmap & Próximos Passos

## Estado Actual (v0.1 — Setup + Auth)

### ✅ Feito
- Solução .NET 10 com Clean Architecture (Domain, Application, Infrastructure, API)
- Entidades de domínio: User, Client, Quote, QuoteLine, Equipment, Intervention
- BaseEntity com IsDeleted (soft delete), ModifiedBy, ModifiedAt
- Auth completa: Register, Login, RefreshToken (JWT 60min + Refresh 30 dias)
- EF Core configurações: jsonb para fotos, índices, query filters para soft delete
- Swagger com Bearer auth
- Serilog + ExceptionMiddleware (RFC 7807 ProblemDetails)
- Docker Compose com PostgreSQL 16
- Next.js 14 com App Router, Tailwind, shadcn/ui (base)
- TanStack Query + Zustand auth store
- Páginas /login e /register com validação Zod
- Axios client com interceptor de refresh token automático
- Formatters PT-PT (moeda, data, validação NIF)

---

## Próximas Versões

### v0.2 — Clientes (próximo)
**Backend:**
- [ ] `GetClientsQuery` — lista paginada com pesquisa por nome/email
- [ ] `GetClientByIdQuery` — detalhe com histórico (orçamentos, intervenções, equipamentos)
- [ ] `CreateClientCommand` + validator
- [ ] `UpdateClientCommand` + validator
- [ ] `DeleteClientCommand` (soft delete)
- [ ] `ClientsController` com `/api/v1/clients`
- [ ] Primeira migration EF Core (`InitialCreate`)

**Frontend:**
- [ ] `/dashboard/clientes` — listagem com pesquisa e paginação
- [ ] `/dashboard/clientes/novo` — formulário de criação
- [ ] `/dashboard/clientes/[id]` — página de detalhe
- [ ] `DataTable` component reutilizável (shadcn)
- [ ] Layout do dashboard com sidebar e navegação
- [ ] Hook `useClients`

### v0.3 — Orçamentos (módulo core)
**Backend:**
- [ ] Numeração automática: `ORC-2025-0001` (sequência por userId + ano)
- [ ] `CreateQuoteCommand` com linhas, IVA, desconto
- [ ] `UpdateQuoteCommand` (só Draft)
- [ ] `SendQuoteCommand` — muda estado para Sent, envia email
- [ ] `UpdateQuoteStatusCommand` — transições de estado válidas
- [ ] `GetQuotesQuery` — lista paginada com filtro por estado
- [ ] `QuotesController`

**Frontend:**
- [ ] `/dashboard/orcamentos` — listagem com badges de estado
- [ ] `/dashboard/orcamentos/novo` — wizard: cliente → linhas → preview
- [ ] Componente `QuoteStatusBadge`
- [ ] Componente `QuoteLineEditor` (add/remove/edit linhas)

### v0.4 — PDF + Email
**Backend:**
- [ ] Adicionar QuestPDF à Infrastructure
- [ ] `PdfService` — gera PDF com logo, dados empresa/cliente, NIF, linhas, IVA, total
- [ ] `EmailService` — Resend ou SendGrid via HTTP
- [ ] `GenerateQuotePdfCommand`
- [ ] Upload PDF para Azure Blob ou S3

**Frontend:**
- [ ] Botão "Descarregar PDF" na página do orçamento
- [ ] Botão "Enviar por Email"

### v0.5 — Dashboard
**Backend:**
- [ ] `GetDashboardQuery` — total orçamentos mês (€ e qtd), taxa aceitação, próximas manutenções
- [ ] `DashboardController`

**Frontend:**
- [ ] Cards de métricas no dashboard
- [ ] Gráfico de orçamentos por estado (recharts ou chart.js)
- [ ] Lista de próximas manutenções

### v0.6 — Equipamentos + Alertas
**Backend:**
- [ ] CRUD Equipamentos (`EquipmentController`)
- [ ] Adicionar Hangfire à Infrastructure
- [ ] Job: `MaintenanceAlertJob` — corre diariamente, envia email 7 dias antes
- [ ] Toggle por cliente (enviar alerta ao cliente sim/não)

### v0.7 — Intervenções / Ordens de Serviço
**Backend:**
- [ ] CRUD Intervenções
- [ ] Associação a equipamentos (many-to-many)
- [ ] PDF de relatório de intervenção (QuestPDF)
- [ ] Upload de fotos antes/depois

### v0.8 — Assinatura Digital (Pro)
**Backend:**
- [ ] Token único por orçamento (GUID urlsafe, expira em 72h)
- [ ] `POST /api/v1/quotes/{id}/sign` — endpoint público, guarda imagem da assinatura
- [ ] Reembed assinatura no PDF final
- [ ] Guard de plano: só Pro e Team

**Frontend:**
- [ ] `/sign/[token]` — página pública, canvas de assinatura
- [ ] Componente de assinatura touch-friendly

### v0.9 — Stripe + Planos
**Backend:**
- [ ] Stripe webhook handler
- [ ] `SubscriptionService` — criar/cancelar subscrição
- [ ] Guards de plano em commands críticos (ex: limite de 5 orçamentos/mês no Free)
- [ ] `PlansController`

**Frontend:**
- [ ] Página de planos e preços
- [ ] Stripe Checkout redirect
- [ ] Banner de upgrade quando limite atingido

---

## Decisões de Arquitectura Tomadas

| Decisão | Escolha | Razão |
|---|---|---|
| Soft delete | `IsDeleted` em BaseEntity | Query filters globais, simples |
| Auth | JWT custom (sem ASP.NET Identity) | Mais leve, controlo total, compatível com mobile futuro |
| ORM | EF Core 8 com Npgsql | Postgres jsonb nativo para fotos/materiais |
| Erros | Ardalis.Result (não exceções) | Erros de negócio explícitos no tipo de retorno |
| Passwords | BCrypt.Net-Next | Padrão da indústria, simples |
| Solution format | `.slnx` (.NET 10) | `dotnet build TecnicoApp.slnx` |

## Notas Técnicas Importantes

- **Build:** `dotnet build TecnicoApp.slnx` (não `.sln`)
- **Migrations:** sempre com `--project src/TecnicoApp.Infrastructure --startup-project src/TecnicoApp.API`
- **AutoMapper removido** — vulnerabilidade NU1903; usar mapeamento manual com records
- **Frontend env:** `NEXT_PUBLIC_API_URL` no `.env.local` (padrão: `http://localhost:5000`)
- **Porta API dev:** confirmar em `launchSettings.json` do API project
