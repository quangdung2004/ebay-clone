import { Link } from 'react-router-dom';
import { useState, useEffect } from 'react';
import { formatCurrency, formatDateTime, getConditionLabel, getStatusLabel, getPlaceholderImage, normalizeProductImageUrl } from '../utils/productUtils';
import './ProductCard.css';

const ProductCard = ({ product }) => {
    const {
        id, title, price, thumbnailUrl,
        status, condition,
        isAuction, inStock, availableQuantity,
        bidCount, currentBid, isEnded, auctionEndTime, viewCount
    } = product;

    const conditionLabel = getConditionLabel(condition);
    const statusLabel = getStatusLabel(status);

    const isTimeExpired = isAuction && auctionEndTime && new Date(auctionEndTime).getTime() <= new Date().getTime();
    const actuallyEnded = isEnded || status === 'ENDED' || status === 'SOLD' || isTimeExpired;

    const [timeLeftStr, setTimeLeftStr] = useState(isTimeExpired ? 'Ended' : '');

    useEffect(() => {
        if (!isAuction || actuallyEnded || !auctionEndTime) {
            setTimeLeftStr(isTimeExpired ? 'Ended' : '');
            return;
        }

        const intervalId = setInterval(() => {
            const now = new Date().getTime();
            const end = new Date(auctionEndTime).getTime();
            const diff = end - now;

            if (diff <= 0) {
                setTimeLeftStr('Ended');
                clearInterval(intervalId);
            } else {
                const d = Math.floor(diff / (1000 * 60 * 60 * 24));
                const h = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
                const m = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
                const s = Math.floor((diff % (1000 * 60)) / 1000);

                let timeStr = '';
                if (d > 0) timeStr = `${d}d ${h}h`;
                else if (h > 0) timeStr = `${h}h ${m}m`;
                else if (m > 0) timeStr = `${m}m ${s}s`;
                else timeStr = `${s}s`;

                setTimeLeftStr(timeStr);
            }
        }, 1000);

        return () => clearInterval(intervalId);
    }, [isAuction, actuallyEnded, auctionEndTime, isTimeExpired]);

    return (
        <Link to={`/products/${id}`} className="product-card" id={`product-card-${id}`}>
            <div className="product-card__img-wrap">
                <img
                    src={normalizeProductImageUrl(thumbnailUrl)}
                    alt={title}
                    className="product-card__img"
                    loading="lazy"
                    onError={(e) => { e.target.src = getPlaceholderImage(); }}
                />
                <div className="product-card__badges-top-left">
                    <span className={`product-badge bg-${conditionLabel.color}`}>
                        {conditionLabel.text}
                    </span>
                    {isAuction ? (
                        <span className="product-badge bg-primary">Auction</span>
                    ) : (
                        <span className="product-badge bg-info">Buy It Now</span>
                    )}
                </div>

                <div className="product-card__badges-top-right">
                    {(!inStock || status === 'OUT_OF_STOCK') && (
                        <span className="product-badge bg-danger">Out of stock</span>
                    )}
                    {actuallyEnded && (
                        <span className={`product-badge bg-${status === 'SOLD' ? 'success' : 'dark'}`}>{status === 'SOLD' ? 'Sold' : 'Ended'}</span>
                    )}
                </div>
            </div>

            <div className="product-card__body">
                <h3 className="product-card__title">{title}</h3>

                <div className="product-card__price-section">
                    <p className="product-card__price">
                        {isAuction && currentBid != null ? formatCurrency(currentBid) : formatCurrency(price)}
                    </p>
                    {isAuction && <span className="product-card__bid-count">{bidCount || 0} bids</span>}
                </div>

                <div className="product-card__meta">
                    {isAuction ? (
                        <>
                            <span className={`product-card__time ${actuallyEnded ? 'text-danger' : 'text-success'}`}>
                                {actuallyEnded ? 'Auction Ended' : (timeLeftStr ? `${timeLeftStr} left` : 'Loading...')}
                            </span>
                        </>
                    ) : (
                        <>
                            <span className="product-card__stock text-muted">
                                {inStock ? `${availableQuantity} available` : 'Sold out'}
                            </span>
                            <span className="product-card__views text-muted">
                                {viewCount} views
                            </span>
                        </>
                    )}
                </div>
                {product.sellerName && (
                    <div className="product-card__seller text-muted">
                        Seller: {product.sellerName}
                    </div>
                )}
            </div>
        </Link>
    );
};

export default ProductCard;
