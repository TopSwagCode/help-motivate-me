// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces
declare global {
	namespace App {
		// interface Error {}
		// interface Locals {}
		// interface PageData {}
		// interface PageState {}
		// interface Platform {}
	}
}

// PWA Virtual Module Types - these are provided by vite-plugin-pwa at runtime
declare module 'virtual:pwa-register/svelte' {
	// eslint-disable-next-line @typescript-eslint/no-explicit-any
	export function useRegisterSW(options?: any): any;
}

export {};
