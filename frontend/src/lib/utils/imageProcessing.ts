/**
 * Image processing utilities for client-side image optimization
 * Handles image compression, resizing, and WebP conversion
 */

// Configuration constants
const MAX_IMAGE_WIDTH = 1920;
const MAX_IMAGE_HEIGHT = 1920;
const WEBP_QUALITY = 0.85;
const MAX_FILE_SIZE = 5 * 1024 * 1024; // 5MB
const GIF_SIZE_WARNING_THRESHOLD = 2 * 1024 * 1024; // 2MB

export interface ProcessedImageResult {
	file: File;
	wasProcessed: boolean;
	originalSize: number;
	newSize: number;
	warning?: string;
}

/**
 * Check if the file is a GIF
 */
function isGif(file: File): boolean {
	return file.type === 'image/gif';
}

/**
 * Check if the browser supports WebP
 */
function supportsWebP(): boolean {
	const canvas = document.createElement('canvas');
	canvas.width = 1;
	canvas.height = 1;
	return canvas.toDataURL('image/webp').indexOf('data:image/webp') === 0;
}

/**
 * Load an image file into an HTMLImageElement
 */
function loadImage(file: File): Promise<HTMLImageElement> {
	return new Promise((resolve, reject) => {
		const img = new Image();
		const url = URL.createObjectURL(file);

		img.onload = () => {
			URL.revokeObjectURL(url);
			resolve(img);
		};

		img.onerror = () => {
			URL.revokeObjectURL(url);
			reject(new Error('Failed to load image'));
		};

		img.src = url;
	});
}

/**
 * Calculate new dimensions while maintaining aspect ratio
 */
function calculateNewDimensions(
	width: number,
	height: number,
	maxWidth: number,
	maxHeight: number
): { width: number; height: number } {
	if (width <= maxWidth && height <= maxHeight) {
		return { width, height };
	}

	const aspectRatio = width / height;

	if (width > height) {
		// Landscape
		const newWidth = Math.min(width, maxWidth);
		const newHeight = Math.round(newWidth / aspectRatio);
		return { width: newWidth, height: newHeight };
	} else {
		// Portrait or square
		const newHeight = Math.min(height, maxHeight);
		const newWidth = Math.round(newHeight * aspectRatio);
		return { width: newWidth, height: newHeight };
	}
}

/**
 * Convert a canvas to a File object
 */
function canvasToFile(canvas: HTMLCanvasElement, fileName: string, mimeType: string, quality: number): Promise<File> {
	return new Promise((resolve, reject) => {
		canvas.toBlob(
			(blob) => {
				if (!blob) {
					reject(new Error('Failed to convert canvas to blob'));
					return;
				}

				// Create a new filename with .webp extension
				const nameWithoutExt = fileName.replace(/\.[^/.]+$/, '');
				const newFileName = mimeType === 'image/webp' ? `${nameWithoutExt}.webp` : fileName;

				const file = new File([blob], newFileName, { type: mimeType });
				resolve(file);
			},
			mimeType,
			quality
		);
	});
}

/**
 * Process a regular image (non-GIF): resize and convert to WebP
 */
async function processRegularImage(file: File): Promise<ProcessedImageResult> {
	const originalSize = file.size;

	// If file is already small enough and WebP, return as-is
	if (file.type === 'image/webp' && file.size < MAX_FILE_SIZE) {
		const img = await loadImage(file);
		if (img.width <= MAX_IMAGE_WIDTH && img.height <= MAX_IMAGE_HEIGHT) {
			return {
				file,
				wasProcessed: false,
				originalSize,
				newSize: file.size
			};
		}
	}

	// Load the image
	const img = await loadImage(file);

	// Calculate new dimensions
	const { width, height } = calculateNewDimensions(
		img.width,
		img.height,
		MAX_IMAGE_WIDTH,
		MAX_IMAGE_HEIGHT
	);

	// Create canvas and draw resized image
	const canvas = document.createElement('canvas');
	canvas.width = width;
	canvas.height = height;

	const ctx = canvas.getContext('2d');
	if (!ctx) {
		throw new Error('Failed to get canvas context');
	}

	// Use better image smoothing
	ctx.imageSmoothingEnabled = true;
	ctx.imageSmoothingQuality = 'high';
	ctx.drawImage(img, 0, 0, width, height);

	// Convert to WebP if supported, otherwise use JPEG
	const mimeType = supportsWebP() ? 'image/webp' : 'image/jpeg';
	const processedFile = await canvasToFile(canvas, file.name, mimeType, WEBP_QUALITY);

	return {
		file: processedFile,
		wasProcessed: true,
		originalSize,
		newSize: processedFile.size
	};
}

/**
 * Process a GIF file: validate size and warn if too large
 */
function processGif(file: File): ProcessedImageResult {
	const originalSize = file.size;
	let warning: string | undefined;

	if (file.size > MAX_FILE_SIZE) {
		warning = `This GIF is too large (${(file.size / 1024 / 1024).toFixed(2)}MB). Maximum size is ${MAX_FILE_SIZE / 1024 / 1024}MB. Please use a smaller GIF or convert it to a video format.`;
	} else if (file.size > GIF_SIZE_WARNING_THRESHOLD) {
		warning = `Large GIF detected (${(file.size / 1024 / 1024).toFixed(2)}MB). Consider using a smaller GIF for better performance.`;
	}

	return {
		file,
		wasProcessed: false,
		originalSize,
		newSize: file.size,
		warning
	};
}

/**
 * Process an image file for upload
 * - GIFs are kept as-is but validated for size
 * - Other images are resized and converted to WebP
 *
 * @param file - The image file to process
 * @returns ProcessedImageResult with the processed file and metadata
 * @throws Error if the file cannot be processed or is too large
 */
export async function processImageForUpload(file: File): Promise<ProcessedImageResult> {
	// Validate that it's an image
	if (!file.type.startsWith('image/')) {
		throw new Error('File must be an image');
	}

	// Handle GIFs separately (keep them as-is, just validate size)
	if (isGif(file)) {
		const result = processGif(file);
		if (result.warning && file.size > MAX_FILE_SIZE) {
			throw new Error(result.warning);
		}
		return result;
	}

	// Process regular images (resize and convert to WebP)
	try {
		const result = await processRegularImage(file);

		// If processing somehow resulted in a larger file, use the original
		if (result.newSize > result.originalSize && result.originalSize < MAX_FILE_SIZE) {
			return {
				file,
				wasProcessed: false,
				originalSize: result.originalSize,
				newSize: result.originalSize
			};
		}

		// Verify the processed file is within size limits
		if (result.newSize > MAX_FILE_SIZE) {
			throw new Error(
				`Image is too large even after compression (${(result.newSize / 1024 / 1024).toFixed(2)}MB). Please use a smaller image.`
			);
		}

		return result;
	} catch (error) {
		if (error instanceof Error) {
			throw error;
		}
		throw new Error('Failed to process image');
	}
}

/**
 * Process multiple image files
 * Returns both successfully processed files and any errors
 */
export async function processMultipleImages(files: File[]): Promise<{
	processed: ProcessedImageResult[];
	errors: Array<{ fileName: string; error: string }>;
}> {
	const processed: ProcessedImageResult[] = [];
	const errors: Array<{ fileName: string; error: string }> = [];

	for (const file of files) {
		try {
			const result = await processImageForUpload(file);
			processed.push(result);
		} catch (error) {
			errors.push({
				fileName: file.name,
				error: error instanceof Error ? error.message : 'Failed to process image'
			});
		}
	}

	return { processed, errors };
}

/**
 * Format file size for display
 */
export function formatFileSize(bytes: number): string {
	if (bytes < 1024) {
		return `${bytes} B`;
	} else if (bytes < 1024 * 1024) {
		return `${(bytes / 1024).toFixed(1)} KB`;
	} else {
		return `${(bytes / 1024 / 1024).toFixed(2)} MB`;
	}
}
