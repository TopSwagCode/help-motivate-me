Daily Identity Commitment – Periodic Notification Worker Plan

This document defines the architecture, logic, and execution plan for the Daily Identity Commitment push notification worker.

It is designed to be implemented using MinimalWorker – Periodic Worker semantics (see referenced README), and intentionally avoids user-specific cron schedules in favor of frequent eligibility scanning.

⸻

1. Goal

The worker’s job is not to send notifications on a schedule.

Its job is to:

Continuously scan for users who are currently eligible to receive a Daily Identity Commitment notification, and send at most one appropriate notification per window per day.

This preserves:
	•	Calm UX
	•	Identity-based intent
	•	User trust
	•	Deterministic behavior

⸻

2. Worker Type
	•	Type: Periodic Background Worker
	•	Execution Interval: Every 5 minutes
	•	Trigger Mechanism: Central scheduler (not per-user)

The worker must be idempotent and safe to re-run frequently.

worker documentation here: https://raw.githubusercontent.com/TopSwagCode/MinimalWorker/refs/heads/master/README.llm

⸻

3. Core Responsibilities

On each execution, the worker must:
	1.	Fetch candidate users
	2.	Resolve user-local time
	3.	Resolve active notification window
	4.	Evaluate eligibility rules
	5.	Send at most one notification per window
	6.	Persist notification metadata

⸻

4. Required User Data (Already Exists)

// Schedule - Time slots
public TimeSlot PreferredTimeSlot { get; set; } = TimeSlot.Morning;
public TimeOnly? CustomTimeStart { get; set; }
public TimeOnly? CustomTimeEnd { get; set; }

// Timezone
public string TimezoneId { get; set; } = "UTC";
public int UtcOffsetMinutes { get; set; } = 0;

Slot definitions:
	•	Morning: 06:00 – 12:00
	•	Afternoon: 12:00 – 18:00
	•	Evening: 18:00 – 22:00

Custom time always overrides slot selection.

⸻

5. Daily Commitment State Model (Logical)

The worker relies on a derived daily state per user:
	•	NotStarted
	•	Committed
	•	Completed
	•	DismissedForToday
	•	Expired

The worker only targets NotStarted users.

⸻

6. Notification Windows

Each day has up to three possible windows:

Window	Purpose
Morning	Invitation
Midday	Gentle reminder
Evening	Reflection

A user may receive zero or one notification per window.

⸻

7. Eligibility Rules (Must All Pass)

A user is eligible only if all conditions below are true:
	•	Notifications enabled
	•	Daily commitment not completed
	•	Daily commitment not dismissed
	•	User has not opened app today
	•	Current local time is inside user’s allowed window
	•	User has not already been notified in this window today

If any rule fails → skip silently.

⸻

8. Time Resolution Logic

8.1 Resolve Local Time

UTC Now → TimezoneId → Local DateTime → Local TimeOnly

TimezoneId is authoritative. UtcOffsetMinutes is fallback only.

⸻

8.2 Resolve Notification Window

Resolution rules:

IF CustomTimeStart AND CustomTimeEnd exist
    → Use custom window
ELSE
    → Use PreferredTimeSlot window


⸻

8.3 Window Containment Check

The system must support both same-day and overnight windows.

IF start < end
    now ∈ [start, end)
ELSE
    now ≥ start OR now < end


⸻

9. Worker Execution Flow

High-Level Flow

Every 5 minutes:
    Fetch candidate users
    For each user:
        Resolve local time
        Resolve notification window
        If not inside window → continue
        If eligibility rules fail → continue
        Determine notification type
        Send push notification
        Persist notification record


⸻

10. Candidate User Query (Critical for Scale)

The worker must not scan all users.

Initial filter should include:
	•	Notifications enabled
	•	Daily commitment not completed
	•	Daily commitment not dismissed

Further filtering happens in-memory per user.

⸻

11. Notification Type Selection

Based on:
	•	Current window (Morning / Midday / Evening)
	•	Commitment state

Examples:
	•	Morning + NotStarted → Identity invitation
	•	Midday + Committed → Action reminder
	•	Evening + Committed → Reflection prompt

Copy selection is state-driven, not time-driven.

⸻

12. Persistence Requirements

Each successful notification send must persist:
	•	UserId
	•	LocalDate
	•	WindowType (Morning / Midday / Evening)
	•	SentAtUtc

This guarantees:
	•	No duplicate sends
	•	Idempotent retries
	•	Debuggable behavior

⸻

13. Failure Handling
	•	A single user failure must not fail the worker
	•	Exceptions are logged per-user
	•	Worker continues processing remaining users

No retries within the same execution cycle.

⸻

14. Design Principles (Non-Negotiable)
	•	Never nag
	•	Never shame
	•	Never double-send
	•	Never break user trust
	•	Notifications are invitations, not commands

⸻

15. Why Periodic > Scheduled

This design:
	•	Handles timezones correctly
	•	Survives deploys and restarts
	•	Allows future intelligence (behavior-based timing)
	•	Scales without cron explosion
	•	Aligns with identity-based UX

⸻

16. Future Extensions (Explicitly Out of Scope)
	•	Email delivery
	•	A/B testing
	•	Adaptive timing
	•	Multi-device coordination

These are intentionally deferred until push behavior is stable.

⸻

17. Summary

This worker is a ritual gatekeeper, not a scheduler.

It quietly watches for the right moment, asks one meaningful question, and then steps aside.

That restraint is the feature.