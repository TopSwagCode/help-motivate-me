# Daily Identity Commitment
## Core Addictive Feature Specification

This document describes the **Daily Identity Commitment** feature for *Help Motivate Me*.
It is designed to drive **daily return**, **habit follow-through**, and **identity alignment** without shame or pressure.

This feature intentionally replaces generic “daily login rewards” with a **meaningful daily ritual**.

---

## Purpose

The Daily Identity Commitment exists to answer one question every day:

> **“Who am I choosing to be today?”**

It creates:
- A daily reason to open the app
- Psychological ownership of identity
- A micro-commitment that increases follow-through
- A ritual users don’t want to skip

---

## High-Level Concept

Once per day, the user:
1. Chooses an identity to focus on
2. Commits to one tiny action that proves it
3. Receives identity-based reinforcement

The entire flow takes **30–60 seconds**.

---

## Why This Works (Psychology)

This feature leverages:
- Identity-based motivation (Atomic Habits)
- Cognitive consistency (“I said I would”)
- Ritual formation (stronger than reminders)
- Internal rewards instead of external bribes

Users return daily not for points — but to **align with who they want to be**.

---

## Trigger Conditions

The Daily Identity Commitment prompt appears when:
- The user opens the app for the first time that day  
OR
- A scheduled morning notification is tapped

### Do NOT show when:
- User already completed it today
- User is mid-task or flow state
- User explicitly dismissed it earlier that day

---

## Flow Specification

### Step 1: Identity Selection

**Prompt copy:**
> **“Today, I choose to show up as…”**

User selects:
- One identity (recommended)
- Maximum two identities (optional, advanced users)

**Smart defaults:**
- Preselect weakest identity (recovery nudge)
- OR strongest identity (momentum nudge)
- User can always override

---

### Step 2: Tiny Action Commitment

**Prompt copy:**
> **“What’s the smallest action that proves this today?”**

Examples:
- Write one sentence
- Put on workout shoes
- Drink one glass of water
- Review goals for 2 minutes

**Rules:**
- Action must be tiny
- One action only
- Auto-suggest from existing habits/tasks
- Editable by user

---

### Step 3: Commitment Lock-In

**Button copy:**
> **“I commit to this.”**

This step is critical.
It creates psychological ownership and intent.

No countdowns.
No pressure.
Just clarity.

---

### Step 4: Immediate Identity Reinforcement

After committing, show feedback such as:
- “This is how a **Writer** starts their day.”
- “One action is enough to reinforce identity.”
- “Showing up matters more than intensity.”

This reinforces intrinsic motivation immediately.

---

## Completion Handling

### When the committed action is completed:
- Grant **+1 bonus Identity Vote**
- Show explicit message:

> “You honored today’s identity commitment.”

This ties commitment directly to the Identity Score.

---

### If the commitment is not completed:
- No punishment
- No score penalty
- No guilt messaging

Next-day copy example:
> “Yesterday didn’t go as planned.  
> Today is a new chance to show up.”

Trust is preserved.

---

## Identity Presence Tracking (Optional Layer)

Track:
> **Days the user consciously chose an identity**

This is NOT a streak.
It does not reset.

Example labels:
- 3 days → Identity Awareness
- 7 days → Intentional
- 21 days → Self-Aligned
- 60 days → Identity-Driven

This reinforces long-term identity awareness without pressure.

---

## Notification Copy Examples

### Morning
> “Who are you choosing to be today?”

### Midday
> “One small action still proves your **Athlete** identity.”

### Evening
> “Did you show up as who you wanted to be today?”

Notifications are cues — not commands.

---

## UX Principles

- Fast (under 60 seconds)
- Calm and intentional
- Never guilt-based
- Always optional
- Always recoverable

If users skip it, the app remains welcoming.

---

## Relationship to Other Systems

- Feeds directly into **Identity Score**
- Enhances habit completion
- Improves daily active usage
- Replaces generic login rewards
- Strengthens emotional attachment to the app

---

## Success Metrics

Track:
- Daily commitment completion rate
- Commitment → habit completion lift
- DAU increase after feature launch
- Identity Score stability over time
- Retention at Day 7 / Day 30

---

## Design Philosophy

This feature exists to build **self-trust**, not pressure.

Users should feel:
> “I showed up for myself today.”

That feeling is the addiction — and it’s healthy.

