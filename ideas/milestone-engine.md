
# Milestone / Achievement Engine (Progress-Only)

## Core Idea

The milestone engine is **event-driven and additive**.

User actions emit events.  
Events update user statistics.  
Milestones evaluate against those stats and recent events.  
Awards are granted once and never revoked.

Time gaps never invalidate progress.

---

## Events: Source of Truth

Every meaningful user action emits a domain event.

Examples:
- UserLoggedIn
- WinLogged
- HabitCompleted

Each event contains:
- user_id
- event_type
- occurred_at
- optional metadata

Events are append-only and immutable.

They exist to:
1. Support window-based rules (e.g. last N days)
2. Allow retroactive milestone evaluation in the future

Stats are derived from events, never the other way around.

---

## User Stats: Fast Aggregates

To avoid expensive queries, the engine maintains a single stats row per user.

Typical fields:
- login_count
- total_wins
- total_habits_completed
- last_login_at
- last_activity_at

Stats:
- Only increase (no streaks, no resets)
- Are updated in the same transaction as the event insert
- Represent lifetime progress

---

## Milestone Definitions

Milestones are data, not code.

Each milestone defines:
- code (stable identifier)
- title
- description
- icon
- trigger_event
- rule_type
- rule_data (JSON)
- is_active

Only milestones whose trigger_event matches the current event are evaluated.

---

## Supported Rule Types

The engine intentionally supports a small set of progress-friendly rules.

### Count Rule

Lifetime accumulation.

Rule data:
{
  "field": "total_wins",
  "threshold": 100
}

Evaluation:
- Read user_stats[field]
- Achieved if value >= threshold

---

### Window Count Rule

Momentum without streaks.

Rule data:
{
  "event": "HabitCompleted",
  "count": 5,
  "window_days": 14
}

Evaluation:
- Count matching events in the last N days
- Achieved if count >= required

---

### Return After Gap Rule

Celebrates coming back.

Rule data:
{
  "gap_days": 14
}

Evaluation:
- Compare current login time with previous last_login_at
- Achieved if gap >= required days

Only evaluated on login events.

---

## Rule Evaluation Flow

When an action occurs:

1. Insert domain event
2. Update user stats
3. Load active milestones matching trigger_event
4. Evaluate each rule
5. If achieved:
   - Insert (user_id, milestone_id) into user_milestones
6. Collect newly awarded milestones

The process is:
- Deterministic
- Idempotent
- Safe to retry

---

## Awarding Milestones

Award records are stored in user_milestones.

Properties:
- Awarded once
- Never revoked
- Enforced by database uniqueness constraint

The database, not application logic, prevents duplicate awards.

---

## Catch-Up Evaluation

On login, the engine also evaluates all login-triggered milestones.

This ensures:
- Returning users are rewarded
- Newly added milestones can be earned retroactively

Catch-up uses the same evaluation logic as real-time actions.

---

## Engine Output

The engine returns:
- Zero or more newly awarded milestones

Each milestone includes:
- title
- description
- icon

The engine does not handle:
- UI
- Animations
- Notifications

It only reports facts.

---

## Design Principles

- No streaks
- No punishment
- No reset states
- Only accumulated proof of progress

Milestones act as receipts:

"At some point, you did this."

The engine exists to witness and acknowledge progress.

## Custom animations.

Each milestone should have option for custom animation on frontend, 
Make it simple for now with options to extend in future. Eg. show different Webm, confetti, etc.
For now just use same confetti logic we have as when user is first onboarded.

## Milestones translations

Milestones should support translation keys and be simple to add / update.


## Admin tab

I want an admin tab, where I can trigger view of milestones. It shouldn't complete a milestone. Just a place for me to debug and see how the milestone completion looks for a user.