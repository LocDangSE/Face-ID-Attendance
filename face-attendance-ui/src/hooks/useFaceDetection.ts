/**
 * useFaceDetection Hook
 * Real-time face detection using BlazeFace with smart auto-capture
 */

import { useRef, useState, useEffect, useCallback } from 'react';
import * as blazeface from '@tensorflow-models/blazeface';
import '@tensorflow/tfjs-core';
import '@tensorflow/tfjs-converter';
import Webcam from 'react-webcam';
import {
    predictionToBox,
    analyzeFacePosition,
    drawFaceBox,
    getFaceGuidanceMessage,
    calculateQualityScore,
    canAutoCapture,
    DETECTION_CONFIG,
    type DetectionBox,
    type FacePosition
} from '@utils/faceDetectionUtils';

interface UseFaceDetectionOptions {
    /** Enable auto-detection */
    enabled: boolean;
    /** Callback when good face detected and ready to capture */
    onFaceReady?: (qualityScore: number) => void;
    /** Callback when face lost */
    onFaceLost?: () => void;
    /** Webcam ref from useWebcam hook */
    webcamRef: React.RefObject<Webcam | null>;
}

interface UseFaceDetectionReturn {
    /** BlazeFace model loading state */
    isModelLoading: boolean;
    /** Detection running state */
    isDetecting: boolean;
    /** Model load error */
    modelError: string | null;
    /** Currently detected faces count */
    facesDetected: number;
    /** Current face position analysis */
    facePosition: FacePosition | null;
    /** Detection box for visual overlay */
    detectionBox: DetectionBox | null;
    /** User guidance message */
    guidanceMessage: string;
    /** Quality score (0-100) */
    qualityScore: number;
    /** Canvas ref for drawing overlay */
    canvasRef: React.RefObject<HTMLCanvasElement | null>;
    /** Whether face is ready for auto-capture */
    isFaceReady: boolean;
    /** Manually start detection */
    startDetection: () => void;
    /** Manually stop detection */
    stopDetection: () => void;
}

/**
 * Custom hook for real-time face detection with BlazeFace
 * 
 * Features:
 * - Automatic face detection at 10 FPS
 * - Face positioning guidance (center, distance)
 * - Quality scoring
 * - Visual feedback with bounding boxes
 * - Smart auto-capture triggering
 * - Debouncing to prevent duplicate captures
 * 
 * @example
 * const { 
 *   isFaceReady, 
 *   guidanceMessage, 
 *   canvasRef 
 * } = useFaceDetection({
 *   enabled: autoMode,
 *   onFaceReady: handleAutoCapture,
 *   webcamRef
 * });
 */
export function useFaceDetection(options: UseFaceDetectionOptions): UseFaceDetectionReturn {
    const { enabled, onFaceReady, onFaceLost, webcamRef } = options;

    // Model state
    const [isModelLoading, setIsModelLoading] = useState(false);
    const [modelError, setModelError] = useState<string | null>(null);
    const modelRef = useRef<blazeface.BlazeFaceModel | null>(null);

    // Detection state
    const [isDetecting, setIsDetecting] = useState(false);
    const [facesDetected, setFacesDetected] = useState(0);
    const [facePosition, setFacePosition] = useState<FacePosition | null>(null);
    const [detectionBox, setDetectionBox] = useState<DetectionBox | null>(null);
    const [guidanceMessage, setGuidanceMessage] = useState('👤 Looking for faces...');
    const [qualityScore, setQualityScore] = useState(0);
    const [isFaceReady, setIsFaceReady] = useState(false);

    // Refs
    const canvasRef = useRef<HTMLCanvasElement>(null);
    const detectionIntervalRef = useRef<NodeJS.Timeout | null>(null);
    const lastCaptureTimeRef = useRef(0);
    const faceStableTimerRef = useRef<NodeJS.Timeout | null>(null);
    const lastFaceDetectedRef = useRef(false);

    /**
     * Load BlazeFace model
     */
    const loadModel = useCallback(async () => {
        if (modelRef.current) {
            console.log('[FaceDetection] Model already loaded');
            return;
        }

        setIsModelLoading(true);
        setModelError(null);

        try {
            console.log('[FaceDetection] Loading BlazeFace model...');
            const model = await blazeface.load();
            modelRef.current = model;
            console.log('[FaceDetection] ✅ BlazeFace model loaded successfully');
        } catch (error) {
            const errorMessage = error instanceof Error ? error.message : 'Failed to load model';
            setModelError(errorMessage);
            console.error('[FaceDetection] ❌ Model loading error:', error);
        } finally {
            setIsModelLoading(false);
        }
    }, []);

    /**
     * Run face detection on current video frame
     */
    const detectFaces = useCallback(async () => {
        if (!modelRef.current || !webcamRef.current || !canvasRef.current) {
            return;
        }

        const video = webcamRef.current.video;
        if (!video || video.readyState !== 4) {
            return;
        }

        try {
            // Run BlazeFace detection
            const predictions = await modelRef.current.estimateFaces(video, false);

            // Update detection count
            setFacesDetected(predictions.length);

            // Handle no face detected
            if (predictions.length === 0) {
                setDetectionBox(null);
                setFacePosition(null);
                setGuidanceMessage('👤 No face detected');
                setQualityScore(0);
                setIsFaceReady(false);

                // Trigger face lost callback
                if (lastFaceDetectedRef.current && onFaceLost) {
                    onFaceLost();
                    lastFaceDetectedRef.current = false;
                }

                // Clear canvas
                const ctx = canvasRef.current.getContext('2d');
                if (ctx) {
                    ctx.clearRect(0, 0, canvasRef.current.width, canvasRef.current.height);
                }

                return;
            }

            // Process the first detected face (closest/largest)
            const prediction = predictions[0];
            const probabilityValue = prediction.probability;
            const confidence = probabilityValue
                ? (Array.isArray(probabilityValue) ? probabilityValue[0] : probabilityValue)
                : 0;

            // Convert prediction to box coordinates
            const box = predictionToBox(prediction, video.videoWidth, video.videoHeight);

            // Analyze face position and quality
            const position = analyzeFacePosition(box, video.videoWidth, video.videoHeight, confidence);

            // Calculate quality score
            const score = calculateQualityScore(position, confidence);

            // Update state
            setDetectionBox(box);
            setFacePosition(position);
            setQualityScore(score);
            setGuidanceMessage(getFaceGuidanceMessage(position));

            // Draw visual feedback on canvas
            const ctx = canvasRef.current.getContext('2d');
            if (ctx) {
                // Ensure canvas matches video dimensions
                if (canvasRef.current.width !== video.videoWidth) {
                    canvasRef.current.width = video.videoWidth;
                }
                if (canvasRef.current.height !== video.videoHeight) {
                    canvasRef.current.height = video.videoHeight;
                }

                drawFaceBox(ctx, box, position, video.videoWidth, video.videoHeight);
            }

            // Check if face is ready for capture
            const readyForCapture = position.isGoodQuality &&
                score >= 75 &&
                canAutoCapture(lastCaptureTimeRef.current, Date.now());

            // Manage face ready state with stability timer
            if (readyForCapture) {
                if (!faceStableTimerRef.current) {
                    // Start stability timer
                    faceStableTimerRef.current = setTimeout(() => {
                        setIsFaceReady(true);

                        // Trigger auto-capture callback
                        if (onFaceReady) {
                            onFaceReady(score);
                            lastCaptureTimeRef.current = Date.now();
                        }

                        faceStableTimerRef.current = null;
                    }, DETECTION_CONFIG.FACE_STABLE_TIME);
                }
            } else {
                // Cancel stability timer if face moves
                if (faceStableTimerRef.current) {
                    clearTimeout(faceStableTimerRef.current);
                    faceStableTimerRef.current = null;
                }
                setIsFaceReady(false);
            }

            lastFaceDetectedRef.current = true;

        } catch (error) {
            console.error('[FaceDetection] Detection error:', error);
        }
    }, [webcamRef, onFaceReady, onFaceLost]);

    /**
     * Start detection loop
     */
    const startDetection = useCallback(() => {
        if (detectionIntervalRef.current || !modelRef.current) {
            return;
        }

        console.log('[FaceDetection] Starting detection loop');
        setIsDetecting(true);

        // Run detection at configured interval (10 FPS)
        detectionIntervalRef.current = setInterval(() => {
            detectFaces();
        }, DETECTION_CONFIG.DETECTION_INTERVAL);

        // Run first detection immediately
        detectFaces();
    }, [detectFaces]);

    /**
     * Stop detection loop
     */
    const stopDetection = useCallback(() => {
        console.log('[FaceDetection] Stopping detection loop');

        if (detectionIntervalRef.current) {
            clearInterval(detectionIntervalRef.current);
            detectionIntervalRef.current = null;
        }

        if (faceStableTimerRef.current) {
            clearTimeout(faceStableTimerRef.current);
            faceStableTimerRef.current = null;
        }

        setIsDetecting(false);
        setFacesDetected(0);
        setDetectionBox(null);
        setFacePosition(null);
        setGuidanceMessage('👤 Detection paused');
        setQualityScore(0);
        setIsFaceReady(false);

        // Clear canvas
        if (canvasRef.current) {
            const ctx = canvasRef.current.getContext('2d');
            if (ctx) {
                ctx.clearRect(0, 0, canvasRef.current.width, canvasRef.current.height);
            }
        }
    }, []);

    /**
     * Load model on mount
     */
    useEffect(() => {
        loadModel();

        return () => {
            stopDetection();
        };
    }, [loadModel, stopDetection]);

    /**
     * Handle enabled state changes
     */
    useEffect(() => {
        if (enabled && modelRef.current && !isDetecting) {
            startDetection();
        } else if (!enabled && isDetecting) {
            stopDetection();
        }
    }, [enabled, isDetecting, startDetection, stopDetection]);

    return {
        isModelLoading,
        isDetecting,
        modelError,
        facesDetected,
        facePosition,
        detectionBox,
        guidanceMessage,
        qualityScore,
        canvasRef,
        isFaceReady,
        startDetection,
        stopDetection
    };
}

// Export types
export type { UseFaceDetectionOptions, UseFaceDetectionReturn };
