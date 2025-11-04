/**
 * Logging Utilities
 * Client-side logging with timestamps and log levels (UTC+7)
 */

import { getNow, formatDateTime } from './timezone';

export enum LogLevel {
    DEBUG = 'DEBUG',
    INFO = 'INFO',
    WARN = 'WARN',
    ERROR = 'ERROR'
}

interface LogEntry {
    level: LogLevel;
    message: string;
    timestamp: Date;
    data?: any;
}

class Logger {
    private logs: LogEntry[] = [];
    private maxLogs = 100;

    private log(level: LogLevel, message: string, data?: any) {
        const entry: LogEntry = {
            level,
            message,
            timestamp: getNow(), // Use UTC+7
            data
        };

        this.logs.push(entry);

        // Keep only last maxLogs entries
        if (this.logs.length > this.maxLogs) {
            this.logs.shift();
        }

        // Console output with colors (format with UTC+7)
        const timestamp = formatDateTime(entry.timestamp, 'yyyy-MM-dd HH:mm:ss');
        const prefix = `[${timestamp} ICT] [${level}]`; switch (level) {
            case LogLevel.DEBUG:
                console.debug(prefix, message, data);
                break;
            case LogLevel.INFO:
                console.info(prefix, message, data);
                break;
            case LogLevel.WARN:
                console.warn(prefix, message, data);
                break;
            case LogLevel.ERROR:
                console.error(prefix, message, data);
                break;
        }
    }

    debug(message: string, data?: any) {
        this.log(LogLevel.DEBUG, message, data);
    }

    info(message: string, data?: any) {
        this.log(LogLevel.INFO, message, data);
    }

    warn(message: string, data?: any) {
        this.log(LogLevel.WARN, message, data);
    }

    error(message: string, data?: any) {
        this.log(LogLevel.ERROR, message, data);
    }

    getLogs(): LogEntry[] {
        return [...this.logs];
    }

    clearLogs() {
        this.logs = [];
    }

    exportLogs(): string {
        return JSON.stringify(this.logs, null, 2);
    }
}

export const logger = new Logger();
export default logger;
