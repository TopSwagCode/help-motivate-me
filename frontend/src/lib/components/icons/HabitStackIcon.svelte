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
	viewBox="0 0 24 24"
	fill="none"
	stroke="currentColor"
	stroke-width="2"
	stroke-linecap="round"
	stroke-linejoin="round"
	class={className}
>
	<!-- Four horizontal lines matching the navigation icon -->
	<path d="M4 6h16" />
	<path d="M4 10h16" />
	<path d="M4 14h16" />
	<path d="M4 18h16" />
</svg>
