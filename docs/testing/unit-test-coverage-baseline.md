# Unit test coverage baseline

## Scope

Coverage is collected for the two automated test projects in `Misha.slnx`:

- `tests/Misha.Domain.Tests`
- `tests/Misha.Integration.Tests`

Both test projects already use the Coverlet collector. The CI workflow now collects Cobertura coverage for every test run and publishes the reports as workflow artifacts.

## Baseline policy

The first CI-generated report is the authoritative baseline for the repository. A numeric coverage gate is intentionally not guessed before the first measured report is available.

After the first successful coverage run, the measured line/branch coverage will be recorded here and used to establish a non-regression threshold in CI.

## CI evidence

Coverage is generated with `dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults` and published from the CI run. The coverage artifact is named `unit-test-coverage`.

This baseline is an engineering guardrail: coverage must not be treated as proof of functional correctness or production readiness.
