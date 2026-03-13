import { useState, useEffect, useCallback } from 'react';
import { getBids } from '../api/bidApi';
import { formatCurrency, formatDateTime } from '../utils/productUtils';
import LoadingSpinner from './ui/LoadingSpinner';
import ErrorAlert from './ui/ErrorAlert';
import './BidHistory.css';

const BidHistory = ({ productId, currentBid, lastBidTrigger }) => {
    const [bids, setBids] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [page, setPage] = useState(1);
    const [hasMore, setHasMore] = useState(false);
    const pageSize = 10;

    const fetchBids = useCallback(async (reset = false) => {
        if (reset) {
            setLoading(true);
            setPage(1);
        }
        setError(null);
        try {
            const data = await getBids(productId, reset ? 1 : page, pageSize);
            const newBids = data.items || [];
            if (reset) {
                setBids(newBids);
            } else {
                setBids((prev) => [...prev, ...newBids]);
            }
            setHasMore(data.total > (reset ? 1 : page) * pageSize);
        } catch (err) {
            setError(err);
        } finally {
            setLoading(false);
        }
    }, [productId, page]);

    // Initial load
    useEffect(() => {
        fetchBids(true);
    }, [productId]);

    // Refetch when lastBidTrigger changes (from SignalR)
    useEffect(() => {
        if (lastBidTrigger) {
            fetchBids(true);
        }
    }, [lastBidTrigger, fetchBids]);

    const maskBidderName = (bidderId, bidderName) => {
        if (bidderName && bidderName.length > 2) {
            return `${bidderName[0]}***${bidderName[bidderName.length - 1]}`;
        }
        return `Bidder #${bidderId}`;
    };

    if (loading && page === 1) {
        return <div className="bid-history-loading"><LoadingSpinner /></div>;
    }

    if (error && page === 1) {
        return <ErrorAlert error={error} onRetry={() => fetchBids(true)} />;
    }

    return (
        <div className="bid-history-container">
            <h3 className="bid-history-title">Bid History</h3>

            {bids.length === 0 ? (
                <div className="bid-history-empty">No bids have been placed yet.</div>
            ) : (
                <ul className="bid-history-list">
                    {bids.map((bid, index) => (
                        <li key={bid.id} className={`bid-history-item ${index === 0 ? 'bid-highest' : ''}`}>
                            <div className="bid-info">
                                <span className="bid-user">{maskBidderName(bid.bidderId, bid.bidderName)}</span>
                                <span className="bid-time">{formatDateTime(bid.bidTime)}</span>
                            </div>
                            <div className="bid-amount">
                                {formatCurrency(bid.amount)}
                            </div>
                        </li>
                    ))}
                </ul>
            )}

            {hasMore && (
                <button
                    className="bid-load-more"
                    onClick={() => { setPage(p => p + 1); fetchBids(false); }}
                    disabled={loading}
                >
                    {loading ? 'Loading...' : 'Show More Bids'}
                </button>
            )}
        </div>
    );
};

export default BidHistory;
