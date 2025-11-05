/**
 * Face Detection Utilities
 * Helper functions for BlazeFace detection, face positioning, and visual feedback
 */

import type * as blazeface from '@tensorflow-models/blazeface';

export interface DetectionBox {
    x: number;
    y: number;
    width: number;
    height: number;
}

export interface FacePosition {
    centerX: number;
    centerY: number;
    size: number; // Face size as percentage of frame
    isCentered: boolean;
    isCloseEnough: boolean;
    isGoodQuality: boolean;
}

/**
 * Configuration for face detection
 */
export const DETECTION_CONFIG = {
    // Detection frequency (ms between detections)
    DETECTION_INTERVAL: 100, // 10 FPS for detection loop

    // Face positioning thresholds
    CENTER_TOLERANCE: 0.15, // Face can be 15% off center
    MIN_FACE_SIZE: 0.15, // Face should be at least 15% of frame
    MAX_FACE_SIZE: 0.75, // Face should be at most 75% of frame
    OPTIMAL_FACE_SIZE: 0.35, // Optimal face size is 35% of frame

    // Quality thresholds
    MIN_CONFIDENCE: 0.85, // BlazeFace confidence threshold

    // Capture debouncing
    CAPTURE_COOLDOWN: 3000, // 3 seconds between auto-captures
    FACE_STABLE_TIME: 500, // Face must be stable for 500ms before capture

    // Visual feedback
    BOX_COLOR_GOOD: '#10B981', // Green when ready
    BOX_COLOR_CENTERING: '#F59E0B', // Yellow when adjusting
    BOX_COLOR_TOO_FAR: '#EF4444', // Red when too far
    BOX_LINE_WIDTH: 3,
} as const;

/**
 * Convert BlazeFace prediction to normalized box coordinates
 */
export function predictionToBox(
    prediction: blazeface.NormalizedFace,
    _videoWidth: number,
    _videoHeight: number
): DetectionBox {
    const topLeft = prediction.topLeft as [number, number];
    const bottomRight = prediction.bottomRight as [number, number];

    return {
        x: topLeft[0],
        y: topLeft[1],
        width: bottomRight[0] - topLeft[0],
        height: bottomRight[1] - topLeft[1]
    };
}

/**
 * Analyze face position and quality
 */
export function analyzeFacePosition(
    box: DetectionBox,
    videoWidth: number,
    videoHeight: number,
    confidence: number
): FacePosition {
    // Calculate face center (normalized 0-1)
    const centerX = (box.x + box.width / 2) / videoWidth;
    const centerY = (box.y + box.height / 2) / videoHeight;

    // Calculate face size as percentage of frame
    const faceArea = box.width * box.height;
    const frameArea = videoWidth * videoHeight;
    const size = Math.sqrt(faceArea / frameArea);

    // Check if face is centered (within tolerance of 0.5, 0.5)
    const xOffset = Math.abs(centerX - 0.5);
    const yOffset = Math.abs(centerY - 0.5);
    const isCentered = xOffset < DETECTION_CONFIG.CENTER_TOLERANCE &&
        yOffset < DETECTION_CONFIG.CENTER_TOLERANCE;

    // Check if face is close enough (good size)
    const isCloseEnough = size >= DETECTION_CONFIG.MIN_FACE_SIZE &&
        size <= DETECTION_CONFIG.MAX_FACE_SIZE;

    // Check overall quality (centered + right size + good confidence)
    const isGoodQuality = isCentered &&
        isCloseEnough &&
        confidence >= DETECTION_CONFIG.MIN_CONFIDENCE;

    return {
        centerX,
        centerY,
        size,
        isCentered,
        isCloseEnough,
        isGoodQuality
    };
}

/**
 * Get guidance message for user based on face position
 */
export function getFaceGuidanceMessage(position: FacePosition): string {
    if (position.isGoodQuality) {
        return '✅ Perfect! Hold still...';
    }

    if (!position.isCentered) {
        const xOffset = position.centerX - 0.5;
        const yOffset = position.centerY - 0.5;

        let message = '📍 Move ';
        if (Math.abs(xOffset) > Math.abs(yOffset)) {
            message += xOffset > 0 ? 'left' : 'right';
        } else {
            message += yOffset > 0 ? 'up' : 'down';
        }
        return message;
    }

    if (!position.isCloseEnough) {
        if (position.size < DETECTION_CONFIG.MIN_FACE_SIZE) {
            return '🔍 Move closer to camera';
        } else {
            return '↔️ Move away from camera';
        }
    }

    return '⏳ Adjusting...';
}

/**
 * Draw face detection box on canvas
 */
export function drawFaceBox(
    ctx: CanvasRenderingContext2D,
    box: DetectionBox,
    position: FacePosition,
    videoWidth: number,
    videoHeight: number
): void {
    // Clear canvas
    ctx.clearRect(0, 0, videoWidth, videoHeight);

    // Determine box color based on position quality
    let color: string = DETECTION_CONFIG.BOX_COLOR_TOO_FAR;
    if (position.isGoodQuality) {
        color = DETECTION_CONFIG.BOX_COLOR_GOOD;
    } else if (position.isCentered || position.isCloseEnough) {
        color = DETECTION_CONFIG.BOX_COLOR_CENTERING;
    }    // Draw outer box
    ctx.strokeStyle = color;
    ctx.lineWidth = DETECTION_CONFIG.BOX_LINE_WIDTH;
    ctx.strokeRect(box.x, box.y, box.width, box.height);

    // Draw corner accents (more stylish)
    const cornerLength = Math.min(box.width, box.height) * 0.15;
    ctx.lineWidth = DETECTION_CONFIG.BOX_LINE_WIDTH + 1;

    // Top-left corner
    ctx.beginPath();
    ctx.moveTo(box.x, box.y + cornerLength);
    ctx.lineTo(box.x, box.y);
    ctx.lineTo(box.x + cornerLength, box.y);
    ctx.stroke();

    // Top-right corner
    ctx.beginPath();
    ctx.moveTo(box.x + box.width - cornerLength, box.y);
    ctx.lineTo(box.x + box.width, box.y);
    ctx.lineTo(box.x + box.width, box.y + cornerLength);
    ctx.stroke();

    // Bottom-left corner
    ctx.beginPath();
    ctx.moveTo(box.x, box.y + box.height - cornerLength);
    ctx.lineTo(box.x, box.y + box.height);
    ctx.lineTo(box.x + cornerLength, box.y + box.height);
    ctx.stroke();

    // Bottom-right corner
    ctx.beginPath();
    ctx.moveTo(box.x + box.width - cornerLength, box.y + box.height);
    ctx.lineTo(box.x + box.width, box.y + box.height);
    ctx.lineTo(box.x + box.width, box.y + box.height - cornerLength);
    ctx.stroke();

    // Draw center crosshair if face is good quality
    if (position.isGoodQuality) {
        const centerX = box.x + box.width / 2;
        const centerY = box.y + box.height / 2;
        const crossSize = 15;

        ctx.strokeStyle = color;
        ctx.lineWidth = 2;

        // Horizontal line
        ctx.beginPath();
        ctx.moveTo(centerX - crossSize, centerY);
        ctx.lineTo(centerX + crossSize, centerY);
        ctx.stroke();

        // Vertical line
        ctx.beginPath();
        ctx.moveTo(centerX, centerY - crossSize);
        ctx.lineTo(centerX, centerY + crossSize);
        ctx.stroke();
    }

    // Draw size indicator arc
    const centerX = box.x + box.width / 2;
    const centerY = box.y + box.height / 2;
    const radius = Math.max(box.width, box.height) / 2;
    const sizeScore = Math.min(1, position.size / DETECTION_CONFIG.OPTIMAL_FACE_SIZE);

    ctx.strokeStyle = color;
    ctx.lineWidth = 2;
    ctx.globalAlpha = 0.3;
    ctx.beginPath();
    ctx.arc(centerX, centerY, radius * 1.1, 0, 2 * Math.PI * sizeScore);
    ctx.stroke();
    ctx.globalAlpha = 1.0;
}

/**
 * Calculate detection quality score (0-100)
 */
export function calculateQualityScore(position: FacePosition, confidence: number): number {
    // Centering score (0-40 points)
    const xOffset = Math.abs(position.centerX - 0.5) / 0.5;
    const yOffset = Math.abs(position.centerY - 0.5) / 0.5;
    const centeringScore = (1 - (xOffset + yOffset) / 2) * 40;

    // Size score (0-30 points)
    const sizeDeviation = Math.abs(position.size - DETECTION_CONFIG.OPTIMAL_FACE_SIZE);
    const sizeScore = Math.max(0, (1 - sizeDeviation / DETECTION_CONFIG.OPTIMAL_FACE_SIZE) * 30);

    // Confidence score (0-30 points)
    const confidenceScore = confidence * 30;

    return Math.round(centeringScore + sizeScore + confidenceScore);
}

/**
 * Check if enough time has passed since last capture (debouncing)
 */
export function canAutoCapture(lastCaptureTime: number, currentTime: number): boolean {
    return currentTime - lastCaptureTime >= DETECTION_CONFIG.CAPTURE_COOLDOWN;
}

/**
 * Format quality score for display
 */
export function formatQualityScore(score: number): string {
    if (score >= 90) return '🌟 Excellent';
    if (score >= 75) return '✅ Good';
    if (score >= 60) return '👌 Fair';
    return '⚠️ Poor';
}
