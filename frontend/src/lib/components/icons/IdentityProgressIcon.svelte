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
	<!-- Person silhouette with upward arrow representing identity growth -->
	<circle cx="10" cy="5" r="2.5" />
	<path d="M5 17v-2a5 5 0 0 1 10 0v2" />
	<!-- Upward growth arrow -->
	<path d="M15 8l2.5 -3.5M17.5 4.5l-2.5 0M17.5 4.5l0 2.5" stroke-width="1.5" />
</svg>
