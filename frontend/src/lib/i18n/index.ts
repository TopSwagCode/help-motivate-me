import { browser } from '$app/environment';
import { init, register, locale, getLocaleFromNavigator } from 'svelte-i18n';

const defaultLocale = 'en';

register('en', () => import('./locales/en.json'));
register('da', () => import('./locales/da.json'));

export function initI18n() {
	init({
		fallbackLocale: defaultLocale,
		initialLocale: browser ? getInitialLocale() : defaultLocale
	});
}

function getInitialLocale(): string {
	// Check localStorage first (for returning users)
	const stored = localStorage.getItem('preferredLanguage');
	if (stored && ['en', 'da'].includes(stored)) {
		return stored;
	}

	// Try to detect from browser
	const browserLocale = getLocaleFromNavigator();
	if (browserLocale?.startsWith('da')) {
		return 'da';
	}

	return defaultLocale;
}

export function setLocale(newLocale: string) {
	if (['en', 'da'].includes(newLocale)) {
		locale.set(newLocale);
		if (browser) {
			localStorage.setItem('preferredLanguage', newLocale);
		}
	}
}

export function getLocaleFromLanguage(language: string): string {
	// Map backend Language enum values to locale codes
	switch (language.toLowerCase()) {
		case 'danish':
			return 'da';
		case 'english':
		default:
			return 'en';
	}
}

export function getLanguageFromLocale(localeCode: string): string {
	// Map locale codes to backend Language enum values
	switch (localeCode) {
		case 'da':
			return 'Danish';
		case 'en':
		default:
			return 'English';
	}
}

export { locale } from 'svelte-i18n';
