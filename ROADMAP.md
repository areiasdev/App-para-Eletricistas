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
- [x] Quadro de **IVA por taxa** no PDF da fatura (art. 36.º CIVA).
- [ ] **Autoliquidação** (art. 2.º n.º 1 j) CIVA) e **motivo de isenção** por linha; taxa 6% (verba 2.27) para reabilitação.
- [ ] **Retenção na fonte** (IRS categoria B).
- [ ] **Nota de crédito** em vez de "cancelar"; **recibos**; pagamentos parciais.
- [x] Morada do cliente nos documentos.
- [ ] Morada da obra (distinta da de faturação); alvará / registo DGEG da empresa no cabeçalho.

## P1 — Trabalho de campo (eletricistas / manutenção)

- [x] Fotos pela câmara do telemóvel (upload real, comprimidas no browser).
- [x] Folha de obra / relatório de intervenção em PDF com **assinatura do cliente** no fim.
- [ ] Registo de **ensaios elétricos** (isolamento, terra, diferenciais, continuidade) + Termo de Responsabilidade / Ficha Eletrotécnica.
- [x] **Faturar intervenção sem orçamento** (horas × preço/hora + materiais ao preço de venda).
- [x] Horas de trabalho por intervenção e preço/hora da empresa.
- [ ] Cronómetro início/fim por técnico, deslocação; corrigir dupla contagem de receita no relatório de rentabilidade.
- [x] Unidade de medida nas linhas (un, m, m², h, vg…) e preços com 4 casas decimais.
- [ ] **Catálogo** de materiais/serviços com custo, preço e margem.
- [x] Agenda semanal filtrável por técnico.
- [ ] Arrastar para reagendar / atribuir.
- [x] Periodicidade de manutenção por equipamento (a próxima data avança ao concluir a intervenção).
- [ ] Contratos de manutenção (avenças) com faturação recorrente.
- [x] PWA instalável (manifest, ícones, atalhos).
- [ ] Modo offline (service worker + fila de sincronização).

## Cliente e cobrança

- [x] **Aprovação online do orçamento**: link no email, o cliente aceita com assinatura ou recusa com motivo; a equipa é notificada.
- [x] Lembrete à equipa de orçamentos sem resposta há 7 dias.
- [x] Faturas passam a "Vencida" automaticamente; "A receber" e "Vencidas" no dashboard.
- [x] Exportação CSV de faturas para o contabilista.

## P2 — Empresas de construção (obras)

- [ ] Entidade **Obra/Projeto** (morada própria, orçamentos, intervenções, faturas, custos).
- [x] Duplicar orçamento (revisão / trabalho semelhante).
- [ ] Orçamento por **capítulos** / mapa de quantidades, itens opcionais.
- [ ] **Autos de medição** (faturação por % de execução), **retenção de garantia**, adiantamentos.
- [ ] Trabalhos a mais / a menos ligados ao orçamento original.
- [ ] Fornecedores, encomendas, subempreiteiros; orçamentado vs. real por obra.

## Técnico

- [x] Coluna `Position` nas linhas de orçamento/fatura (ordem estável).
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
