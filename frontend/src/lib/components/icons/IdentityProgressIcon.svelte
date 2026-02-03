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
	<path d="M3 16V12" />
	<path d="M7 16V9" />
	<path d="M11 16V6" />
	<path d="M15 16V4" />
	<circle cx="15" cy="4" r="1.5" fill="currentColor" stroke="none" />
</svg>
