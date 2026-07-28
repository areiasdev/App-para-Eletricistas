# TécnicoApp

BackOffice para eletricistas, técnicos de AVAC e pequenas empresas de manutenção em Portugal — orçamentos, faturação, gestão de clientes/equipamentos/intervenções, portal do cliente e cobrança de faturas online.

**Modelo de venda: uma instalação por cliente.** Cada empresa que compra o produto recebe a sua própria instalação self-hosted (própria base de dados, próprias credenciais Stripe/Twilio/SMTP) — não é um SaaS multi-tenant partilhado. Isto está deliberado; não reintroduzir faturação/subscrição partilhada.

## Stack

| Camada | Tecnologia |
|---|---|
| Backend / API | ASP.NET Core 10, Clean Architecture (Domain/Application/Infrastructure/API), MediatR, EF Core + Npgsql |
| Frontend | Next.js 16 (App Router) + TypeScript + Tailwind v4 |
| Base de dados | PostgreSQL 16 |
| Jobs agendados | Hangfire (armazenamento em Postgres) |
| PDFs | QuestPDF |
| Email | SMTP via MailKit |
| Pagamentos | Stripe Checkout (cartão + MB WAY nativo) |
| Notificações | Twilio (SMS + WhatsApp Business API) |

## Desenvolvimento local

Pré-requisitos: .NET 10 SDK, Node 20+, Docker (para o Postgres local) ou uma instância Postgres já corrente.

```bash
# Base de dados local
cp docker-compose.env.example docker-compose.env   # define uma password
docker compose up -d                                 # sobe só o Postgres, porta 5432

# Backend — a partir da raiz do repo
cp src/TecnicoApp.API/appsettings.Development.example.json src/TecnicoApp.API/appsettings.Development.json
# edita a connection string e (opcionalmente) SMTP/Stripe/Twilio de teste
dotnet ef database update --project src/TecnicoApp.Infrastructure --startup-project src/TecnicoApp.API
dotnet watch run --project src/TecnicoApp.API      # http://localhost:5092

# Frontend
cd tecnico-app
npm install
npm run dev                                          # http://localhost:3000
```

`appsettings.Development.json` está no `.gitignore` — nunca commitar credenciais reais, mesmo de teste.

### Testes

```bash
dotnet test TecnicoApp.slnx                          # backend — xUnit + NSubstitute + EF InMemory
cd tecnico-app && npm test -- --run                   # frontend — Vitest
```

## Deploy em produção (uma instalação por cliente)

`docker-compose.prod.yml` sobe três serviços: `postgres`, `api` (porta 8080 internamente) e `frontend` (porta 3000). A `api` e a `frontend` correm em containers separados atrás de um reverse proxy à tua escolha (Caddy, Nginx, Traefik — não incluído neste repo).

```bash
cp docker-compose.prod.env.example docker-compose.prod.env
# preenche TODAS as variáveis marcadas "must be set" abaixo antes de continuar
docker compose -f docker-compose.prod.yml --env-file docker-compose.prod.env up -d --build
```

### Variáveis de ambiente obrigatórias

| Variável | Obrigatória | Nota |
|---|---|---|
| `POSTGRES_PASSWORD` | ✅ | password forte, única por instalação |
| `JWT_SECRET` | ✅ | mín. 32 caracteres aleatórios (`openssl rand -hex 32`) |
| `FRONTEND_URL` | ✅ | URL pública do frontend — usado em CORS, emails, links de pagamento |
| `NEXT_PUBLIC_API_URL` | ✅ | URL pública da API — fica embutido no build do Next.js, não pode mudar sem rebuild |

### Variáveis opcionais (a app funciona sem, mas essa funcionalidade fica desativada)

| Variável | Efeito se vazia |
|---|---|
| `SMTP_*` | Emails (orçamentos, faturas, convites de equipa, portal do cliente) ficam só registados em log, nunca enviados. Isto bloqueia efetivamente o produto para um cliente real — configurar antes de entregar a instalação. |
| `STRIPE_SECRET_KEY` / `STRIPE_WEBHOOK_SECRET` / `STRIPE_PUBLISHABLE_KEY` | O botão "Faturar" e o PDF da fatura continuam a funcionar; só o link de pagamento online fica indisponível. Cada instalação usa a **sua própria** conta Stripe — nunca partilhar chaves entre clientes. Depois de criar o webhook endpoint no dashboard Stripe (`{FRONTEND_URL da API}/api/v1/webhooks/stripe`, eventos `checkout.session.completed` e `checkout.session.async_payment_succeeded`), copiar o *signing secret* para `STRIPE_WEBHOOK_SECRET`. |
| `TWILIO_*` | Os pings de WhatsApp/SMS (orçamento enviado, fatura enviada, lembretes de vencimento e de intervenção) ficam só registados em log. Nada mais é afetado — email continua a ser o canal principal. |

**Importante — faturação não é certificada AT.** As faturas geradas por esta aplicação são documentos profissionais reais (numeração sequencial, snapshot imutável, PDF com IBAN) mas **não têm certificação da Autoridade Tributária portuguesa**. Isso exigiria registar o software junto da AT — um processo legal fora do âmbito deste produto. O PDF da fatura e a página de detalhe trazem sempre um aviso visível nesse sentido; não representar o produto a um cliente como "faturação certificada".

### Checklist de arranque para um cliente novo

1. Aprovisionar a instalação (VM/container isolado, base de dados própria).
2. Preencher `docker-compose.prod.env` — no mínimo as 4 variáveis obrigatórias; SMTP antes de qualquer utilização real.
3. `docker compose -f docker-compose.prod.yml --env-file docker-compose.prod.env up -d --build`
4. As migrations aplicam-se automaticamente no arranque da `api`. Confirmar nos logs (`docker compose logs api`) que não há erros de ligação à base de dados.
5. Registar a primeira conta (torna-se automaticamente `Owner`) em `{FRONTEND_URL}/register`.
6. O assistente de configuração inicial (`/onboarding`) aparece automaticamente — nome da empresa, logótipo/cor de marca, primeiro cliente.
7. Se aplicável: configurar Stripe (webhook) e Twilio, testar um envio de orçamento por email e, se configurado, um pagamento em modo de teste Stripe.
8. Entregar credenciais de acesso ao cliente.

## Estrutura do backend (Clean Architecture)

```
src/
  TecnicoApp.Domain/          Entidades, enums, exceções de domínio
  TecnicoApp.Application/     Handlers MediatR, validadores FluentValidation, DTOs
  TecnicoApp.Infrastructure/  EF Core, Hangfire, serviços externos (Stripe, Twilio, SMTP, PDF)
  TecnicoApp.API/             Controllers ASP.NET Core, Program.cs, configuração
tests/
  TecnicoApp.UnitTests/       xUnit + NSubstitute + EF Core InMemory
```

Direção de dependência: Domain ← Application ← Infrastructure ← API.

## Estrutura do frontend

```
tecnico-app/src/
  app/(auth)/        Login, registo, recuperação de password
  app/(dashboard)/   BackOffice autenticado — clientes, orçamentos, faturas, equipa, etc.
  app/(portal)/      Portal do cliente — acesso por magic link, sem conta própria
  app/pay/[token]/   Página pública de pagamento de fatura (sem autenticação)
  app/onboarding/    Assistente de primeira configuração
  components/        Componentes partilhados e específicos de funcionalidade
  hooks/, lib/       React Query hooks, clientes de API, utilitários
```
