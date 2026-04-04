# Mutation Testing Analysis (Stryker.NET)

This document illustrates the mutation-testing requirement from the course.

## 1) Tool used
- `Stryker.NET` for C# mutation testing.

## 2) How to run
From repository root:

```bash
dotnet tool install -g dotnet-stryker
cd ProiectTSS.UnitTests
dotnet stryker
```

## 3) Typical report interpretation
The report usually contains:
- **Killed mutants**: tests detected behavior change.
- **Survived mutants**: current tests did not detect behavior change.
- **Equivalent mutants**: behavior-preserving transformations (cannot be killed by tests).

## 4) Two non-equivalent survived mutants targeted with extra tests
From `StrykerOutput/2026-04-20.00-44-47/reports/mutation-report.html`, two survived mutants were explicitly targeted:

### Mutant A (id: 52 in `ShippingCalculatorService.cs`)
- **Mutation**: statement removal on `rules.Add(RuleCouponDiscount);`.
- **Why non-equivalent**: behavior changes in traceability output (`RuleApplied`) even when numeric cost is unchanged.
- **Killer test added**: `ShippingCalculatorServiceTests.Calculate_WhenCouponIsApplied_RuleAppliedContainsCouponDiscountToken`.

### Mutant B (id: 67 in `ShippingCalculatorService.cs`)
- **Mutation**: statement removal on `rules.Add(RuleMaxCap);`.
- **Why non-equivalent**: behavior changes in `RuleApplied` output when max-cap branch executes.
- **Killer test added**: `ShippingCalculatorServiceTests.Calculate_WhenCapIsApplied_RuleAppliedContainsMaxCapToken`.

## 5) Mapping to course requirement
- Mutation report analysis is documented here.
- Two extra tests were added specifically to kill two non-equivalent survivors from the report.
