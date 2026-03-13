/**
 * Field-level validator factories.
 * Each factory returns a function: (value) => errorMessage | ''
 * Empty string means valid.
 */

export const required = (msg = 'This field is required') => (value) =>
    (!value && value !== 0) || !String(value).trim() ? msg : '';

export const minLen = (n, msg) => (value) => {
    const trimmed = String(value || '').trim();
    if (!trimmed) return ''; // let required() handle empty
    return trimmed.length < n ? (msg || `Must be at least ${n} characters`) : '';
};

export const maxLen = (n, msg) => (value) =>
    String(value || '').trim().length > n
        ? (msg || `Must be at most ${n} characters`)
        : '';

export const emailFormat = (msg = 'Please enter a valid email address') => (value) => {
    const trimmed = String(value || '').trim();
    if (!trimmed) return '';
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(trimmed) ? '' : msg;
};

export const urlFormat = (msg = 'Please enter a valid URL') => (value) => {
    const trimmed = String(value || '').trim();
    if (!trimmed) return '';
    try { new URL(trimmed); return ''; }
    catch { return msg; }
};

export const positiveNumber = (msg = 'Must be greater than 0') => (value) => {
    if (value === '' || value === null || value === undefined) return '';
    const num = Number(value);
    return isNaN(num) || num <= 0 ? msg : '';
};

export const nonNegativeInteger = (msg = 'Must be a whole number (0 or greater)') => (value) => {
    if (value === '' || value === null || value === undefined) return '';
    const num = Number(value);
    return isNaN(num) || num < 0 || !Number.isInteger(num) ? msg : '';
};

export const futureDate = (msg = 'Must be a future date') => (value) => {
    if (!value) return '';
    return new Date(value) <= new Date() ? msg : '';
};

/**
 * Run multiple validators on a single value.
 * Returns the FIRST error message, or '' if all pass.
 */
export const runValidators = (value, ...validators) => {
    for (const v of validators) {
        const err = v(value);
        if (err) return err;
    }
    return '';
};

/**
 * Returns true if an errors object has at least one non-empty error.
 */
export const hasErrors = (errors) =>
    Object.values(errors).some((e) => !!e);
