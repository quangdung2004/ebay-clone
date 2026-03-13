/**
 * Form-specific validators.
 * Each returns an errors object: { fieldName: errorMessage | '' }
 */
import {
    required, minLen, maxLen, emailFormat, urlFormat,
    positiveNumber, nonNegativeInteger, futureDate,
    runValidators, hasErrors,
} from './validators';

export { hasErrors };

// ─── Auth ──────────────────────────────────────────

export function validateLogin(values) {
    return {
        email: runValidators(values.email,
            required('Email is required'),
            emailFormat(),
        ),
        password: runValidators(values.password,
            required('Password is required'),
        ),
    };
}

export function validateRegister(values) {
    return {
        username: runValidators(values.username,
            required('Username is required'),
            minLen(3, 'Username must be at least 3 characters'),
            maxLen(50, 'Username must be at most 50 characters'),
        ),
        email: runValidators(values.email,
            required('Email is required'),
            emailFormat(),
            maxLen(255, 'Email is too long'),
        ),
        password: runValidators(values.password,
            required('Password is required'),
            minLen(6, 'Password must be at least 6 characters'),
        ),
        confirmPassword: !values.confirmPassword
            ? 'Please confirm your password'
            : values.password !== values.confirmPassword
                ? 'Passwords do not match'
                : '',
    };
}

export function validateForgotPassword(values) {
    return {
        email: runValidators(values.email,
            required('Email is required'),
            emailFormat(),
        ),
    };
}

export function validateResetPassword(values) {
    return {
        newPassword: runValidators(values.newPassword,
            required('New password is required'),
            minLen(6, 'Password must be at least 6 characters'),
        ),
        confirmPassword: !values.confirmPassword
            ? 'Please confirm your password'
            : values.newPassword !== values.confirmPassword
                ? 'Passwords do not match'
                : '',
    };
}

// ─── Product ──────────────────────────────────────

export function validateProduct(values) {
    const errors = {
        title: runValidators(values.title,
            required('Title is required'),
            minLen(3, 'Title must be at least 3 characters'),
            maxLen(150, 'Title must be at most 150 characters'),
        ),
        description: runValidators(values.description,
            maxLen(2000, 'Description is too long (max 2000 characters)'),
        ),
        price: runValidators(values.price,
            required('Price is required'),
            positiveNumber('Price must be greater than 0'),
        ),
        categoryId: !values.categoryId ? 'Please select a category' : '',
        quantity: runValidators(values.quantity,
            required('Quantity is required'),
            nonNegativeInteger('Quantity must be a whole number (0 or greater)'),
        ),
        condition: !values.condition ? 'Please select a condition' : '',
    };

    if (values.isAuction) {
        errors.auctionEndTime = runValidators(values.auctionEndTime,
            required('Auction end time is required for auction listings'),
            futureDate('Auction end time must be in the future'),
        );
    }

    return errors;
}

export function validateInventory(values) {
    return {
        quantity: runValidators(values.quantity,
            required('Quantity is required'),
            nonNegativeInteger('Quantity must be a whole number (0 or greater)'),
        ),
    };
}

// ─── Store ──────────────────────────────────────

export function validateStore(values) {
    return {
        storeName: runValidators(values.storeName,
            required('Store name is required'),
            minLen(3, 'Store name must be at least 3 characters'),
            maxLen(100, 'Store name must be at most 100 characters'),
        ),
        description: runValidators(values.description,
            maxLen(1000, 'Description is too long (max 1000 characters)'),
        ),
        bannerImageURL: runValidators(values.bannerImageURL,
            urlFormat('Please enter a valid URL'),
            maxLen(500, 'URL is too long (max 500 characters)'),
        ),
    };
}
