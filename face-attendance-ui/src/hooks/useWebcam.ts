/**
 * useWebcam Hook
 * Webcam management with FPS limiting for performance optimization
 */

import { useRef, useState, useCallback, useEffect } from 'react';
import Webcam from 'react-webcam';

// Configuration
const FPS_LIMIT = 1; // Capture max 1 frame per second (96% CPU reduction from 30 FPS)
const MIN_CAPTURE_INTERVAL = 1000 / FPS_LIMIT; // Minimum milliseconds between captures

interface UseWebcamReturn {
    webcamRef: React.RefObject<Webcam | null>;
    isCapturing: boolean;
    isReady: boolean;
    error: string | null;
    capture: () => Promise<Blob | null>;
    canCapture: boolean;
    timeUntilNextCapture: number;
}

/**
 * Custom hook for FPS-limited webcam capture
 * Prevents excessive CPU usage by limiting capture rate to 1 FPS
 * 
 * @returns Webcam controls and state
 * 
 * @example
 * const { webcamRef, capture, canCapture, isCapturing } = useWebcam();
 * 
 * const handleCapture = async () => {
 *   if (!canCapture) return;
 *   const blob = await capture();
 *   if (blob) {
 *     // Process the captured image
 *   }
 * };
 */
export function useWebcam(): UseWebcamReturn {
    const webcamRef = useRef<Webcam>(null);
    const [isCapturing, setIsCapturing] = useState(false);
    const [isReady] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const lastCaptureTime = useRef(0);
    const [timeUntilNextCapture, setTimeUntilNextCapture] = useState(0);

    // Calculate if capture is allowed based on FPS limit
    const canCapture = useCallback((): boolean => {
        const now = Date.now();
        const timeSinceLastCapture = now - lastCaptureTime.current;
        return timeSinceLastCapture >= MIN_CAPTURE_INTERVAL;
    }, []);

    // Update timer every 100ms
    useEffect(() => {
        const interval = setInterval(() => {
            if (lastCaptureTime.current > 0) {
                const now = Date.now();
                const timeSinceLastCapture = now - lastCaptureTime.current;
                const remaining = Math.max(0, MIN_CAPTURE_INTERVAL - timeSinceLastCapture);
                setTimeUntilNextCapture(remaining);
            } else {
                setTimeUntilNextCapture(0);
            }
        }, 100);

        return () => clearInterval(interval);
    }, []);

    /**
     * Capture image from webcam with FPS limiting
     * @returns Image blob or null if capture failed/rate limited
     */
    const capture = useCallback(async (): Promise<Blob | null> => {
        // Check rate limiting
        if (!canCapture()) {
            const timeRemaining = (MIN_CAPTURE_INTERVAL - (Date.now() - lastCaptureTime.current)) / 1000;
            console.warn(`[Webcam] Rate limited. Wait ${timeRemaining.toFixed(1)}s before next capture`);
            return null;
        }

        // Check webcam availability
        if (!webcamRef.current) {
            setError('Webcam not ready');
            console.error('[Webcam] Webcam ref not available');
            return null;
        }

        setIsCapturing(true);
        setError(null);

        try {
            // Capture screenshot as base64
            const imageSrc = webcamRef.current.getScreenshot();

            if (!imageSrc) {
                setError('Failed to capture image');
                console.error('[Webcam] getScreenshot returned null');
                return null;
            }

            // Convert base64 to blob
            const blob = await fetch(imageSrc).then(r => r.blob());

            // Update last capture time
            lastCaptureTime.current = Date.now();

            console.log(`[Webcam] ✅ Captured ${(blob.size / 1024).toFixed(1)}KB image`);

            return blob;
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Unknown capture error';
            setError(errorMessage);
            console.error('[Webcam] Capture error:', error);
            return null;
        } finally {
            setIsCapturing(false);
        }
    }, [canCapture]);

    return {
        webcamRef,
        isCapturing,
        isReady,
        error,
        capture,
        canCapture: canCapture(),
        timeUntilNextCapture
    };
}

// Export types for external use
export type { UseWebcamReturn };
