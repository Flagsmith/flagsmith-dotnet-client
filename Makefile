SDK_BENCHMARKS := Flagsmith.EngineTest/SdkBenchmarks
INSTRUMENT_HOOKS := Flagsmith.Benchmarks/CodSpeed/instrument-hooks
BENCHMARKS_BUILD := Flagsmith.Benchmarks/bin/Release/net10.0

INSTRUMENT_HOOKS_CFLAGS := -std=gnu11 -O2 -fPIC -Wno-maybe-uninitialized -Wno-unused-variable -Wno-unused-parameter -Wno-unused-but-set-variable -Wno-type-limits -Wno-format -Wno-format-security
ifeq ($(shell uname -s),Darwin)
INSTRUMENT_HOOKS_LIB := $(BENCHMARKS_BUILD)/libinstrument_hooks.dylib
INSTRUMENT_HOOKS_LDFLAGS := -dynamiclib
else
INSTRUMENT_HOOKS_LIB := $(BENCHMARKS_BUILD)/libinstrument_hooks.so
INSTRUMENT_HOOKS_LDFLAGS := -shared
endif

.PHONY: install
install: ## Install development dependencies
	npm install -g quicktype

$(SDK_BENCHMARKS)/environments:
	git submodule update --init -- $(SDK_BENCHMARKS)

$(INSTRUMENT_HOOKS)/dist/core.c:
	git submodule update --init -- $(INSTRUMENT_HOOKS)

$(INSTRUMENT_HOOKS_LIB): | $(INSTRUMENT_HOOKS)/dist/core.c
	mkdir -p $(@D)
	$(CC) $(INSTRUMENT_HOOKS_CFLAGS) -I$(INSTRUMENT_HOOKS)/includes $(INSTRUMENT_HOOKS_LDFLAGS) -o $@ $(INSTRUMENT_HOOKS)/dist/core.c

.PHONY: benchmark benchmark-build benchmark-run
benchmark: benchmark-build benchmark-run ## Run the SDK benchmarks

# Kept apart from benchmark-run: CodSpeed's simulation mode runs its whole command under Valgrind
benchmark-build: | $(SDK_BENCHMARKS)/environments
	dotnet build Flagsmith.Benchmarks --configuration Release
	$(MAKE) $(INSTRUMENT_HOOKS_LIB)

benchmark-run: | $(SDK_BENCHMARKS)/environments
	SDK_BENCHMARKS_ENVIRONMENTS_DIR=$(abspath $(SDK_BENCHMARKS)/environments) dotnet $(BENCHMARKS_BUILD)/Flagsmith.Benchmarks.dll

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
