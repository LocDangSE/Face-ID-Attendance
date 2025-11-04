/**
 * Image Processing Utilities
 * Handle image conversion, compression, and validation
 */

/**
 * Convert Blob to File
 */
export const blobToFile = (blob: Blob, filename: string): File => {
    return new File([blob], filename, {
        type: blob.type || 'image/jpeg',
        lastModified: Date.now()
    });
};

/**
 * Convert base64 to Blob
 */
export const base64ToBlob = async (base64: string): Promise<Blob> => {
    const response = await fetch(base64);
    return response.blob();
};

/**
 * Compress image file
 * @param file - Image file to compress
 * @param maxSizeMB - Maximum size in megabytes
 * @param maxWidthOrHeight - Maximum width or height in pixels
 * @returns Compressed image file
 */
export const compressImage = async (
    file: File,
    maxSizeMB: number = 1,
    maxWidthOrHeight: number = 1920
): Promise<File> => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();

        reader.onload = (e) => {
            const img = new Image();

            img.onload = () => {
                const canvas = document.createElement('canvas');
                let { width, height } = img;

                // Calculate new dimensions
                if (width > height) {
                    if (width > maxWidthOrHeight) {
                        height = (height * maxWidthOrHeight) / width;
                        width = maxWidthOrHeight;
                    }
                } else {
                    if (height > maxWidthOrHeight) {
                        width = (width * maxWidthOrHeight) / height;
                        height = maxWidthOrHeight;
                    }
                }

                canvas.width = width;
                canvas.height = height;

                const ctx = canvas.getContext('2d');
                if (!ctx) {
                    reject(new Error('Failed to get canvas context'));
                    return;
                }

                ctx.drawImage(img, 0, 0, width, height);

                // Convert to blob with quality adjustment
                canvas.toBlob(
                    (blob) => {
                        if (!blob) {
                            reject(new Error('Failed to compress image'));
                            return;
                        }

                        const compressedFile = blobToFile(blob, file.name);

                        // Check if size is acceptable
                        if (compressedFile.size > maxSizeMB * 1024 * 1024) {
                            console.warn(`[ImageProcessor] Compressed size ${(compressedFile.size / 1024 / 1024).toFixed(2)}MB exceeds ${maxSizeMB}MB`);
                        }

                        resolve(compressedFile);
                    },
                    'image/jpeg',
                    0.85 // Quality
                );
            };

            img.onerror = () => reject(new Error('Failed to load image'));
            img.src = e.target?.result as string;
        };

        reader.onerror = () => reject(new Error('Failed to read file'));
        reader.readAsDataURL(file);
    });
};

/**
 * Validate image file
 */
export interface ImageValidationResult {
    valid: boolean;
    error?: string;
}

export const validateImageFile = (file: File | null): ImageValidationResult => {
    if (!file) {
        return { valid: false, error: 'No file provided' };
    }

    // Check file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
        return {
            valid: false,
            error: `Invalid file type. Allowed: ${allowedTypes.join(', ')}`
        };
    }

    // Check file size (max 10MB)
    const maxSize = 10 * 1024 * 1024; // 10MB
    if (file.size > maxSize) {
        return {
            valid: false,
            error: `File too large. Maximum size: ${maxSize / 1024 / 1024}MB`
        };
    }

    return { valid: true };
};

/**
 * Get image dimensions
 */
export const getImageDimensions = (file: File): Promise<{ width: number; height: number }> => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();

        reader.onload = (e) => {
            const img = new Image();

            img.onload = () => {
                resolve({ width: img.width, height: img.height });
            };

            img.onerror = () => reject(new Error('Failed to load image'));
            img.src = e.target?.result as string;
        };

        reader.onerror = () => reject(new Error('Failed to read file'));
        reader.readAsDataURL(file);
    });
};

/**
 * Create image preview URL
 */
export const createImagePreview = (file: File): Promise<string> => {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();

        reader.onload = (e) => {
            resolve(e.target?.result as string);
        };

        reader.onerror = () => reject(new Error('Failed to create preview'));
        reader.readAsDataURL(file);
    });
};

/**
 * Revoke image preview URL
 */
export const revokeImagePreview = (url: string): void => {
    if (url.startsWith('blob:')) {
        URL.revokeObjectURL(url);
    }
};
