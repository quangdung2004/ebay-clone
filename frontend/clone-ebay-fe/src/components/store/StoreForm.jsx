import { useState, useEffect } from 'react';
import { parseApiError } from '../../utils/errorUtils';
import { validateStore, hasErrors } from '../../utils/formValidation';
import ErrorAlert from '../ui/ErrorAlert';
import FormFieldError from '../ui/FormFieldError';
import './StoreForm.css';

/**
 * Reusable form for creating / editing a store.
 *
 * Props:
 *   initialData  – { storeName, description, bannerImageURL }
 *   onSubmit     – async (payload) => void (should throw on failure)
 *   submitLabel  – button text (default "Save")
 *   loading      – external loading flag (optional)
 */
const StoreForm = ({ initialData = {}, onSubmit, submitLabel = 'Save', loading: externalLoading }) => {
    const [storeName, setStoreName] = useState(initialData.storeName || '');
    const [description, setDescription] = useState(initialData.description || '');
    const [bannerImageURL, setBannerImageURL] = useState(initialData.bannerImageURL || '');
    const [fieldErrors, setFieldErrors] = useState({});
    const [apiError, setApiError] = useState(null);
    const [submitting, setSubmitting] = useState(false);

    // Sync when initialData changes (e.g. after fetch)
    useEffect(() => {
        if (initialData.storeName !== undefined) setStoreName(initialData.storeName || '');
        if (initialData.description !== undefined) setDescription(initialData.description || '');
        if (initialData.bannerImageURL !== undefined) setBannerImageURL(initialData.bannerImageURL || '');
    }, [initialData.storeName, initialData.description, initialData.bannerImageURL]);

    const isLoading = submitting || externalLoading;

    const getValues = () => ({ storeName, description, bannerImageURL });

    const clearFieldError = (field) => {
        if (fieldErrors[field]) {
            setFieldErrors((prev) => ({ ...prev, [field]: '' }));
        }
    };

    const handleBlur = (field) => {
        const errors = validateStore(getValues());
        setFieldErrors((prev) => ({ ...prev, [field]: errors[field] || '' }));
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setApiError(null);

        const errors = validateStore(getValues());
        setFieldErrors(errors);
        if (hasErrors(errors)) {
            const first = Object.keys(errors).find((k) => errors[k]);
            const idMap = { storeName: 'store-name', description: 'store-desc', bannerImageURL: 'store-banner' };
            if (first && idMap[first]) document.getElementById(idMap[first])?.focus();
            return;
        }

        setSubmitting(true);
        try {
            await onSubmit({
                storeName: storeName.trim(),
                description: description.trim() || null,
                bannerImageURL: bannerImageURL.trim() || null,
            });
        } catch (err) {
            setApiError(parseApiError(err, 'Something went wrong'));
        } finally {
            setSubmitting(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="store-form" noValidate>
            <ErrorAlert error={apiError} />

            <div className="form-group">
                <label htmlFor="store-name" className="form-label">Store Name *</label>
                <input
                    id="store-name"
                    type="text"
                    value={storeName}
                    onChange={(e) => { setStoreName(e.target.value); clearFieldError('storeName'); }}
                    onBlur={() => handleBlur('storeName')}
                    className={`form-input${fieldErrors.storeName ? ' form-input--error' : ''}`}
                    placeholder="Enter your store name"
                    disabled={isLoading}
                />
                <FormFieldError error={fieldErrors.storeName} />
            </div>

            <div className="form-group">
                <label htmlFor="store-desc" className="form-label">Description</label>
                <textarea
                    id="store-desc"
                    value={description}
                    onChange={(e) => { setDescription(e.target.value); clearFieldError('description'); }}
                    onBlur={() => handleBlur('description')}
                    className={`form-input form-textarea${fieldErrors.description ? ' form-input--error' : ''}`}
                    placeholder="Describe your store…"
                    rows={4}
                    disabled={isLoading}
                />
                <FormFieldError error={fieldErrors.description} />
            </div>

            <div className="form-group">
                <label htmlFor="store-banner" className="form-label">Banner Image URL</label>
                <input
                    id="store-banner"
                    type="text"
                    value={bannerImageURL}
                    onChange={(e) => { setBannerImageURL(e.target.value); clearFieldError('bannerImageURL'); }}
                    onBlur={() => handleBlur('bannerImageURL')}
                    className={`form-input${fieldErrors.bannerImageURL ? ' form-input--error' : ''}`}
                    placeholder="https://example.com/banner.jpg"
                    disabled={isLoading}
                />
                <FormFieldError error={fieldErrors.bannerImageURL} />
                {bannerImageURL.trim() && !fieldErrors.bannerImageURL ? (
                    <div className="banner-preview">
                        <img
                            src={bannerImageURL}
                            alt="Store banner preview"
                            onError={(e) => { e.target.style.display = 'none'; }}
                        />
                    </div>
                ) : (
                    <div className="banner-preview">
                        <div className="banner-preview-empty">No banner image</div>
                    </div>
                )}
            </div>

            <button type="submit" className="submit-btn" disabled={isLoading}>
                {isLoading ? 'Saving…' : submitLabel}
            </button>
        </form>
    );
};

export default StoreForm;
