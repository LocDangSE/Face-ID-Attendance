/**
 * useApi Hook
 * Generic hook for API calls with loading/error states
 */

import { useState, useCallback } from 'react';
import { getErrorMessage } from '@api/client';

interface UseApiState<T> {
    data: T | null;
    loading: boolean;
    error: string | null;
}

interface UseApiReturn<T, P extends any[]> extends UseApiState<T> {
    execute: (...args: P) => Promise<T>;
    reset: () => void;
}

/**
 * Generic hook for API calls with automatic state management
 * @param apiFunction - The API function to wrap
 * @returns Object with data, loading, error states and execute function
 * 
 * @example
 * const { data, loading, error, execute } = useApi(studentApi.getAll);
 * await execute();
 */
export function useApi<T, P extends any[]>(
    apiFunction: (...args: P) => Promise<T>
): UseApiReturn<T, P> {
    const [state, setState] = useState<UseApiState<T>>({
        data: null,
        loading: false,
        error: null
    });

    const execute = useCallback(
        async (...args: P): Promise<T> => {
            setState({ data: null, loading: true, error: null });

            try {
                const result = await apiFunction(...args);
                setState({ data: result, loading: false, error: null });
                return result;
            } catch (error) {
                const errorMessage = getErrorMessage(error);
                setState({ data: null, loading: false, error: errorMessage });
                throw error;
            }
        },
        [apiFunction]
    );

    const reset = useCallback(() => {
        setState({ data: null, loading: false, error: null });
    }, []);

    return {
        ...state,
        execute,
        reset
    };
}
