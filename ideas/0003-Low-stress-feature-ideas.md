# Low-Stress Feature Ideas

## Product Direction

Help Motivate Me should help a person collect evidence of who they are becoming without turning self-improvement into another source of judgment. Its strongest differentiators are identity votes, habit stacks, daily commitments, identity-driven goals, and private reflection. New features should deepen that loop:

> Choose who matters today, make the action small enough for real life, record the vote, learn without judgment, and begin again.

The app already supports identities, weighted identity proofs, habit and task completions, daily commitments, habit schedules, journaling, analytics, and milestones. The ideas below extend those capabilities rather than replace them.

## Rating Scale

- **Effort:** 1 = small change, 5 = major cross-stack investment.
- **Value:** 1 = limited user benefit, 5 = potentially transformative.
- **Fit:** 1 = weak connection, 5 = central to the app's identity-first, low-pressure philosophy.
- Priority favors high value and fit. Effort is a cost, not a quality score.

## Ranked Ideas

| Rank | Feature | Effort | Value | Fit | Recommended phase |
| ---: | --- | :---: | :---: | :---: | --- |
| 1 | Minimum Viable Votes | 3/5 | 5/5 | 5/5 | Now |
| 2 | Begin Again Recovery | 2/5 | 5/5 | 5/5 | Now |
| 3 | Capacity-Aware Today | 3/5 | 5/5 | 5/5 | Now |
| 4 | Flexible Rhythms | 4/5 | 5/5 | 5/5 | Next |
| 5 | Compassionate Weekly Review | 3/5 | 5/5 | 5/5 | Next |
| 6 | Goal Seasons and Maintenance Mode | 3/5 | 4/5 | 5/5 | Next |
| 7 | Identity Evidence Story | 3/5 | 4/5 | 5/5 | Next |
| 8 | Friction Plans | 2/5 | 4/5 | 5/5 | Now |
| 9 | Two-Week Experiments | 4/5 | 4/5 | 4/5 | Later |
| 10 | Gentle Insights | 4/5 | 4/5 | 4/5 | Later |

## 1. Minimum Viable Votes

Let every habit and goal task have an optional **tiny version**: read one paragraph, put on running shoes, open the document, or tidy one item. On a difficult day, the user can complete that version and cast a valid identity vote without pretending the full action happened.

**Why it matters:** The hardest part of consistency is often starting. A tiny action preserves identity continuity and makes success available on low-energy days.

**Builds on:** Habit stack items, tasks, identity links, completions, and identity scoring.

**Low-pressure rule:** A tiny vote is displayed as a real success, never as an incomplete or second-class completion. Avoid large bonus rewards for the full version that would undermine this message.

**First release:** Add an optional `minimumVersion` to habits and tasks, then offer **Do tiny version** from Today.

## 2. Begin Again Recovery

When a day is missed, replace loss-focused streak messaging with a short recovery flow: **Rested intentionally**, **Life got in the way**, **The action was too large**, or **The cue did not happen**. Offer one tap to resume, shrink, reschedule, or pause the activity.

**Why it matters:** The moment after a miss is where many trackers create shame and abandonment. Helping the user return is more valuable than celebrating an unbroken streak.

**Builds on:** Existing missed and dismissed daily commitments, skipped days, streak data, and the yesterday recovery message.

**Low-pressure rule:** Never require an explanation and never erase progress. Track returns and recovery speed as positive signals instead of highlighting broken streaks.

**First release:** Add **Begin again today** after a miss and celebrate the first new vote as a comeback.

## 3. Capacity-Aware Today

Start Today with an optional capacity choice: **Low**, **Normal**, or **High**. Use it to show a realistic plan: one tiny identity vote on a low-capacity day, the normal plan on a regular day, and optional extras only when capacity is high.

**Why it matters:** A static task list treats every day as equally easy. Capacity-aware planning lets the app adapt to the person instead of making the person fail the plan.

**Builds on:** Daily commitments, identity recommendations, Today tasks, habit stacks, and minimum viable votes.

**Low-pressure rule:** Capacity is private context, not a performance metric. Low-capacity days must not lower scores or trigger warnings.

**First release:** Store only today's capacity and use it to recommend the size of one daily commitment. Long-term mood tracking is unnecessary at first.

## 4. Flexible Rhythms

Support targets such as **three times this week**, **most weekdays**, **once this weekend**, and **whenever the cue occurs**, alongside fixed weekday schedules. Let users move a planned occurrence within its window without creating an overdue item.

**Why it matters:** Many valuable behaviors do not belong on exact days. Weekly rhythms support consistency while allowing illness, travel, shift work, and ordinary unpredictability.

**Builds on:** Odd/even week habit scheduling, Today, completion history, and adherence analytics.

**Low-pressure rule:** Show opportunities remaining, not failures accumulating. “One more opportunity this week” is better than “two days missed.”

**First release:** Add a weekly frequency schedule to habit stacks and calculate progress across the user's local week.

## 5. Compassionate Weekly Review

Offer a five-minute weekly review centered on learning:

1. Which identity felt most alive?
2. What became easier?
3. What created friction?
4. What should become smaller, move, pause, or stay the same?
5. What is one vote worth making next week?

Generate a short review card from recent votes, but let the user edit or skip every answer.

**Why it matters:** Data helps only when it changes the next decision. A review turns analytics into gentle adaptation instead of a report card.

**Builds on:** Journal entries, identity scores, completions, proofs, and analytics.

**Low-pressure rule:** Do not assign a weekly grade, productivity score, red status, or comparison with an ideal week.

**First release:** Create a journal-backed review template with prefilled facts and one chosen adjustment.

## 6. Goal Seasons and Maintenance Mode

Let goals and identities move between **Growing**, **Maintaining**, **Resting**, and **Complete**. A user might actively grow as a runner this season while maintaining their reader identity and resting a career goal.

**Why it matters:** Trying to improve everything at once creates pressure and diluted attention. Seasons make deliberate focus and deliberate rest legitimate.

**Builds on:** Active and completed goals, active habit stacks, identities, and pausing behavior.

**Low-pressure rule:** Resting is a healthy state, not failure. Paused goals should disappear from overdue counts while preserving history and an easy return path.

**First release:** Add **Rest until...** to goals and stacks, then show at most one or two growing identities on Today.

## 7. Identity Evidence Story

Create a chronological, human-readable story for each identity that combines habit completions, completed tasks, identity proofs, journal excerpts, photos, and comeback moments. Summarize it as evidence: “You cast 14 votes for being a caring friend this month.”

**Why it matters:** A score is abstract; remembered evidence changes self-belief. This makes identity votes the emotional center of the product.

**Builds on:** Identity proofs, identity scoring, linked journal entries and images, habit completions, and tasks.

**Low-pressure rule:** Lead with concrete evidence rather than ranking the identity as strong or weak. Let the user hide numeric scores entirely.

**First release:** Add an **Evidence** tab to each identity and group existing events by week.

## 8. Friction Plans

Let the user attach a simple obstacle response to a habit or goal task: **If [obstacle], then I will [smaller or alternate action]**. Examples: “If it rains, I will stretch indoors” or “If I miss the morning cue, I will read after lunch.”

**Why it matters:** Habit stacks define what happens after a good cue. Friction plans cover predictable moments when the ideal plan fails.

**Builds on:** Habit cues, minimum versions, task editing, and recovery reasons.

**Low-pressure rule:** Suggest a plan only after repeated friction or when the user asks. Do not create more setup work during onboarding.

**First release:** Add one optional obstacle and fallback action per stack, surfaced contextually on Today.

## 9. Two-Week Experiments

Allow the user to try a behavior as an experiment with a question such as, “Does a ten-minute walk after lunch improve my afternoon energy?” At the end, ask whether to adopt, adjust, extend, or discard it.

**Why it matters:** Experiments replace permanent promises with curiosity. They are especially useful when a user does not yet know which routine fits their life.

**Builds on:** Habit stacks, schedules, journal reflection, completion history, and optional AI guidance.

**Low-pressure rule:** Discarding an experiment is a successful result because the user learned something. Experiments should not affect streaks or milestone eligibility by default.

**First release:** Add an optional end date and reflection prompt to a habit stack, with manual adoption afterward.

## 10. Gentle Insights

Surface one optional, plain-language observation at a time, such as “Your reading votes happen more often after lunch than before work” or “This habit has been postponed three times; making it smaller may help.” Every insight should offer **Useful**, **Not for me**, and **Hide insights like this**.

**Why it matters:** The app already gathers enough information to help users notice patterns, but conventional dashboards can feel clinical or judgmental. Small observations are easier to act on.

**Builds on:** Analytics, habit timing, task changes, identity votes, journaling, and optional local or OpenAI-backed suggestions.

**Low-pressure rule:** Describe patterns, never diagnose causes or predict failure. Core deterministic insights must work without an OpenAI key and remain private in this self-hosted app.

**First release:** Implement a few transparent rules, starting with repeated postponement and consistently successful time windows.

## Recommended Sequence

### Phase 1: Make Success Easier

Build **Minimum Viable Votes**, **Begin Again Recovery**, **Capacity-Aware Today**, and **Friction Plans** together. They form one coherent daily loop: choose a realistic action, use a fallback when needed, and recover cleanly after disruption.

### Phase 2: Make the System Adapt

Add **Flexible Rhythms**, **Compassionate Weekly Review**, **Goal Seasons**, and the **Identity Evidence Story**. This shifts the app from tracking a fixed plan to helping the user maintain a sustainable practice.

### Phase 3: Help the User Learn

Add **Two-Week Experiments** and **Gentle Insights** after enough real usage data exists to validate which guidance is useful.

## Product Guardrails

- Reward returning, adapting, and resting intentionally, not only uninterrupted streaks.
- Never subtract identity votes or frame inactivity as evidence against an identity.
- Make success achievable in under two minutes on a hard day.
- Keep Today finite: one meaningful vote can make the day successful.
- Prefer descriptive language over judgmental labels, red warnings, and grades.
- Make every reminder, review, score, milestone, and AI feature optional.
- Preserve local-first privacy and full usefulness without an OpenAI key.
- Test the emotional tone of empty, missed, overdue, paused, and comeback states as carefully as the happy path.

## Success Measures

Avoid optimizing for raw task volume or perfect streak length. Better product measures are:

- Percentage of missed days followed by a new identity vote within three days.
- Percentage of active users who record at least one meaningful vote per week.
- Use of shrink, reschedule, pause, and fallback actions instead of abandonment.
- User-reported pressure after the first week and first month.
- Percentage of goals intentionally completed, maintained, or rested rather than silently abandoned.
- Retention with notifications and AI disabled, proving that the core behavior loop stands on its own.