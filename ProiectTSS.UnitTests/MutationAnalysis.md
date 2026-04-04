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

## 4) Examples of non-equivalent survived mutants and killer tests
The following additional tests were introduced to explicitly kill non-equivalent mutants:

### Mutant A
- **Mutation**: `request.Subtotal > request.FreeShippingThreshold` changed to `>=`.
- **Why non-equivalent**: behavior changes at boundary `subtotal == threshold`.
- **Killer test**: `StrategyBlackBoxTests.Calculate_WhenSubtotalEqualsFreeShippingThreshold_DoesNotApplyFreeShipping`.

### Mutant B
- **Mutation**: `net > request.MaxCap` changed to `>=`.
- **Why non-equivalent**: behavior changes at boundary `shipping == cap`.
- **Killer test**: `StrategyBlackBoxTests.Calculate_WhenShippingEqualsCap_DoesNotApplyCapReduction`.

## 5) Mapping to course requirement
- Mutation report analysis is documented here.
- Two extra boundary tests were added specifically to kill two non-equivalent survivors.
