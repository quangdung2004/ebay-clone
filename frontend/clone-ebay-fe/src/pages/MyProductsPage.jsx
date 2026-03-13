import { useState, useEffect, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { getMyProducts, deleteProduct, updateProductStatus } from '../api/productApi';
import { closeAuction } from '../api/auctionApi';
import { parseApiError } from '../utils/errorUtils';
import { formatCurrency, formatDateTime, getStatusLabel, getConditionLabel, getPlaceholderImage, normalizeProductImageUrl } from '../utils/productUtils';
import { useToast } from '../components/Toast';
import Pagination from '../components/Pagination';
import ErrorAlert from '../components/ui/ErrorAlert';
import LoadingSpinner from '../components/ui/LoadingSpinner';
import './MyProductsPage.css';

const PAGE_SIZE = 10;

const ProductRow = ({ p, onRefresh }) => {
    const { showSuccess, showError } = useToast();
    const [actionLoading, setActionLoading] = useState(false);

    const statusLabel = getStatusLabel(p.status);
    const conditionLabel = getConditionLabel(p.condition);
    const isTimeExpired = p.isAuction && new Date(p.auctionEndTime).getTime() <= new Date().getTime();
    const isEndedAuction = p.isAuction && (p.isEnded || p.status === 'ENDED' || p.status === 'SOLD' || isTimeExpired);
    const canCloseAuction = p.isAuction && !isEndedAuction && p.status === 'ACTIVE';

    const handleDelete = async () => {
        if (!window.confirm(`Are you sure you want to soft delete "${p.title}"?`)) return;
        setActionLoading(true);
        try {
            await deleteProduct(p.id);
            showSuccess('Product deleted successfully');
            onRefresh();
        } catch (err) {
            showError(parseApiError(err).message);
        } finally {
            setActionLoading(false);
        }
    };

    const handleStatusChange = async () => {
        const newStatus = p.status === 'ACTIVE' ? 'INACTIVE' : 'ACTIVE';
        if (!window.confirm(`Change status to ${newStatus}?`)) return;
        setActionLoading(true);
        try {
            await updateProductStatus(p.id, { status: newStatus });
            showSuccess(`Status changed to ${newStatus}`);
            onRefresh();
        } catch (err) {
            showError(parseApiError(err).message);
        } finally {
            setActionLoading(false);
        }
    };

    const handleCloseAuction = async () => {
        if (!window.confirm('Are you sure you want to close this auction? This will finalize the winner and end the listing.')) return;
        setActionLoading(true);
        try {
            await closeAuction(p.id);
            showSuccess('Auction closed successfully');
            onRefresh();
        } catch (err) {
            showError(parseApiError(err).message);
        } finally {
            setActionLoading(false);
        }
    };

    return (
        <tr key={p.id} className={actionLoading ? 'mp-row-loading' : ''}>
            <td className="mp-cell-item">
                <img
                    src={normalizeProductImageUrl(p.thumbnailUrl)}
                    alt={p.title}
                    className="mp-thumbnail"
                    onError={(e) => { e.target.src = getPlaceholderImage(); }}
                />
                <div className="mp-item-info">
                    <Link to={`/products/${p.id}`} className="mp-item-title">{p.title}</Link>
                    <div className="mp-item-badges">
                        <span className={`mp-small-badge bg-${conditionLabel.color}`}>{conditionLabel.text}</span>
                        <span className="mp-small-badge bg-primary">{p.isAuction ? 'Auction' : 'Buy It Now'}</span>
                    </div>
                    {p.isAuction && (
                        <div className="mp-item-time text-muted">
                            {isEndedAuction ? 'Ended' : `Ends: ${formatDateTime(p.auctionEndTime)}`}
                        </div>
                    )}
                </div>
            </td>
            <td>{formatCurrency(p.isAuction && p.currentBid != null ? p.currentBid : p.price)}</td>
            <td>
                {p.isAuction ? (
                    <span>{p.bidCount || 0} Bids</span>
                ) : (
                    <span>{p.availableQuantity || 0} Qty</span>
                )}
            </td>
            <td>
                <span className={`mp-badge bg-${statusLabel.color}`}>
                    {statusLabel.text}
                </span>
            </td>
            <td>
                <div className="mp-actions-grid">
                    <Link
                        to={`/sell/edit/${p.id}`}
                        className={`mp-action-btn edit ${actionLoading ? 'disabled' : ''}`}
                        title="Edit, Update Inventory, Manage Images"
                        onClick={() => console.log(`[DEBUG] Clicked Edit button in MyProductsPage for product ID: ${p.id}`)}
                        style={actionLoading ? { pointerEvents: 'none', opacity: 0.6 } : {}}
                    >
                        Edit
                    </Link>
                    <button
                        className={`mp-action-btn toggle ${p.status !== 'ACTIVE' ? 'activate' : ''}`}
                        onClick={handleStatusChange}
                        disabled={actionLoading}
                    >
                        {p.status === 'ACTIVE' ? 'Deactivate' : 'Activate'}
                    </button>
                    <button
                        className="mp-action-btn delete"
                        onClick={handleDelete}
                        disabled={actionLoading}
                    >
                        Delete
                    </button>
                    {canCloseAuction && (
                        <button
                            className="mp-action-btn close-auction"
                            onClick={handleCloseAuction}
                            disabled={actionLoading}
                        >
                            Close Auction
                        </button>
                    )}
                </div>
            </td>
        </tr>
    );
};

const MyProductsPage = () => {
    const { showError } = useToast();

    const [products, setProducts] = useState([]);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(1);
    const [statusFilter, setStatusFilter] = useState('');

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const fetchMyProducts = useCallback(async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await getMyProducts({
                page,
                pageSize: PAGE_SIZE,
                status: statusFilter || undefined
            });
            setProducts(data.items || []);
            setTotal(data.total || 0);
        } catch (err) {
            setError(parseApiError(err, 'Failed to load your products'));
        } finally {
            setLoading(false);
        }
    }, [page, statusFilter]);

    useEffect(() => {
        fetchMyProducts();
    }, [fetchMyProducts]);

    return (
        <div className="my-products-page">
            <div className="mp-header">
                <h1 className="mp-title">My Products</h1>
                <Link to="/sell/new" className="mp-btn mp-btn-primary">Create New Listing</Link>
            </div>

            <div className="mp-filters">
                <label htmlFor="filter-status">Filter by Status:</label>
                <select
                    id="filter-status"
                    value={statusFilter}
                    onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}
                    className="mp-select"
                >
                    <option value="">All Statuses</option>
                    <option value="ACTIVE">Active</option>
                    <option value="INACTIVE">Inactive</option>
                    <option value="OUT_OF_STOCK">Out of Stock</option>
                    <option value="ENDED">Ended</option>
                    <option value="SOLD">Sold</option>
                </select>
            </div>

            {error && <ErrorAlert error={error} onRetry={fetchMyProducts} />}

            {loading ? (
                <div style={{ padding: '4rem 0' }}>
                    <LoadingSpinner message="Loading products..." />
                </div>
            ) : products.length === 0 ? (
                <div className="mp-empty">
                    <p>No products found.</p>
                </div>
            ) : (
                <div className="mp-table-container">
                    <table className="mp-table">
                        <thead>
                            <tr>
                                <th>Item</th>
                                <th>Price / Current Bid</th>
                                <th>Stock / Bids</th>
                                <th>Status</th>
                                <th>Actions</th>
                            </tr>
                        </thead>
                        <tbody>
                            {products.map(p => (
                                <ProductRow key={p.id} p={p} onRefresh={fetchMyProducts} />
                            ))}
                        </tbody>
                    </table>
                </div>
            )}

            {!loading && products.length > 0 && (
                <Pagination
                    page={page}
                    pageSize={PAGE_SIZE}
                    total={total}
                    onPageChange={setPage}
                />
            )}
        </div>
    );
};

export default MyProductsPage;
