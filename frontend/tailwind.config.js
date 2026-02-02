/** @type {import('tailwindcss').Config} */
export default {
	content: ['./src/**/*.{html,js,svelte,ts}'],
	theme: {
		extend: {
			screens: {
				'xs': '475px', // Extra small devices
			},
			colors: {
				primary: {
					50: '#fefcf9',   // Lightest cream
					100: '#fdf6ed',  // Light cream
					200: '#f9e8d2',  // Warm beige
					300: '#f2d4ae',  // Light caramel
					400: '#e8b87a',  // Medium caramel
					500: '#d4944c',  // Main caramel
					600: '#b87a3a',  // Hover states
					700: '#965e2c',  // Darker
					800: '#7a4a24',  // Deep
					900: '#643c1e',  // Very dark
					950: '#3d2412',  // Darkest
				},
				warm: {
					cream: '#faf7f2',   // Main background
					beige: '#f5f0e8',   // Secondary bg
					paper: '#fdfcfa',   // Card background
				},
				sage: {
					400: '#94a57f',
					500: '#768862',     // Accent green
					600: '#5d6d4e',
				},
				cocoa: {
					400: '#b09780',     // Placeholder text
					500: '#9a7d64',     // Muted text
					600: '#8a6a54',     // Secondary text
					700: '#735748',     // Body text
					800: '#5f483d',     // Primary text
					900: '#4a3830',     // Headlines
				},
			},
			fontFamily: {
				sans: ['Nunito', 'Inter', 'system-ui', 'sans-serif']
			}
		}
	},
	plugins: [require('@tailwindcss/forms')]
};
