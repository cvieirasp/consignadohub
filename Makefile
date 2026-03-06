# ─────────────────────────────────────────────
# ConsignadoHub – Docker Compose + EF Core
# ─────────────────────────────────────────────

COMPOSE_FILE := infra/local/docker-compose.yml
DC           := docker compose -f $(COMPOSE_FILE)

# ── infrastructure (Docker Compose) ──────────

.PHONY: infra-up
infra-up:
	$(DC) up -d

.PHONY: infra-down
infra-down:
	$(DC) down

.PHONY: infra-down-v
infra-down-v:
	$(DC) down -v

.PHONY: infra-ps
infra-ps:
	$(DC) ps

.PHONY: infra-logs
infra-logs:
	$(DC) logs -f

.PHONY: infra-restart
infra-restart:
	$(DC) restart

# ─────────────────────────────────────────────
# ConsignadoHub – EF Core migration targets
# ─────────────────────────────────────────────

CUSTOMER_INFRA    := src/services/CustomerService/src/CustomerService.Infrastructure
CUSTOMER_API      := src/services/CustomerService/src/CustomerService.Api
CUSTOMER_CONTEXT  := CustomerDbContext

PROPOSAL_INFRA    := src/services/ProposalService/src/ProposalService.Infrastructure
PROPOSAL_API      := src/services/ProposalService/src/ProposalService.Api
PROPOSAL_CONTEXT  := ProposalDbContext

WORKFLOW_INFRA    := src/services/WorkflowWorker/src/WorkflowWorker.Infrastructure
WORKFLOW_STARTUP  := src/services/WorkflowWorker/src/WorkflowWorker.Worker
WORKFLOW_CONTEXT  := WorkflowDbContext

NOTIFICATION_INFRA   := src/services/NotificationService/src/NotificationService.Infrastructure
NOTIFICATION_STARTUP := src/services/NotificationService/src/NotificationService.Worker
NOTIFICATION_CONTEXT := NotificationDbContext

CUSTOMER_UNIT_TESTS      := src/services/CustomerService/tests/CustomerService.UnitTests
PROPOSAL_UNIT_TESTS      := src/services/ProposalService/tests/ProposalService.UnitTests
NOTIFICATION_UNIT_TESTS  := src/services/NotificationService/tests/NotificationService.UnitTests

EF := dotnet ef

# ── helpers ──────────────────────────────────

.PHONY: help
help:
	@echo ""
	@echo "Usage: make <target> [name=<MigrationName>]"
	@echo ""
	@echo "  infra-up                     Start infrastructure containers (SQL Server, RabbitMQ)"
	@echo "  infra-down                   Stop infrastructure containers"
	@echo "  infra-down-v                 Stop infrastructure containers and remove volumes"
	@echo "  infra-ps                     Show status of infrastructure containers"
	@echo "  infra-logs                   Follow logs of all infrastructure containers"
	@echo "  infra-restart                Restart infrastructure containers"
	@echo ""
	@echo "  migrate-all                  Apply pending migrations for all services"
	@echo ""
	@echo "  migrate-customer             Apply pending migrations – CustomerService"
	@echo "  migrate-proposal             Apply pending migrations – ProposalService"
	@echo "  migrate-workflow             Apply pending migrations – WorkflowWorker"
	@echo "  migrate-notification         Apply pending migrations – NotificationService"
	@echo ""
	@echo "  add-migration-customer       Add a migration – CustomerService     (requires name=<Name>)"
	@echo "  add-migration-proposal       Add a migration – ProposalService     (requires name=<Name>)"
	@echo "  add-migration-workflow       Add a migration – WorkflowWorker      (requires name=<Name>)"
	@echo "  add-migration-notification   Add a migration – NotificationService (requires name=<Name>)"
	@echo ""
	@echo "  list-migrations-customer     List migrations – CustomerService"
	@echo "  list-migrations-proposal     List migrations – ProposalService"
	@echo "  list-migrations-workflow     List migrations – WorkflowWorker"
	@echo "  list-migrations-notification List migrations – NotificationService"
	@echo ""
	@echo "  rollback-customer            Revert last migration – CustomerService"
	@echo "  rollback-proposal            Revert last migration – ProposalService"
	@echo "  rollback-workflow            Revert last migration – WorkflowWorker"
	@echo "  rollback-notification        Revert last migration – NotificationService"
	@echo ""
	@echo "  coverage-customer            Run unit tests with coverage and open HTML report – CustomerService"
	@echo "  coverage-proposal            Run unit tests with coverage and open HTML report – ProposalService"
	@echo "  coverage-notification        Run unit tests with coverage and open HTML report – NotificationService"
	@echo "  coverage-all                 Run all unit tests with merged coverage HTML report"
	@echo ""

# ── code coverage ────────────────────────────

COVERAGE_DIR := .coverage

.PHONY: coverage-customer
coverage-customer:
	dotnet test $(CUSTOMER_UNIT_TESTS) \
		--collect:"XPlat Code Coverage" \
		--results-directory $(COVERAGE_DIR)/customer
	reportgenerator \
		-reports:"$(COVERAGE_DIR)/customer/**/coverage.cobertura.xml" \
		-targetdir:"$(COVERAGE_DIR)/customer/html" \
		-reporttypes:Html \
		-assemblyfilters:"-*.UnitTests"
	@echo ""
	@echo "Report: $(COVERAGE_DIR)/customer/html/index.html"

.PHONY: coverage-proposal
coverage-proposal:
	dotnet test $(PROPOSAL_UNIT_TESTS) \
		--collect:"XPlat Code Coverage" \
		--results-directory $(COVERAGE_DIR)/proposal
	reportgenerator \
		-reports:"$(COVERAGE_DIR)/proposal/**/coverage.cobertura.xml" \
		-targetdir:"$(COVERAGE_DIR)/proposal/html" \
		-reporttypes:Html \
		-assemblyfilters:"-*.UnitTests"
	@echo ""
	@echo "Report: $(COVERAGE_DIR)/proposal/html/index.html"

.PHONY: coverage-notification
coverage-notification:
	dotnet test $(NOTIFICATION_UNIT_TESTS) \
		--collect:"XPlat Code Coverage" \
		--results-directory $(COVERAGE_DIR)/notification
	reportgenerator \
		-reports:"$(COVERAGE_DIR)/notification/**/coverage.cobertura.xml" \
		-targetdir:"$(COVERAGE_DIR)/notification/html" \
		-reporttypes:Html \
		-assemblyfilters:"-*.UnitTests"
	@echo ""
	@echo "Report: $(COVERAGE_DIR)/notification/html/index.html"

.PHONY: coverage-all
coverage-all:
	dotnet test $(CUSTOMER_UNIT_TESTS) \
		--collect:"XPlat Code Coverage" \
		--results-directory $(COVERAGE_DIR)/customer
	dotnet test $(PROPOSAL_UNIT_TESTS) \
		--collect:"XPlat Code Coverage" \
		--results-directory $(COVERAGE_DIR)/proposal
	dotnet test $(NOTIFICATION_UNIT_TESTS) \
		--collect:"XPlat Code Coverage" \
		--results-directory $(COVERAGE_DIR)/notification
	reportgenerator \
		-reports:"$(COVERAGE_DIR)/**/coverage.cobertura.xml" \
		-targetdir:"$(COVERAGE_DIR)/html" \
		-reporttypes:Html \
		-assemblyfilters:"-*.UnitTests"
	@echo ""
	@echo "Report: $(COVERAGE_DIR)/html/index.html"

# ── apply migrations ─────────────────────────

.PHONY: migrate-all
migrate-all: migrate-customer migrate-proposal migrate-workflow migrate-notification

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

.PHONY: migrate-workflow
migrate-workflow:
	$(EF) database update \
		--project $(WORKFLOW_INFRA) \
		--startup-project $(WORKFLOW_STARTUP) \
		--context $(WORKFLOW_CONTEXT)

.PHONY: migrate-notification
migrate-notification:
	$(EF) database update \
		--project $(NOTIFICATION_INFRA) \
		--startup-project $(NOTIFICATION_STARTUP) \
		--context $(NOTIFICATION_CONTEXT)

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

.PHONY: add-migration-workflow
add-migration-workflow:
ifndef name
	$(error name is required. Usage: make add-migration-workflow name=<MigrationName>)
endif
	$(EF) migrations add $(name) \
		--project $(WORKFLOW_INFRA) \
		--startup-project $(WORKFLOW_STARTUP) \
		--context $(WORKFLOW_CONTEXT) \
		--output-dir Persistence/Migrations

.PHONY: add-migration-notification
add-migration-notification:
ifndef name
	$(error name is required. Usage: make add-migration-notification name=<MigrationName>)
endif
	$(EF) migrations add $(name) \
		--project $(NOTIFICATION_INFRA) \
		--startup-project $(NOTIFICATION_STARTUP) \
		--context $(NOTIFICATION_CONTEXT) \
		--output-dir Migrations

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

.PHONY: list-migrations-workflow
list-migrations-workflow:
	$(EF) migrations list \
		--project $(WORKFLOW_INFRA) \
		--startup-project $(WORKFLOW_STARTUP) \
		--context $(WORKFLOW_CONTEXT)

.PHONY: list-migrations-notification
list-migrations-notification:
	$(EF) migrations list \
		--project $(NOTIFICATION_INFRA) \
		--startup-project $(NOTIFICATION_STARTUP) \
		--context $(NOTIFICATION_CONTEXT)

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

.PHONY: rollback-workflow
rollback-workflow:
	$(EF) migrations remove \
		--project $(WORKFLOW_INFRA) \
		--startup-project $(WORKFLOW_STARTUP) \
		--context $(WORKFLOW_CONTEXT)

.PHONY: rollback-notification
rollback-notification:
	$(EF) migrations remove \
		--project $(NOTIFICATION_INFRA) \
		--startup-project $(NOTIFICATION_STARTUP) \
		--context $(NOTIFICATION_CONTEXT)
