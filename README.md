# Sabemi TEC: serviço de webhooks de pagamento

Serviço que recebe notificações de pagamento (webhooks) de um banco parceiro, garante
idempotência por transação, processa a regra de negócio em background e expõe um painel
administrativo com os eventos em tempo real.

Projeto em construção. A documentação completa (arquitetura, contrato do webhook, decisões
técnicas e instruções de execução) será consolidada aqui ao final.

## Stack

| Camada | Tecnologia |
| --- | --- |
| Backend | .NET 10, ASP.NET Core Minimal APIs, EF Core, SignalR |
| Banco | PostgreSQL 17 |
| Frontend | Next.js (App Router), TypeScript, Tailwind CSS |
| Testes | xUnit, WebApplicationFactory, Testcontainers |
| Execução | Docker Compose |

## Estrutura

```
backend/    solução .NET (API, domínio e infraestrutura) e testes
frontend/   painel administrativo em Next.js
docs/       coleção de requisições HTTP
scripts/    utilitários para assinar e disparar webhooks
```

## Rodando o banco localmente

```bash
cp .env.example .env
docker compose up -d db
```
