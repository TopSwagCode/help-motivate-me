<script lang="ts">
	interface Props {
		class?: string;
	}
	let { class: className = '' }: Props = $props();
</script>

<svg
	xmlns="http://www.w3.org/2000/svg"
	width="16"
	height="16"
	viewBox="0 0 20 20"
	fill="none"
	stroke="currentColor"
	stroke-width="1.5"
	stroke-linecap="round"
	stroke-linejoin="round"
	class={className}
>
	<!-- Three stacked layers representing habit chains -->
	<rect x="4" y="3" width="12" height="3" rx="1" fill="currentColor" opacity="0.3" stroke="none" />
	<rect x="4" y="8" width="12" height="3" rx="1" fill="currentColor" opacity="0.5" stroke="none" />
	<rect x="4" y="13" width="12" height="3" rx="1" fill="currentColor" opacity="0.8" stroke="none" />
	<!-- Connecting dots -->
	<circle cx="10" cy="6.5" r="0.75" fill="currentColor" stroke="none" />
	<circle cx="10" cy="11.5" r="0.75" fill="currentColor" stroke="none" />
</svg>
