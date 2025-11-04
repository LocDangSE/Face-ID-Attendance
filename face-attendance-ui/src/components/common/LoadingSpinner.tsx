/**
 * LoadingSpinner Component
 * Reusable loading indicator with optional message
 */

import React from 'react';

interface LoadingSpinnerProps {
    size?: 'sm' | 'md' | 'lg';
    message?: string;
    className?: string;
}

export const LoadingSpinner: React.FC<LoadingSpinnerProps> = ({
    size = 'md',
    message,
    className = ''
}) => {
    const sizeClasses = {
        sm: 'h-4 w-4',
        md: 'h-8 w-8',
        lg: 'h-12 w-12'
    };

    return (
        <div className={`flex flex-col items-center justify-center gap-3 ${className}`}>
            <div
                className={`animate-spin rounded-full border-b-2 border-blue-600 ${sizeClasses[size]}`}
                role="status"
                aria-label="Loading"
            />
            {message && (
                <p className="text-sm text-gray-600 font-medium">{message}</p>
            )}
        </div>
    );
};

export default LoadingSpinner;
