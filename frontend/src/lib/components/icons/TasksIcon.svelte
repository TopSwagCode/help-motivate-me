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
	<!-- Clipboard shape -->
	<rect x="4" y="3" width="12" height="14" rx="2" />
	<!-- Clipboard top -->
	<path d="M8 3V2a1 1 0 011-1h2a1 1 0 011 1v1" />
	<!-- Task lines -->
	<line x1="7" y1="8" x2="13" y2="8" />
	<line x1="7" y1="11" x2="13" y2="11" />
	<line x1="7" y1="14" x2="10" y2="14" />
</svg>
