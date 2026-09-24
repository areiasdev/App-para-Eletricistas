# TécnicoApp — Roadmap

Backlog priorizado a partir da revisão completa de setembro de 2026 — ver
[`docs/REVIEW-2026-09.md`](docs/REVIEW-2026-09.md) para o detalhe e a justificação de cada ponto.

## Estado atual

Em produção por instalação (uma empresa = uma instalação): clientes, equipamentos, intervenções,
orçamentos (PDF, email, assinatura), faturação a partir de orçamento aceite (PDF **não certificado
AT**), pagamento online Stripe (cartão + MB WAY), portal do cliente por magic link, equipa com
papéis (Owner/Admin/Technician/Commercial), auditoria, lembretes por email/WhatsApp, onboarding.

## P0 — Bloqueia uso real para faturação em Portugal

- [ ] Decidir e aplicar **desconto antes do IVA** (hoje o desconto é abatido depois do IVA → IVA sobreavaliado). Requer regra versionada para não alterar faturas já emitidas.
- [ ] **Integração com software de faturação certificado** (Moloni / InvoiceXpress / Vendus / …) para emitir o documento fiscal (ATCUD, QR, SAF-T) a partir do "Faturar".
- [ ] Quadro de **IVA por taxa** no PDF da fatura (art. 36.º CIVA).
- [ ] **Autoliquidação** (art. 2.º n.º 1 j) CIVA) e **motivo de isenção** por linha; taxa 6% (verba 2.27) para reabilitação.
- [ ] **Retenção na fonte** (IRS categoria B).
- [ ] **Nota de crédito** em vez de "cancelar"; **recibos**; pagamentos parciais.
- [ ] Morada do cliente / da obra nos documentos; alvará / registo DGEG da empresa no cabeçalho.

## P1 — Trabalho de campo (eletricistas / manutenção)

- [ ] Fotos pela câmara do telemóvel (upload real em vez de URL).
- [ ] Folha de obra / relatório de intervenção em PDF com **assinatura do cliente** no fim.
- [ ] Registo de **ensaios elétricos** (isolamento, terra, diferenciais, continuidade) + Termo de Responsabilidade / Ficha Eletrotécnica.
- [ ] **Faturar intervenção sem orçamento** (horas + materiais usados).
- [ ] Registo de **horas** por técnico, preço/hora, deslocação; corrigir dupla contagem de receita no relatório de rentabilidade.
- [ ] **Catálogo** de materiais/serviços com custo, preço, unidade (un, m, m², h) e margem.
- [ ] Agenda / despacho por técnico.
- [ ] **Contratos de manutenção** com periodicidade (avançar `NextMaintenance` automaticamente) e faturação recorrente.
- [ ] PWA com modo offline.

## P2 — Empresas de construção (obras)

- [ ] Entidade **Obra/Projeto** (morada própria, orçamentos, intervenções, faturas, custos).
- [ ] Orçamento por **capítulos** / mapa de quantidades, itens opcionais, **revisões**.
- [ ] **Autos de medição** (faturação por % de execução), **retenção de garantia**, adiantamentos.
- [ ] Trabalhos a mais / a menos ligados ao orçamento original.
- [ ] Fornecedores, encomendas, subempreiteiros; orçamentado vs. real por obra.

## Técnico

- [ ] Coluna `Position` nas linhas de orçamento/fatura (ordem estável).
- [ ] Atualizar EF Core / JwtBearer / Serilog para 10.x e fixar versões (Central Package Management).
- [ ] CI (build + testes + lint) em cada PR.
- [ ] Backups automáticos do Postgres por instalação.
- [ ] Autorização por papel/tenant centralizada (behaviour MediatR ou policies + query filter por tenant).
- [ ] Audience/esquema JWT próprio para o portal do cliente.
- [ ] Mover lógica do `ClientPortalController` para handlers MediatR.
- [ ] `timestamptz` + UTC em todo o lado (remover `EnableLegacyTimestampBehavior`).

## Notas técnicas

- **Build:** `dotnet build TecnicoApp.slnx`
- **Migrations:** `dotnet ef migrations add <Nome> --project src/TecnicoApp.Infrastructure --startup-project src/TecnicoApp.API` (aplicadas automaticamente no arranque da API)
- **Frontend env:** `NEXT_PUBLIC_API_URL` (padrão `http://localhost:5092`), `NEXT_PUBLIC_APP_NAME`
- **Contas:** só a primeira conta se regista livremente (fica Owner); as restantes entram por convite (`App:AllowOpenRegistration`).
