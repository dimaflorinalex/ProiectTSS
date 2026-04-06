# AI-Assisted Software Testing Report

## Project Context
- **Technology stack**: `.NET 10`, `C# 14`, `NUnit`, `coverlet`, `Stryker.NET`
- **AI tool used**: **GitHub Copilot (GPT-5.3-Codex)**

---

## 1) Report Objective
This report documents how an AI tool was used during software testing activities and provides a direct comparison between self-authored tests and AI-assisted test proposals.

The report covers:
- the practical role of AI in test design,
- representative prompts and generated outputs,
- differences between the two test suites,
- observed strengths and weaknesses,
- interpretation of the final outcome.

---

## 2) Working Approach

Two parallel test design streams were used.

### A) Self-authored test suite
The self-authored suite was designed directly from business rules and course testing strategies. It includes:
- black-box tests (equivalence classes and boundary values),
- white-box tests (statement/decision/condition-focused paths),
- independent path tests,
- mutation-oriented reinforcement tests.

### B) AI-assisted test proposals
GitHub Copilot was used to generate candidate tests and suggestions for:
- missing validation paths,
- additional assertions,
- branch completion,
- survived mutant targeting.

AI output was treated as draft material and reviewed manually before integration.

---

## 3) Prompt and Output Examples

### Prompt 1 - baseline test generation
> "Generate NUnit tests for a shipping cost service in C#. Include AAA pattern, branch coverage for brackets and base-plus-per-kg models, coupon logic, free-shipping threshold, and input validation exceptions."

#### Output summary
The generated output provided a useful baseline with:
- positive pricing scenarios,
- negative validation scenarios,
- surcharge/discount scenarios.

#### Technical assessment
The output was useful for initial acceleration, but incomplete for:
- mutation-survivor targeting,
- traceability assertions (`RuleApplied`),
- edge-condition precision.

---

### Prompt 2 - mutation reinforcement
> "Given survived mutants in ShippingCalculatorService, suggest two non-equivalent test cases that are likely to kill them, and include precise assertions."

#### Output summary
The generated proposals focused on asserting rule-trace tokens:
- `COUPON_DISCOUNT` in `RuleApplied`,
- `MAX_CAP` in `RuleApplied`.

#### Integrated result
The following tests were added:
- `Calculate_WhenCouponIsApplied_RuleAppliedContainsCouponDiscountToken`
- `Calculate_WhenCapIsApplied_RuleAppliedContainsMaxCapToken`

---

## 4) Comparative Analysis: Team Suite vs AI-Assisted Suite

| Criterion | Self-authored Suite | AI-assisted Suite | Interpretation |
|---|---|---|---|
| Alignment with course requirements | Very strong, explicitly mapped | Good, but generic in places | Team suite is better aligned to rubric-level expectations |
| Business-rule precision | High | Medium | AI misses context-specific intent in some cases |
| Test naming quality | Domain-specific and consistent | Sometimes generic | Manual design is clearer for maintainers |
| Rare branch coverage | Strong after iterative refinement | Variable | AI helps discovery, manual refinement finalizes quality |
| Assertion quality | Strong, includes traceability checks | Good baseline, occasionally shallow | Human review remains necessary |
| Mutation-oriented effectiveness | Strong after targeted additions | Good at suggesting candidates | Final selection requires context-aware judgment |
| Stability under change | Higher | Variable | AI-generated tests may need more adjustment over time |

---

## 5) Key Differences Observed

### Where the self-authored suite is stronger
1. It validates not only numeric output (`ShippingCost`) but also decision traceability via `RuleApplied`.
2. It handles strict boundary semantics (`==` vs `>`) more explicitly.
3. It introduces targeted tests for concrete survived mutants.
4. It mirrors course taxonomy directly in the test structure.

### Where AI assistance was valuable
1. Faster initial test drafting.
2. Quick discovery of missing negative scenarios.
3. Useful branch-completion suggestions.
4. Efficient support for repetitive test scaffolding.

---

## 6) Interpretation
AI assistance improved productivity and reduced initial authoring time. However, final test quality depended on manual review, domain understanding, and mutation-driven refinement.

In this project, AI served as an accelerator, while final correctness and coverage quality were achieved through team-level engineering decisions.

---

## 7) Risks and Limits of AI Use in Testing
### Observed risks
- syntactically correct but semantically weak tests,
- incomplete edge coverage,
- potential false confidence from high test volume,
- limited ability to classify equivalent vs non-equivalent mutants without project context.

### Mitigation applied in this project
Risk mitigation in this project:
- manual review of generated tests,
- mutation testing feedback loop,
- additional business-trace assertions.

---

## 8) Conclusion
The use of GitHub Copilot (GPT-5.3-Codex) was effective and measurable in the testing workflow. The strongest results were obtained when AI-generated proposals were treated as engineering inputs and validated against project-specific requirements, boundary logic, and mutation outcomes.

**Explicit credit**: this project was assisted by **GitHub Copilot (GPT-5.3-Codex)**.