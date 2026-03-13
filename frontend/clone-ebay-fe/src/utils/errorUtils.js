/**
 * Shared error-handling utilities for the entire project.
 *
 * All API calls go through the axios instance (src/api/axios.js) which already
 * wraps errors into ApiError { message, code, correlationId, status }.
 * These helpers normalise that into UI-friendly objects.
 */

/**
 * Parse any caught error into a consistent object.
 *
 * Handles:
 *  - ApiError  (thrown by our axios interceptor)
 *  - Raw Axios errors with response.data
 *  - ASP.NET ProblemDetails { title, detail, status, traceId, errors }
 *  - Plain Error objects
 *  - Network / timeout errors
 *
 * @param {Error} error  – the error from try/catch
 * @param {string} fallback – fallback message when nothing in the error is useful
 * @returns {{ message: string, code: string|null, correlationId: string|null, status: number|null }}
 */
export function parseApiError(error, fallback = 'Something went wrong. Please try again.') {
    if (!error) {
        return { message: fallback, code: null, correlationId: null, status: null };
    }

    // ──────────────────────────────────────────
    // 1. ApiError from our axios interceptor
    //    (this should handle 90%+ of cases since the interceptor now
    //     extracts backend messages for all status codes)
    // ──────────────────────────────────────────
    if (error.name === 'ApiError') {
        return {
            message: error.message || fallback,
            code: error.code || null,
            correlationId: error.correlationId || null,
            status: error.status || null,
        };
    }

    // ──────────────────────────────────────────
    // 2. Raw Axios error (if interceptor didn't catch it)
    // ──────────────────────────────────────────
    const rd = error.response?.data;
    if (rd && typeof rd === 'object') {
        // 2a. Custom wrapper { success, message, code }
        if (rd.success === false) {
            return {
                message: rd.message || fallback,
                code: rd.code || null,
                correlationId: rd.correlationId || null,
                status: error.response?.status || null,
            };
        }

        // 2b. ASP.NET ProblemDetails { title, detail, status, traceId, errors }
        if (rd.title || rd.detail) {
            let message = rd.detail || rd.title;

            // Aggregate validation errors from ProblemDetails
            if (rd.errors && typeof rd.errors === 'object') {
                const fieldErrors = Object.entries(rd.errors)
                    .map(([field, msgs]) => {
                        const list = Array.isArray(msgs) ? msgs.join(', ') : String(msgs);
                        return `${field}: ${list}`;
                    })
                    .join('; ');
                if (fieldErrors) {
                    message = message ? `${message} — ${fieldErrors}` : fieldErrors;
                }
            }

            return {
                message: message || fallback,
                code: rd.type || null,
                correlationId: rd.traceId || null,
                status: rd.status || error.response?.status || null,
            };
        }

        // 2c. Simple { message } or { error }
        const message =
            rd.message ||
            rd.error ||
            error.message ||
            fallback;

        return {
            message,
            code: rd.code || null,
            correlationId: rd.correlationId || rd.traceId || null,
            status: error.response?.status || null,
        };
    }

    // ──────────────────────────────────────────
    // 3. Network / timeout / unknown
    // ──────────────────────────────────────────
    if (error.code === 'ERR_NETWORK' || error.message === 'Network Error') {
        return {
            message: 'Unable to connect to the server. Please check your connection and try again.',
            code: 'NETWORK_ERROR',
            correlationId: null,
            status: null,
        };
    }

    return {
        message: error.message || fallback,
        code: null,
        correlationId: null,
        status: error.response?.status || error.status || null,
    };
}

/**
 * Quick shorthand when you only need the message string.
 */
export function getErrorMessage(error, fallback) {
    return parseApiError(error, fallback).message;
}

/**
 * Format an error object (from parseApiError) into a single readable string
 * including code and correlationId when available.
 */
export function formatErrorDisplay(parsed) {
    let text = parsed.message;
    if (parsed.code) text += ` [${parsed.code}]`;
    return text;
}
