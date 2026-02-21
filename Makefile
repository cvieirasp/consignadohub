# ─────────────────────────────────────────────
# ConsignadoHub – EF Core migration targets
# ─────────────────────────────────────────────

CUSTOMER_INFRA    := src/services/CustomerService/src/CustomerService.Infrastructure
CUSTOMER_API      := src/services/CustomerService/src/CustomerService.Api
CUSTOMER_CONTEXT  := CustomerDbContext

PROPOSAL_INFRA    := src/services/ProposalService/src/ProposalService.Infrastructure
PROPOSAL_API      := src/services/ProposalService/src/ProposalService.Api
PROPOSAL_CONTEXT  := ProposalDbContext

EF := dotnet ef

# ── helpers ──────────────────────────────────

.PHONY: help
help:
	@echo ""
	@echo "Usage: make <target> [name=<MigrationName>]"
	@echo ""
	@echo "  migrate-all                  Apply pending migrations for all services"
	@echo ""
	@echo "  migrate-customer             Apply pending migrations – CustomerService"
	@echo "  migrate-proposal             Apply pending migrations – ProposalService"
	@echo ""
	@echo "  add-migration-customer       Add a migration – CustomerService  (requires name=<Name>)"
	@echo "  add-migration-proposal       Add a migration – ProposalService  (requires name=<Name>)"
	@echo ""
	@echo "  list-migrations-customer     List migrations – CustomerService"
	@echo "  list-migrations-proposal     List migrations – ProposalService"
	@echo ""
	@echo "  rollback-customer            Revert last migration – CustomerService"
	@echo "  rollback-proposal            Revert last migration – ProposalService"
	@echo ""

# ── apply migrations ─────────────────────────

.PHONY: migrate-all
migrate-all: migrate-customer migrate-proposal

.PHONY: migrate-customer
migrate-customer:
	$(EF) database update \
		--project $(CUSTOMER_INFRA) \
		--startup-project $(CUSTOMER_API) \
		--context $(CUSTOMER_CONTEXT)

.PHONY: migrate-proposal
migrate-proposal:
	$(EF) database update \
		--project $(PROPOSAL_INFRA) \
		--startup-project $(PROPOSAL_API) \
		--context $(PROPOSAL_CONTEXT)

# ── add migrations ───────────────────────────

.PHONY: add-migration-customer
add-migration-customer:
ifndef name
	$(error name is required. Usage: make add-migration-customer name=<MigrationName>)
endif
	$(EF) migrations add $(name) \
		--project $(CUSTOMER_INFRA) \
		--startup-project $(CUSTOMER_API) \
		--context $(CUSTOMER_CONTEXT) \
		--output-dir Persistence/Migrations

.PHONY: add-migration-proposal
add-migration-proposal:
ifndef name
	$(error name is required. Usage: make add-migration-proposal name=<MigrationName>)
endif
	$(EF) migrations add $(name) \
		--project $(PROPOSAL_INFRA) \
		--startup-project $(PROPOSAL_API) \
		--context $(PROPOSAL_CONTEXT) \
		--output-dir Persistence/Migrations

# ── list migrations ──────────────────────────

.PHONY: list-migrations-customer
list-migrations-customer:
	$(EF) migrations list \
		--project $(CUSTOMER_INFRA) \
		--startup-project $(CUSTOMER_API) \
		--context $(CUSTOMER_CONTEXT)

.PHONY: list-migrations-proposal
list-migrations-proposal:
	$(EF) migrations list \
		--project $(PROPOSAL_INFRA) \
		--startup-project $(PROPOSAL_API) \
		--context $(PROPOSAL_CONTEXT)

# ── rollback (remove last migration) ─────────

.PHONY: rollback-customer
rollback-customer:
	$(EF) migrations remove \
		--project $(CUSTOMER_INFRA) \
		--startup-project $(CUSTOMER_API) \
		--context $(CUSTOMER_CONTEXT)

.PHONY: rollback-proposal
rollback-proposal:
	$(EF) migrations remove \
		--project $(PROPOSAL_INFRA) \
		--startup-project $(PROPOSAL_API) \
		--context $(PROPOSAL_CONTEXT)
