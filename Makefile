.PHONY: install 
install: ## Install development dependencies
	npm install -g quicktype

.PHONY: clone-engine-test-data
clone-engine-test-data: ## (temporary) Clone the engine test data submodule
	git clone https://github.com/Flagsmith/engine-test-data.git Flagsmith.EngineTest/EngineTestDataV2

.PHONY: generate-engine-classes 
generate-engine-classes: ## Generate engine classes from the JSON Schema specification
	quicktype \
		--lang csharp \
		--src-lang schema \
		--namespace FlagsmithEngine \
		--framework NewtonSoft \
		--features attributes-only \
		--check-required \
		--out Flagsmith.Engine/EvaluationContext/EvaluationContext.cs \
		https://raw.githubusercontent.com/Flagsmith/flagsmith/refs/heads/main/sdk/evaluation-context.json
	quicktype \
		--lang csharp \
		--src-lang schema \
		--namespace FlagsmithEngine \
		--framework NewtonSoft \
		--features attributes-only \
		--check-required \
		--out Flagsmith.Engine/EvaluationResult/EvaluationResult.cs \
		https://raw.githubusercontent.com/Flagsmith/flagsmith/refs/heads/main/sdk/evaluation-result.json

help:
	@echo "Usage: make [target]"
	@echo ""
	@echo "Available targets:"
	@awk 'BEGIN {FS = ":.*?## "} /^[a-zA-Z_-]+:.*?## / {printf "  \033[36m%-30s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)
