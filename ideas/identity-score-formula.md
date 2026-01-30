TODO 
- Update to also include Daily commitment
- Also be easier to gain. Doing nothing should slowly decrease, just 1 thing slowly increase.
- Identity progress seems broken day 1. 1 is 33, 3 others is 0. Progress should not be on day 1? but first day 2?

# Identity Score Formula (Production-Ready)

This document describes the **Identity Score** system used in *Help Motivate Me*.
The goal is to measure **identity strength and momentum**, not perfection.

The system is designed to:
- Work well for **new users**
- Scale naturally for **long-term users**
- Be **forgiving**, anti-shame, and motivating
- Encourage daily return and recovery

---

## Core Concept

Each identity has a score from **0–100** representing how strongly that identity is being reinforced **right now**.

The score is based on:
- Recent actions (recency-weighted)
- Consistency over time
- Gentle decay when inactive
- Special protection for new users

---

## 1. Identity Votes

Every completed action linked to an identity grants **Identity Votes**.

| Action Type | Votes |
|------------|-------|
| Small habit | +1 |
| Regular task | +2 |
| Journal entry linked to identity | +1 |
| Completing full habit stack | +2 bonus |

**Daily cap:**  
Max **8–10 votes per identity per day** to prevent gaming.

---

## 2. Recency Weighting

Votes are weighted based on how recent they are.

| Day (relative to today) | Weight |
|------------------------|--------|
| Today | 1.0 |
| Yesterday | 0.9 |
| 2 days ago | 0.8 |
| 3 days ago | 0.7 |
| ... | ... |
| 14 days ago | 0.1 |
| >14 days ago | 0 |

---

## 3. Raw Identity Momentum

```
RawScore = Σ (Votes × RecencyWeight)
```

This represents short-term identity momentum.

---

## 4. Dynamic Time Window (New User Support)

To support new and growing accounts, the evaluation window grows with account age.

```
AccountAgeDays = days since first identity action
EffectiveWindow = min(14, AccountAgeDays + 3)
```

Examples:

| Account Age | Window |
|------------|--------|
| Day 1 | 4 days |
| Day 3 | 6 days |
| Day 7 | 10 days |
| Day 11+ | 14 days |

---

## 5. Normalization

```
MaxPossible = MaxDailyVotes × EffectiveWindow
NormalizedScore = (RawScore / MaxPossible) × 100
```

Clamp result to **0–100**.

---

## 6. Gentle Identity Decay

If **no actions are completed today** for an identity:

```
Score = Score × 0.97
```

This creates:
- Noticeable decline after several days
- Fast recovery after returning
- No harsh punishment for missing a day

---

## 7. Beginner Confidence Floor (First 14 Days)

To prevent early discouragement, apply a temporary score floor.

### Conditions
- AccountAgeDays < 14
- At least one identity action in the last 48 hours

### Floor Formula
```
Floor = 30 + (AccountAgeDays × 2)
```

### Apply
```
FinalScore = max(Score, Floor)
```

This floor fades out naturally after day 14.

---

## 8. Final Identity Score Formula (Summary)

```
IdentityScore =
max(
  Normalize(
    Σ (Votes × RecencyWeight)
  ) × DecayFactor,
  BeginnerFloor
)
```

Where:
- `DecayFactor = 0.97ⁿ` (n = consecutive inactive days)
- `BeginnerFloor` applies only in first 14 days
- Score range: **0–100**

---

## 9. Identity Status Labels (UX Layer)

Never show only a number — always show meaning.

| Score | Status |
|------|-------|
| 0–24 | Dormant |
| 25–39 | Forming  |
| 40–59 | Emerging |
| 60–74 | Stabilizing |
| 75–89 | Strong |
| 90–100 | Automatic |

---

## 10. UX Recommendation (Critical)

For the **first 7 days**:
- Hide the numeric score
- Show only:
  - Status label
  - Direction indicator (↑ ↓ →)
  - Identity proof messages

After day 7:
> “Your identity is now measurable.”

---

## Philosophy

This system measures **who the user is becoming**, not how perfect they are.

Every action is a vote.
Momentum matters more than streaks.
Recovery is always possible.

This is intentional.
