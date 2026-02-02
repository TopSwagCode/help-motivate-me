# Tailwind Implementation Plan: Grass / Ground UI Layers

This document describes how to implement a **subtle grass / ground visual layer** behind the **top navigation** and **bottom app navigation** using **Tailwind CSS**, while keeping the UI clean, readable, and responsive.

---

## Design Goals
- Add an organic “grounded” feeling to the UI
- Keep visuals subtle and low-contrast
- Avoid distracting from navigation or content
- Ensure responsiveness across screen sizes
- Maintain accessibility and readability

---

## Core Layering Concept
Use **three visual layers**:

1. **Base UI layer** – navigation, text, icons
2. **Grass / ground layer** – decorative background
3. **Fade / mask layer** – soft gradient to blend into UI

All grass elements live **behind** UI content using `absolute` positioning and `z-index`.

---

## Color Tokens (Example)
Define these in your Tailwind theme if possible:

- `bg-base`: warm beige / off-white
- `bg-header`: caramel / warm brown
- `grass-light`: muted sage green
- `grass-dark`: soft olive green
- `text-main`: dark cocoa brown

Avoid pure green or high saturation.

---

## Top Navigation (Header)

### Structure
```html
<header class="relative overflow-hidden bg-header rounded-b-3xl">
  <!-- Grass layer -->
  <div class="absolute bottom-0 left-0 w-full h-24 z-0">
    <div class="w-full h-full bg-gradient-to-t from-grass-dark/40 to-transparent"></div>
  </div>

  <!-- Header content -->
  <div class="relative z-10 px-6 py-5">
    <!-- logo / nav content -->
  </div>
</header>
```

### Notes
- Grass occupies bottom ~25% of header
- Use opacity instead of strong color
- Keep text fully above grass layer
- Use `rounded-b-3xl` for softness

---

## Bottom App Navigation

### Structure
```html
<nav class="fixed bottom-0 left-0 w-full pointer-events-none">
  <!-- Grass background -->
  <div class="absolute bottom-0 left-0 w-full h-32 z-0">
    <div class="w-full h-full bg-gradient-to-b from-grass-light/30 to-transparent"></div>
  </div>

  <!-- Navigation bar -->
  <div class="relative z-10 mx-auto max-w-md pointer-events-auto
              bg-base rounded-t-3xl shadow-sm px-6 py-3">
    <!-- nav icons -->
  </div>
</nav>
```

### Notes
- Grass stays behind nav
- Navigation remains clean and readable
- Slight shadow separates nav from background
- Pointer events disabled on background layer

---

## Optional: Illustrated Grass Layer
If using SVG or PNG illustration:

```html
<img
  src="/grass-layer.svg"
  class="absolute bottom-0 left-0 w-full opacity-40 z-0
         pointer-events-none select-none"
/>
```

Guidelines:
- Rounded, stylized shapes only
- 2–3 tones maximum
- No outlines or sharp edges

---

## Responsive Behavior
Use breakpoints to adjust grass height:

```html
<div class="h-20 sm:h-24 lg:h-28"></div>
```

Recommendations:
- Mobile: slightly taller, softer
- Desktop: shallower and wider
- Very small heights: hide grass entirely

---

## Optional Subtle Motion
For calm polish (optional):

```html
<div class="animate-[float_12s_ease-in-out_infinite]"></div>
```

Keep animation:
- Slow (10s+)
- Small movement (1–2px)
- Disabled during interaction

---

## Accessibility Considerations
- Ensure text contrast stays high
- Never place text directly on grass textures
- Keep decorative layers `aria-hidden="true"`
- Avoid motion for users with reduced-motion preferences

---

## Final Checklist
- [ ] Grass is decorative, not dominant
- [ ] UI remains readable at all times
- [ ] Colors are muted and warm
- [ ] No sharp edges or hard dividers
- [ ] Responsive behavior tested

---

## Mental Test
If the grass is noticeable at first glance, reduce:
- Opacity
- Height
- Saturation

Subtlety is the goal.
