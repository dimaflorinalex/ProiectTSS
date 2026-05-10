# Project TSS: Shipping Quote API

## Team members
- Dima Florin-Alexandru - Group 462 - FMI Unibuc
- Copilot (GPT-5.3-Codex)

## Demo
[![Watch the demo](https://img.youtube.com/vi/k5R0SPYNmCI/maxresdefault.jpg)](https://www.youtube.com/watch?v=k5R0SPYNmCI)

## Project scope
This repository contains a .NET 10 shipping quote API and a comprehensive test suite that illustrates the testing strategies required in the course:
- equivalence partitioning
- boundary value analysis
- structural testing (statement, decision, condition)
- independent paths
- mutation testing analysis
- additional tests for killing survived non-equivalent mutants
- randomized and fuzzing-style checks

This README focuses on testing. Detailed mutation analysis and AI testing reports are documented in the separate reports under docs.

---

## Business rules summary
The API calculates shipping costs for a request that contains parcels, zone, pricing model, and optional modifiers.
Key rules enforced by the calculator:
- Input validation for mandatory fields and negative values
- Two pricing models: Brackets and BasePlusPerKg
- Weight rounding rules applied before pricing
- Size-based fees per parcel
- Optional surcharges for fragile and rapid delivery
- Discount handling: free-shipping threshold or coupon discount
- Maximum cap applied after discounts
- Rule traceability via RuleApplied tokens

### Rule tokens produced by the calculator
| Token | Meaning | Trigger |
|---|---|---|
| FALLBACK_ZONE_PRICE | Fallback pricing applied | Zone is null and fallback price is present |
| BRACKETS | Bracket pricing used | PricingModel = Brackets |
| BASE_PLUS_PER_KG | Base plus per kg pricing used | PricingModel = BasePlusPerKg |
| FRAGILE_SURCHARGE | Fragile surcharge applied | Options.Fragil = true |
| RAPID_SURCHARGE | Rapid surcharge applied | Options.Rapid = true |
| FREE_SHIPPING_THRESHOLD | Free shipping applied | Subtotal > FreeShippingThreshold |
| COUPON_DISCOUNT | Coupon applied | Coupon is present and free shipping not applied |
| MAX_CAP | Cap applied | MaxCap is set and net > MaxCap |

---

## Architecture diagrams (Mermaid)

### 1) Component diagram
```mermaid
flowchart LR
    Client[Client / Postman / .http] --> API[ASP.NET Core API]
    API --> Controller[ShippingController]
    Controller --> Service[IShippingCalculatorService]
    Service --> Impl[ShippingCalculatorService]
    Impl --> DTO[Request/Response DTOs]
    Tests[NUnit Test Projects] --> Impl
    Integration[Startup/HTTP Integration Tests] --> API
```

### 2) Core request and response DTOs
```mermaid
classDiagram
    class ShippingQuoteRequest {
        ShippingZone? Zone
        decimal Subtotal
        ShippingOptions Options
        List<ParcelInput> Parcels
        CouponInput? Coupon
        PricingModel? PricingModel
        RoundingRule? RoundingRule
        decimal? FreeShippingThreshold
        decimal? MaxCap
        decimal? FallbackZonePrice
    }

    class ShippingOptions {
        bool Rapid
        bool Fragil
    }

    class ParcelInput {
        decimal WeightKg
        ParcelSize? Size
    }

    class CouponInput {
        CouponType? Type
        decimal Value
    }

    class ShippingQuoteResponse {
        decimal ShippingCost
        string Currency
        ShippingBreakdown Breakdown
        string RuleApplied
    }

    class ShippingBreakdown {
        decimal BaseFee
        decimal PerKgFee
        decimal SizeFee
        decimal FragileSurcharge
        decimal RapidSurcharge
        decimal CouponDiscount
        decimal FreeShippingDiscount
        decimal CapReduction
        decimal SubtotalBeforeDiscounts
        decimal SubtotalAfterDiscounts
    }

    ShippingQuoteRequest --> ShippingOptions
    ShippingQuoteRequest --> ParcelInput
    ShippingQuoteRequest --> CouponInput
    ShippingQuoteResponse --> ShippingBreakdown
```

### 3) Shipping calculation decision flow
```mermaid
flowchart TD
    A[Request] --> B[Validate request]
    B -->|Invalid| E[Throw ArgumentException]
    B -->|Valid| C{Zone is null?}
    C -->|Yes| F[Use fallbackZonePrice]
    C -->|No| D{Pricing model}
    D -->|Brackets| G[Bracket fee by zone + weight]
    D -->|BasePlusPerKg| H[Base + per kg]
    F --> I[Apply size fee]
    G --> I
    H --> I
    I --> J[Fragile surcharge?]
    J --> K[Rapid surcharge?]
    K --> L{Free shipping threshold reached?}
    L -->|Yes| M[Shipping = 0]
    L -->|No| N[Apply coupon]
    M --> O[Apply cap]
    N --> O
    O --> P[Round currency + return response]
```

### 4) Rule trace assembly
```mermaid
flowchart LR
    R0[Start] --> R1[Add pricing model token]
    R1 --> R2{Fragile?}
    R2 -->|Yes| R3[Add FRAGILE_SURCHARGE]
    R2 -->|No| R4[Skip]
    R3 --> R5{Rapid?}
    R4 --> R5
    R5 -->|Yes| R6[Add RAPID_SURCHARGE]
    R5 -->|No| R7[Skip]
    R6 --> R8{Free shipping?}
    R7 --> R8
    R8 -->|Yes| R9[Add FREE_SHIPPING_THRESHOLD]
    R8 -->|No| R10{Coupon?}
    R10 -->|Yes| R11[Add COUPON_DISCOUNT]
    R10 -->|No| R12[Skip]
    R9 --> R13{Cap applied?}
    R11 --> R13
    R12 --> R13
    R13 -->|Yes| R14[Add MAX_CAP]
    R13 -->|No| R15[Finish]
    R14 --> R15
```

### 5) Endpoint sequence
```mermaid
sequenceDiagram
    participant U as User
    participant C as ShippingController
    participant S as ShippingCalculatorService

    U->>C: POST /shipping/quote (JSON)
    C->>S: Calculate(request)
    S->>S: Validate + compute + discounts + cap
    S-->>C: ShippingQuoteResponse
    C-->>U: 200 OK / 400 BadRequest
```

---

## Testing strategy overview

### 1) Strategy map
| Technique | Objective | Target | Evidence (tests) |
|---|---|---|---|
| Equivalence partitioning | Cover representative input classes | Pricing models, zones, request validation | Strategy_BlackBoxTests |
| Boundary value analysis | Guard boundary transitions | Weight brackets, free shipping, cap | Strategy_BlackBoxTests |
| Statement coverage | Execute all statements | ShippingCalculatorService.Calculate | ShippingCalculatorServiceTests |
| Decision coverage | Exercise both branches | Pricing model, rounding, discounts | ShippingCalculatorServiceTests |
| Condition coverage | Toggle atomic conditions | Validation checks | Strategy_WhiteBoxPathTests |
| Independent paths | Cover key end-to-end paths | Fallback, base model, composite modifiers | Strategy_WhiteBoxPathTests |
| Integration tests | Validate API pipeline | Controller and middleware | ShippingControllerTests, ProgramStartupTests |
| Randomized tests | Check invariants under varied data | Valid request generation | Strategy_RandomizedFuzzingTests |
| Mutation testing | Verify assertion strength | ShippingCalculatorService | Stryker report + killer tests |

### 2) Test workflow
```mermaid
flowchart LR
    S[Specification and rules] --> E[Equivalence classes]
    E --> B[Boundary values]
    B --> U[Unit tests]
    U --> C[Coverage review]
    C --> M[Mutation testing]
    M --> R[Add killer tests]
    R --> D[Documentation and traceability]
```

---

## Black-box testing details

### Equivalence classes (examples)
| Input | Partitions | Expected behavior |
|---|---|---|
| Zone | Local, National, International | Valid cost computed |
| PricingModel | Brackets, BasePlusPerKg | Correct pricing path executed |
| Coupon | None, Percent, Fixed | Discount applied when valid |

### Boundary values (examples)
| Boundary | Values | Expected behavior |
|---|---|---|
| Bracket thresholds | 1.0, 1.01, 5.0, 5.01, 10.0, 10.01 | Bracket changes at strict thresholds |
| FreeShippingThreshold | Subtotal == threshold | No free shipping (strict greater-than) |
| MaxCap | Shipping == cap | Cap not applied (strict greater-than) |

---

## White-box testing details

### Structural coverage targets
| Coverage type | Target function | Reason |
|---|---|---|
| Statement | ShippingCalculatorService.Calculate | Main orchestration logic |
| Decision | Validation and pricing branches | Correct routing per input |
| Condition | Validation checks | Correct error behavior |

### Independent paths in Calculate
```mermaid
flowchart TD
    P0[Start] --> P1{Zone null?}
    P1 -->|Yes| P2[Fallback pricing]
    P1 -->|No| P3{Pricing model}
    P3 -->|Brackets| P4[Bracket pricing]
    P3 -->|BasePlusPerKg| P5[Base plus per kg]
    P2 --> P6[Size fee]
    P4 --> P6
    P5 --> P6
    P6 --> P7{Fragile?}
    P7 -->|Yes| P8[Add fragile surcharge]
    P7 -->|No| P9[Skip]
    P8 --> P10{Rapid?}
    P9 --> P10
    P10 -->|Yes| P11[Add rapid surcharge]
    P10 -->|No| P12[Skip]
    P11 --> P13{Free shipping?}
    P12 --> P13
    P13 -->|Yes| P14[Free shipping branch]
    P13 -->|No| P15[Coupon branch]
    P14 --> P16{Cap applied?}
    P15 --> P16
    P16 -->|Yes| P17[Cap applied]
    P16 -->|No| P18[Return]
    P17 --> P18
```

---

## Randomized and fuzzing-style testing
Randomized tests generate valid requests with deterministic seeds and verify invariants:
- ShippingCost is non-negative
- Currency remains RON
- SubtotalAfterDiscounts equals ShippingCost

---

## Mutation testing
Mutation testing is documented in docs/MutationAnalysis.md. The report explains:
- how Stryker mutates the code
- how to interpret survived vs killed mutants
- which two non-equivalent survivors were killed by dedicated tests

---

## AI-assisted testing
AI-assisted testing is documented in docs/AI_Testing_Report.md. The report includes:
- prompts used to generate tests
- analysis of AI output quality
- differences between the AI proposals and the final suite

---

## Environment and configuration
### Hardware and software
- OS: Windows 11 (local machine, no VM)
- .NET SDK: 10.0
- C# language level: 14
- NUnit: 4.3.2
- Microsoft.NET.Test.Sdk: 17.14.0
- coverlet.collector: 6.0.4
- Microsoft.AspNetCore.Mvc.Testing: 10.0.4
- Mutation tool: Stryker.NET (global tool)

---

## Commands
```bash
# Run API
dotnet run --project ProiectTSS/ProiectTSS.csproj

# Run tests
dotnet test ProiectTSS.UnitTests/ProiectTSS.UnitTests.csproj

# Coverage
dotnet test ProiectTSS.UnitTests/ProiectTSS.UnitTests.csproj --collect:"XPlat Code Coverage" --settings coverage.runsettings --results-directory TestResults
reportgenerator "-reports:TestResults/**/coverage.cobertura.xml" "-targetdir:TestResults/CoverageReport" "-reporttypes:Html;TextSummary"

# Mutation testing
cd ProiectTSS.UnitTests
dotnet stryker
```

---

## Tool comparison
| Tool | Purpose | Strengths | Limitations |
|---|---|---|---|
| NUnit | Unit and integration tests | Mature, clear assertions, AAA style | No built-in mutation engine |
| coverlet | Coverage | Native .NET workflow integration | Coverage percent alone does not prove quality |
| Stryker.NET | Mutation testing | Finds weak assertions | Slower than normal unit tests |
| .http and Postman | API checks | Fast manual validation | Not a substitute for automated assertions |

---

## References
[1] C# language documentation, https://learn.microsoft.com/en-us/dotnet/csharp/, Last accessed: 2026-04-04.  
[2] Unit testing C# with NUnit and .NET Core, https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-csharp-with-nunit, Last accessed: 2026-04-04.  
[3] Use .http files in Visual Studio 2022, https://learn.microsoft.com/en-us/aspnet/core/test/http-files, Last accessed: 2026-04-04.  
[4] NUnit Documentation, https://docs.nunit.org/, Last accessed: 2026-04-04.  
[5] Coverlet, https://github.com/coverlet-coverage/coverlet, Last accessed: 2026-04-04.  
[6] Stryker.NET, https://stryker-mutator.io/docs/stryker-net/introduction/, Last accessed: 2026-04-04.  
[7] ASP.NET Core Testing, https://learn.microsoft.com/aspnet/core/test/integration-tests, Last accessed: 2026-04-04.  
[8] GitHub Copilot, https://copilot.microsoft.com, Generation date: 2026-04-04.  
[9] GitHub Copilot (GPT-5.3-Codex), assistance used in repository updates and documentation drafting, Generation date: 2026-04-04.  