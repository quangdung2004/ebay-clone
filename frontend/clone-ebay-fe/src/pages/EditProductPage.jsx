import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getProductById, updateProduct, updateInventory, updateProductStatus, uploadProductImages, deleteProductImage } from '../api/productApi';
import { parseApiError } from '../utils/errorUtils';
import { validateProduct, hasErrors } from '../utils/formValidation';
import { useToast } from '../components/Toast';
import { useAuth } from '../context/AuthContext';
import { normalizeProductImageUrl } from '../utils/productUtils';
import CategorySelect from '../components/CategorySelect';
import ImageUploader from '../components/ImageUploader';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import ErrorAlert from '../components/ui/ErrorAlert';
import FormFieldError from '../components/ui/FormFieldError';
import './EditProductPage.css';

const CONDITION_OPTIONS = [
    { value: '', label: 'Select Condition' },
    { value: 'NEW', label: 'New' },
    { value: 'USED', label: 'Used' },
    { value: 'REFURBISHED', label: 'Refurbished' },
    { value: 'OPEN_BOX', label: 'Open Box' },
    { value: 'FOR_PARTS', label: 'For Parts' },
];

const STATUS_OPTIONS = [
    { value: 'ACTIVE', label: 'Active' },
    { value: 'INACTIVE', label: 'Inactive' },
    { value: 'OUT_OF_STOCK', label: 'Out of Stock' },
    { value: 'ENDED', label: 'Ended' },
];

const EditProductPage = () => {
    const { id } = useParams();
    const navigate = useNavigate();
    const { showSuccess } = useToast();

    const { user } = useAuth();
    const [product, setProduct] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Form states
    const [title, setTitle] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [isAuction, setIsAuction] = useState(false);
    const [auctionEndTime, setAuctionEndTime] = useState('');
    const [condition, setCondition] = useState('');

    // Derived state
    const isEnded = product?.isEnded;

    // Inventory & Status
    const [quantity, setQuantity] = useState('');
    const [status, setStatus] = useState('');

    // Images
    const [images, setImages] = useState([]);
    const [uploadFiles, setUploadFiles] = useState([]);

    // UI States
    const [fieldErrors, setFieldErrors] = useState({});
    const [updatingInfo, setUpdatingInfo] = useState(false);
    const [updatingInv, setUpdatingInv] = useState(false);
    const [updatingStatus, setUpdatingStatus] = useState(false);
    const [updatingImages, setUpdatingImages] = useState(false);

    console.log("[DEBUG] EditProductPage mounted with route id:", id);

    const fetchProduct = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            console.log(`[DEBUG] Attempting to fetch product ID: ${id}`);
            const data = await getProductById(id);
            // We removed the frontend hard-coded (data.sellerId !== user.id) check here.
            // If the user attempts an edit that they shouldn't, the backend will return 403 Forbidden.
            // This guarantees the frontend does not block based on stale state.

            setProduct(data);

            // Populate form
            setTitle(data.title || '');
            setDescription(data.description || '');
            setPrice(data.price || '');
            setCategoryId(data.categoryId || '');
            setIsAuction(data.isAuction || false);

            if (data.auctionEndTime) {
                const d = new Date(data.auctionEndTime);
                const localStr = new Date(d.getTime() - d.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
                setAuctionEndTime(localStr);
            } else {
                setAuctionEndTime('');
            }

            setCondition(data.condition || '');
            setQuantity(data.availableQuantity ?? data.quantity ?? 1);
            setStatus(data.status || 'ACTIVE');
            setImages(data.images || []);
        } catch (err) {
            setError(parseApiError(err, 'Failed to load product'));
        } finally {
            setLoading(false);
        }
    }, [id]);

    useEffect(() => {
        if (user) {
            fetchProduct();
        }
    }, [fetchProduct, user]);

    // Common error handler helper
    const handleActionError = (err) => {
        const parsed = parseApiError(err);
        console.error("[DEBUG] Raw update error response: ", err.response?.data || err);

        if (parsed.code === 'PRODUCT_FORBIDDEN') {
            setError({ message: 'You are not authorized to edit this product. Please ensure you are logged in with the correct seller account.', code: parsed.code });
        } else {
            setError(parsed);
        }
    };

    const handleUpdateInfo = async (e) => {
        e.preventDefault();
        setError(null);

        // Custom validation object since edit doesn't require quantity for info update
        const values = { title, description, price, categoryId, isAuction, auctionEndTime, quantity: 1, condition }; // dummy quantity
        const errors = validateProduct(values);

        // Remove quantity error if any, it's irrelevant here
        delete errors.quantity;

        setFieldErrors(errors);

        if (hasErrors(errors)) return;

        setUpdatingInfo(true);
        try {
            const payload = {
                title: title.trim(),
                description: description.trim() || undefined,
                price: Number(price),
                categoryId: categoryId ? Number(categoryId) : undefined,
                isAuction,
                auctionEndTime: isAuction && auctionEndTime ? new Date(auctionEndTime).toISOString() : undefined,
                condition
            };

            console.log("[DEBUG] Clicked Save Info for ID: ", id);
            console.log(`[DEBUG] Attempting Request PUT /api/products/${id}`);

            await updateProduct(id, payload);
            showSuccess('Basic info updated successfully!');
            fetchProduct();
        } catch (err) {
            handleActionError(err);
        } finally {
            setUpdatingInfo(false);
        }
    };

    const handleUpdateInventory = async (e) => {
        e.preventDefault();
        setError(null);
        const qty = parseInt(quantity, 10);
        if (isNaN(qty) || qty < 0) {
            setError({ message: 'Please enter a valid non-negative quantity' });
            return;
        }

        setUpdatingInv(true);
        try {
            console.log(`[DEBUG] Attempting Request PUT /api/products/${id}/inventory`);
            await updateInventory(id, { quantity: qty });
            showSuccess('Inventory updated successfully!');
            fetchProduct();
        } catch (err) {
            handleActionError(err);
        } finally {
            setUpdatingInv(false);
        }
    };

    const handleUpdateStatus = async (e) => {
        e.preventDefault();
        setError(null);
        if (!status) return;

        setUpdatingStatus(true);
        try {
            console.log(`[DEBUG] Attempting Request PATCH /api/products/${id}/status`);
            await updateProductStatus(id, { status });
            showSuccess('Status updated successfully!');
            fetchProduct();
        } catch (err) {
            handleActionError(err);
        } finally {
            setUpdatingStatus(false);
        }
    };

    const handleUploadImages = async () => {
        if (!uploadFiles.length) return;
        setError(null);
        setUpdatingImages(true);
        try {
            console.log(`[DEBUG] Attempting Request POST /api/products/${id}/images`);
            await uploadProductImages(id, uploadFiles);
            showSuccess('Images uploaded successfully!');
            setUploadFiles([]);
            fetchProduct();
        } catch (err) {
            handleActionError(err);
        } finally {
            setUpdatingImages(false);
        }
    };

    const handleDeleteImage = async (url) => {
        if (!window.confirm('Delete this image?')) return;
        setError(null);
        setUpdatingImages(true);
        try {
            console.log(`[DEBUG] Attempting Request DELETE /api/products/${id}/images`);
            await deleteProductImage(id, url);
            showSuccess('Image deleted!');
            fetchProduct();
        } catch (err) {
            handleActionError(err);
        } finally {
            setUpdatingImages(false);
        }
    };

    if (loading) return <LoadingSpinner message="Loading Listing..." style={{ marginTop: '3rem' }} />;

    return (
        <div className="edit-product-page">
            <h1 className="ep-title">Edit Listing: {product?.title}</h1>

            {error && <div style={{ maxWidth: 600, margin: '0 auto 2rem auto' }}><ErrorAlert error={error} onRetry={fetchProduct} /></div>}

            {product && (
                <div className="ep-sections">
                    {/* Basic Info Section */}
                    <section className="ep-card">
                        <h2 className="ep-card-title">Basic Information</h2>
                        <form onSubmit={handleUpdateInfo}>
                            <div className="form-group">
                                <label className="form-label">Title *</label>
                                <input
                                    value={title}
                                    onChange={(e) => setTitle(e.target.value)}
                                    className={`form-input${fieldErrors.title ? ' form-input--error' : ''}`}
                                    disabled={isAuction && isEnded}
                                />
                                <FormFieldError error={fieldErrors.title} />
                            </div>

                            <div className="form-group">
                                <label className="form-label">Description</label>
                                <textarea
                                    value={description}
                                    onChange={(e) => setDescription(e.target.value)}
                                    rows={4}
                                    className="form-input ep-textarea"
                                    disabled={isAuction && isEnded}
                                />
                                <FormFieldError error={fieldErrors.description} />
                            </div>

                            <div className="ep-row">
                                <div className="form-group" style={{ flex: 1 }}>
                                    <label className="form-label">Price ($) *</label>
                                    <input
                                        type="number" step="0.01" value={price}
                                        onChange={(e) => setPrice(e.target.value)}
                                        className={`form-input${fieldErrors.price ? ' form-input--error' : ''}`}
                                        disabled={isAuction && isEnded}
                                    />
                                    <FormFieldError error={fieldErrors.price} />
                                </div>

                                <div className="form-group" style={{ flex: 1 }}>
                                    <label className="form-label">Condition *</label>
                                    <select
                                        value={condition}
                                        onChange={(e) => setCondition(e.target.value)}
                                        className={`form-input${fieldErrors.condition ? ' form-input--error' : ''}`}
                                        disabled={isAuction && isEnded}
                                    >
                                        {CONDITION_OPTIONS.map(o => <option key={o.value} value={o.value} disabled={!o.value}>{o.label}</option>)}
                                    </select>
                                    <FormFieldError error={fieldErrors.condition} />
                                </div>
                            </div>

                            <div className="ep-row">
                                <div className="form-group" style={{ flex: 1 }}>
                                    <label className="form-label">Category *</label>
                                    <CategorySelect
                                        value={categoryId}
                                        onChange={setCategoryId}
                                        showAll={false}
                                    />
                                    <FormFieldError error={fieldErrors.categoryId} />
                                </div>

                                {isAuction && (
                                    <div className="form-group" style={{ flex: 1 }}>
                                        <label className="form-label">Auction End Time</label>
                                        <input
                                            type="datetime-local"
                                            value={auctionEndTime}
                                            onChange={(e) => setAuctionEndTime(e.target.value)}
                                            className={`form-input${fieldErrors.auctionEndTime ? ' form-input--error' : ''}`}
                                            disabled={isEnded}
                                        />
                                        {isEnded && <span className="ep-warning-text">Auction has ended, cannot modify end time.</span>}
                                        <FormFieldError error={fieldErrors.auctionEndTime} />
                                    </div>
                                )}
                            </div>

                            <div className="ep-actions">
                                <button type="submit" className="ep-btn ep-btn-primary" disabled={updatingInfo || (isAuction && isEnded)}>
                                    {updatingInfo ? 'Saving...' : 'Save Info'}
                                </button>
                            </div>
                        </form>
                    </section>

                    <div className="ep-side-sections">
                        {/* Inventory Section */}
                        {(!isAuction || !isEnded) && (
                            <section className="ep-card">
                                <h2 className="ep-card-title">Inventory</h2>
                                <form onSubmit={handleUpdateInventory} className="ep-flex-form">
                                    <input
                                        type="number" min="0" value={quantity}
                                        onChange={(e) => setQuantity(e.target.value)}
                                        className="form-input"
                                    />
                                    <button type="submit" className="ep-btn ep-btn-secondary" disabled={updatingInv}>
                                        {updatingInv ? 'Updating...' : 'Update Stock'}
                                    </button>
                                </form>
                            </section>
                        )}

                        {/* Status Section */}
                        <section className="ep-card">
                            <h2 className="ep-card-title">Listing Status</h2>
                            <form onSubmit={handleUpdateStatus} className="ep-flex-form">
                                <select
                                    value={status}
                                    onChange={(e) => setStatus(e.target.value)}
                                    className="form-input"
                                >
                                    {STATUS_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
                                </select>
                                <button type="submit" className="ep-btn ep-btn-secondary" disabled={updatingStatus}>
                                    {updatingStatus ? 'Updating...' : 'Change Status'}
                                </button>
                            </form>
                        </section>
                    </div>

                    {/* Images Section */}
                    <section className="ep-card ep-full-width">
                        <h2 className="ep-card-title">Manage Images</h2>

                        <div className="ep-image-gallery">
                            {images.map((imgUrl, i) => (
                                <div key={i} className="ep-image-thumb">
                                    <img src={normalizeProductImageUrl(imgUrl)} alt={`Product ${i}`} />
                                    <button
                                        className="ep-image-delete"
                                        onClick={() => handleDeleteImage(imgUrl)}
                                        disabled={updatingImages}
                                    >
                                        &times;
                                    </button>
                                </div>
                            ))}
                            {images.length === 0 && <p className="text-muted" style={{ marginLeft: 10 }}>No images.</p>}
                        </div>

                        <div className="ep-image-uploader">
                            <label className="form-label" style={{ marginTop: 20 }}>Add New Images</label>
                            <ImageUploader files={uploadFiles} onChange={setUploadFiles} loading={updatingImages} />
                            {uploadFiles.length > 0 && (
                                <button
                                    className="ep-btn ep-btn-primary"
                                    onClick={handleUploadImages}
                                    disabled={updatingImages}
                                    style={{ marginTop: 15 }}
                                >
                                    {updatingImages ? 'Uploading...' : `Upload ${uploadFiles.length} Image(s)`}
                                </button>
                            )}
                        </div>
                    </section>
                </div>
            )}
        </div>
    );
};

export default EditProductPage;
