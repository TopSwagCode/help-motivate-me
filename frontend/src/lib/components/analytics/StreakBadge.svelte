<script lang="ts">
	interface Props {
		currentStreak: number;
		isOnGracePeriod?: boolean;
		daysUntilStreakBreaks?: number;
		size?: 'sm' | 'md' | 'lg';
	}

	let {
		currentStreak,
		isOnGracePeriod = false,
		daysUntilStreakBreaks = 0,
		size = 'md'
	}: Props = $props();

	const sizeClasses = {
		sm: 'text-xs px-2 py-0.5',
		md: 'text-sm px-2.5 py-1',
		lg: 'text-base px-3 py-1.5'
	};

	const getStreakColor = () => {
		if (currentStreak === 0) return 'bg-gray-100 text-cocoa-600';
		if (isOnGracePeriod) return 'bg-yellow-100 text-yellow-700';
		if (currentStreak >= 30) return 'bg-orange-100 text-orange-700';
		if (currentStreak >= 7) return 'bg-green-100 text-green-700';
		return 'bg-blue-100 text-blue-700';
	};

	const getStreakEmoji = () => {
		if (currentStreak === 0) return '';
		if (isOnGracePeriod) return '!!!';
		if (currentStreak >= 30) return ' ';
		if (currentStreak >= 7) return ' ';
		return ' ';
	};
</script>

{#if currentStreak > 0 || isOnGracePeriod}
	<span
		class="inline-flex items-center gap-1 rounded-full font-medium {sizeClasses[size]} {getStreakColor()}"
		title={isOnGracePeriod
			? "Don't break your streak! Complete this habit today."
			: `${currentStreak} day streak`}
	>
		<span>{getStreakEmoji()}</span>
		<span>{currentStreak}</span>
		{#if isOnGracePeriod}
			<span class="text-xs">(Grace)</span>
		{/if}
	</span>
{/if}
