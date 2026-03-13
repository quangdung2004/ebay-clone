import { useState, useEffect } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { createProduct, uploadProductImages } from '../api/productApi';
import { getMyStore } from '../api/storeApi';
import { parseApiError } from '../utils/errorUtils';
import { validateProduct, hasErrors } from '../utils/formValidation';
import { useToast } from '../components/Toast';
import CategorySelect from '../components/CategorySelect';
import ImageUploader from '../components/ImageUploader';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ErrorAlert from '../components/ui/ErrorAlert';
import FormFieldError from '../components/ui/FormFieldError';
import './CreateProductPage.css';

const CONDITION_OPTIONS = [
    { value: '', label: 'Select Condition' },
    { value: 'NEW', label: 'New' },
    { value: 'USED', label: 'Used' },
    { value: 'REFURBISHED', label: 'Refurbished' },
    { value: 'OPEN_BOX', label: 'Open Box' },
    { value: 'FOR_PARTS', label: 'For Parts' },
];

const CreateProductPage = () => {
    const navigate = useNavigate();
    const { showSuccess } = useToast();

    // ---------- Store guard ----------
    const [storeChecking, setStoreChecking] = useState(true);
    const [hasStore, setHasStore] = useState(false);

    useEffect(() => {
        let cancelled = false;
        const check = async () => {
            try {
                const store = await getMyStore();
                if (!cancelled && store) setHasStore(true);
            } catch (err) {
                if (err.code !== 'STORE_NOT_FOUND' && err.status !== 404) {
                    console.error('Store check error:', err);
                }
            } finally {
                if (!cancelled) setStoreChecking(false);
            }
        };
        check();
        return () => { cancelled = true; };
    }, []);

    // ---------- Form state ----------
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [isAuction, setIsAuction] = useState(false);
    const [auctionEndTime, setAuctionEndTime] = useState('');
    const [quantity, setQuantity] = useState('1');
    const [condition, setCondition] = useState('');
    const [imageFiles, setImageFiles] = useState([]);

    const [fieldErrors, setFieldErrors] = useState({});
    const [apiError, setApiError] = useState(null);
    const [loading, setLoading] = useState(false);

    const getValues = () => ({
        title, description, price, categoryId, isAuction, auctionEndTime, quantity, condition
    });

    const clearFieldError = (field) => {
        if (fieldErrors[field]) {
            setFieldErrors((prev) => ({ ...prev, [field]: '' }));
        }
    };

    const handleBlur = (field) => {
        const errors = validateProduct(getValues());
        setFieldErrors((prev) => ({ ...prev, [field]: errors[field] || '' }));
    };

    // Map field names to input IDs for focus
    const fieldIdMap = {
        title: 'cp-title',
        description: 'cp-desc',
        price: 'cp-price',
        categoryId: 'cp-category',
        quantity: 'cp-quantity',
        auctionEndTime: 'cp-auction-end',
        condition: 'cp-condition',
    };

    const handleSubmit = async (e) => {
        e.preventDefault();
        setApiError(null);

        const values = getValues();
        const errors = validateProduct(values);
        setFieldErrors(errors);
        if (hasErrors(errors)) {
            const first = Object.keys(errors).find((k) => errors[k]);
            if (first && fieldIdMap[first]) document.getElementById(fieldIdMap[first])?.focus();
            return;
        }

        setLoading(true);
        try {
            const body = {
                title: title.trim(),
                description: description.trim() || undefined,
                price: Number(price),
                categoryId: categoryId ? Number(categoryId) : undefined,
                isAuction,
                auctionEndTime: isAuction && auctionEndTime ? new Date(auctionEndTime).toISOString() : undefined,
                quantity: isAuction ? 1 : (parseInt(quantity, 10) || 1),
                condition: condition,
            };

            const product = await createProduct(body);

            if (imageFiles.length > 0 && product?.id) {
                try {
                    await uploadProductImages(product.id, imageFiles);
                    showSuccess('Product created with images!');
                } catch {
                    showSuccess('Product created! But image upload failed — you can upload later.');
                }
            } else {
                showSuccess('Product created!');
            }

            navigate(`/products/${product.id}`);
        } catch (err) {
            setApiError(parseApiError(err, 'Failed to create product'));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="create-product-page">
            <div className="cpp-card">
                {/* ---------- Store guard ---------- */}
                {storeChecking ? (
                    <LoadingSpinner size="md" message="Checking store status…" overlay />
                ) : !hasStore ? (
                    <div style={{ textAlign: 'center', padding: '2.5rem 1rem' }}>
                        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', marginBottom: '0.5rem' }}>
                            Store Required
                        </h2>
                        <p style={{ color: '#6b7280', marginBottom: '1.25rem' }}>
                            You need to create a store before selling products.
                        </p>
                        <Link
                            to="/stores/create"
                            style={{
                                display: 'inline-block', padding: '0.625rem 1.5rem',
                                backgroundColor: '#2563eb', color: '#fff', textDecoration: 'none',
                                borderRadius: '0.5rem', fontWeight: 600,
                            }}
                        >
                            Create Store
                        </Link>
                    </div>
                ) : (
                    <>
                        <h1 className="cpp-title">List a New Product</h1>
                        <p className="cpp-subtitle">Fill in the details to create your listing</p>

                        <form onSubmit={handleSubmit} className="cpp-form" noValidate>
                            <ErrorAlert error={apiError} />

                            <div className="form-group">
                                <label htmlFor="cp-title" className="form-label">Title *</label>
                                <input
                                    id="cp-title"
                                    type="text"
                                    value={title}
                                    onChange={(e) => { setTitle(e.target.value); clearFieldError('title'); }}
                                    onBlur={() => handleBlur('title')}
                                    className={`form-input${fieldErrors.title ? ' form-input--error' : ''}`}
                                    placeholder="Product title"
                                    disabled={loading}
                                />
                                <FormFieldError error={fieldErrors.title} />
                            </div>

                            <div className="form-group">
                                <label htmlFor="cp-desc" className="form-label">Description</label>
                                <textarea
                                    id="cp-desc"
                                    value={description}
                                    onChange={(e) => { setDescription(e.target.value); clearFieldError('description'); }}
                                    onBlur={() => handleBlur('description')}
                                    className={`form-input cpp-textarea${fieldErrors.description ? ' form-input--error' : ''}`}
                                    placeholder="Describe your product…"
                                    rows={4}
                                    disabled={loading}
                                />
                                <FormFieldError error={fieldErrors.description} />
                            </div>

                            <div className="cpp-row">
                                <div className="form-group" style={{ flex: 1 }}>
                                    <label htmlFor="cp-price" className="form-label">Price ($) *</label>
                                    <input
                                        id="cp-price"
                                        type="number"
                                        min="0.01"
                                        step="0.01"
                                        value={price}
                                        onChange={(e) => { setPrice(e.target.value); clearFieldError('price'); }}
                                        onBlur={() => handleBlur('price')}
                                        className={`form-input${fieldErrors.price ? ' form-input--error' : ''}`}
                                        placeholder="0.00"
                                        disabled={loading}
                                    />
                                    <FormFieldError error={fieldErrors.price} />
                                </div>

                                <div className="form-group" style={{ flex: 1 }}>
                                    <label htmlFor="cp-condition" className="form-label">Condition *</label>
                                    <select
                                        id="cp-condition"
                                        value={condition}
                                        onChange={(e) => { setCondition(e.target.value); clearFieldError('condition'); }}
                                        onBlur={() => handleBlur('condition')}
                                        className={`form-input${fieldErrors.condition ? ' form-input--error' : ''}`}
                                        disabled={loading}
                                    >
                                        {CONDITION_OPTIONS.map(opt => (
                                            <option key={opt.value} value={opt.value} disabled={opt.value === ''}>
                                                {opt.label}
                                            </option>
                                        ))}
                                    </select>
                                    <FormFieldError error={fieldErrors.condition} />
                                </div>
                            </div>

                            <div className="cpp-row">
                                <div className="form-group" style={{ flex: 1 }}>
                                    <label htmlFor="cp-category" className="form-label">Category *</label>
                                    <CategorySelect
                                        id="cp-category"
                                        value={categoryId}
                                        onChange={(v) => { setCategoryId(v); clearFieldError('categoryId'); }}
                                        showAll={false}
                                    />
                                    <FormFieldError error={fieldErrors.categoryId} />
                                </div>

                                <div className="form-group" style={{ flex: 1 }}>
                                    <label htmlFor="cp-quantity" className="form-label">
                                        Quantity * {isAuction && <small className="text-muted">(Fixed to 1 for Auctions)</small>}
                                    </label>
                                    <input
                                        id="cp-quantity"
                                        type="number"
                                        min="0"
                                        value={isAuction ? 1 : quantity}
                                        onChange={(e) => { setQuantity(e.target.value); clearFieldError('quantity'); }}
                                        onBlur={() => handleBlur('quantity')}
                                        className={`form-input${fieldErrors.quantity ? ' form-input--error' : ''}`}
                                        placeholder="1"
                                        disabled={loading || isAuction}
                                    />
                                    <FormFieldError error={fieldErrors.quantity} />
                                </div>
                            </div>

                            <div className="form-group">
                                <label className="checkbox-label" htmlFor="cp-auction">
                                    <input
                                        type="checkbox"
                                        id="cp-auction"
                                        checked={isAuction}
                                        onChange={(e) => {
                                            setIsAuction(e.target.checked);
                                            if (!e.target.checked) clearFieldError('auctionEndTime');
                                            if (e.target.checked) setQuantity('1');
                                        }}
                                        disabled={loading}
                                    />
                                    <span>This is an auction listing</span>
                                </label>
                            </div>

                            {isAuction && (
                                <div className="form-group">
                                    <label htmlFor="cp-auction-end" className="form-label">Auction End Time *</label>
                                    <input
                                        id="cp-auction-end"
                                        type="datetime-local"
                                        value={auctionEndTime}
                                        onChange={(e) => { setAuctionEndTime(e.target.value); clearFieldError('auctionEndTime'); }}
                                        onBlur={() => handleBlur('auctionEndTime')}
                                        className={`form-input${fieldErrors.auctionEndTime ? ' form-input--error' : ''}`}
                                        disabled={loading}
                                    />
                                    <FormFieldError error={fieldErrors.auctionEndTime} />
                                </div>
                            )}

                            <div className="form-group">
                                <label className="form-label">Product Images</label>
                                <ImageUploader files={imageFiles} onChange={setImageFiles} loading={loading} />
                            </div>

                            <button type="submit" className="submit-button" disabled={loading}>
                                {loading ? 'Creating…' : 'Create Listing'}
                            </button>
                        </form>
                    </>
                )}
            </div>
        </div>
    );
};

export default CreateProductPage;
