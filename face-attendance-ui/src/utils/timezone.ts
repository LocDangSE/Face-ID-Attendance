/**
 * Timezone Utilities
 * Convert UTC to UTC+7 (Indochina Time / Bangkok Time)
 */

import { format, parseISO, formatDistance, isValid } from 'date-fns';
import { toZonedTime, fromZonedTime } from 'date-fns-tz';

// Timezone configuration
const TIMEZONE = 'Asia/Bangkok'; // UTC+7
const TIMEZONE_OFFSET_HOURS = 7;

/**
 * Get current date/time in UTC+7
 */
export const getNow = (): Date => {
    return toZonedTime(new Date(), TIMEZONE);
};

/**
 * Convert UTC date to UTC+7
 */
export const toLocalTime = (date: string | Date): Date => {
    const dateObj = typeof date === 'string' ? parseISO(date) : date;
    return toZonedTime(dateObj, TIMEZONE);
};

/**
 * Convert UTC+7 to UTC for API calls
 */
export const toUTC = (date: Date): Date => {
    return fromZonedTime(date, TIMEZONE);
};

/**
 * Format date string with UTC+7 timezone
 */
export const formatDate = (date: string | Date, formatStr: string = 'MMM dd, yyyy'): string => {
    try {
        const dateObj = typeof date === 'string' ? parseISO(date) : date;
        if (!isValid(dateObj)) {
            return 'Invalid date';
        }
        const localDate = toLocalTime(dateObj);
        return format(localDate, formatStr);
    } catch (error) {
        console.error('Date formatting error:', error);
        return 'Invalid date';
    }
};

/**
 * Format time string with UTC+7 timezone
 */
export const formatTime = (date: string | Date, formatStr: string = 'hh:mm:ss a'): string => {
    try {
        const dateObj = typeof date === 'string' ? parseISO(date) : date;
        if (!isValid(dateObj)) {
            return 'Invalid time';
        }
        const localDate = toLocalTime(dateObj);
        return format(localDate, formatStr);
    } catch (error) {
        console.error('Time formatting error:', error);
        return 'Invalid time';
    }
};

/**
 * Format datetime string with UTC+7 timezone
 */
export const formatDateTime = (date: string | Date, formatStr: string = 'MMM dd, yyyy hh:mm a'): string => {
    try {
        const dateObj = typeof date === 'string' ? parseISO(date) : date;
        if (!isValid(dateObj)) {
            return 'Invalid datetime';
        }
        const localDate = toLocalTime(dateObj);
        return format(localDate, formatStr);
    } catch (error) {
        console.error('DateTime formatting error:', error);
        return 'Invalid datetime';
    }
};

/**
 * Format relative time (e.g., "2 hours ago") with UTC+7
 */
export const formatRelativeTime = (date: string | Date): string => {
    try {
        const dateObj = typeof date === 'string' ? parseISO(date) : date;
        if (!isValid(dateObj)) {
            return 'Invalid date';
        }
        const localDate = toLocalTime(dateObj);
        const now = getNow();
        return formatDistance(localDate, now, { addSuffix: true });
    } catch (error) {
        console.error('Relative time formatting error:', error);
        return 'Invalid date';
    }
};

/**
 * Format percentage
 */
export const formatPercent = (value: number, decimals: number = 1): string => {
    return `${(value * 100).toFixed(decimals)}%`;
};

/**
 * Format file size
 */
export const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';

    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));

    return `${parseFloat((bytes / Math.pow(k, i)).toFixed(2))} ${sizes[i]}`;
};

/**
 * Format student name
 */
export const formatStudentName = (firstName: string, lastName: string): string => {
    return `${firstName} ${lastName}`.trim();
};

/**
 * Format confidence score
 */
export const formatConfidence = (score: number): string => {
    return `${(score * 100).toFixed(1)}%`;
};

/**
 * Truncate text
 */
export const truncateText = (text: string, maxLength: number): string => {
    if (text.length <= maxLength) return text;
    return `${text.substring(0, maxLength)}...`;
};

/**
 * Format phone number (US format)
 */
export const formatPhoneNumber = (phone: string): string => {
    const cleaned = phone.replace(/\D/g, '');
    const match = cleaned.match(/^(\d{3})(\d{3})(\d{4})$/);
    if (match) {
        return `(${match[1]}) ${match[2]}-${match[3]}`;
    }
    return phone;
};

/**
 * Capitalize first letter
 */
export const capitalizeFirst = (text: string): string => {
    return text.charAt(0).toUpperCase() + text.slice(1).toLowerCase();
};

/**
 * Format duration (milliseconds to human readable)
 */
export const formatDuration = (ms: number): string => {
    const seconds = Math.floor(ms / 1000);
    const minutes = Math.floor(seconds / 60);
    const hours = Math.floor(minutes / 60);

    if (hours > 0) {
        return `${hours}h ${minutes % 60}m`;
    } else if (minutes > 0) {
        return `${minutes}m ${seconds % 60}s`;
    } else {
        return `${seconds}s`;
    }
};

// Export timezone info
export const TIMEZONE_INFO = {
    name: TIMEZONE,
    offset: TIMEZONE_OFFSET_HOURS,
    displayName: 'Indochina Time (ICT)',
    abbreviation: 'ICT'
};
