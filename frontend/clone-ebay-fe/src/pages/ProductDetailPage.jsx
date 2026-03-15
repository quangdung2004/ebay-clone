import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { getProductById } from '../api/productApi';
import { placeBid } from '../api/bidApi';
import { checkoutOrder } from '../api/orderApi';
import { getMyAddresses } from '../api/addressApi';
import { parseApiError } from '../utils/errorUtils';
import {
  formatCurrency,
  formatDateTime,
  getConditionLabel,
  getStatusLabel,
  getPlaceholderImage,
  normalizeProductImageUrl,
} from '../utils/productUtils';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../components/Toast';
import BidHistory from '../components/BidHistory';
import signalrService from '../services/signalrService';
import ErrorAlert from '../components/ui/ErrorAlert';
import { addToCart, getCartItem } from '../utils/cartUtils';
import { getSelectedShippingAddressId } from '../utils/checkoutAddress';
import './ProductDetailPage.css';

const DEFAULT_PAYMENT_METHOD = 'COD';

const ProductDetailPage = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();
  const { showSuccess, showError } = useToast();

  const [product, setProduct] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  const [buyingNow, setBuyingNow] = useState(false);
  const [quantity, setQuantity] = useState(1);
  const [isInCart, setIsInCart] = useState(false);

  const [selectedImg, setSelectedImg] = useState(0);

  const [bidAmount, setBidAmount] = useState('');
  const [bidding, setBidding] = useState(false);
  const [lastBidTrigger, setLastBidTrigger] = useState(0);

  const [timeLeftStr, setTimeLeftStr] = useState('');

  const fetchProduct = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const data = await getProductById(id);
      setProduct(data);
    } catch (err) {
      setError(parseApiError(err, 'Failed to load product'));
    } finally {
      setLoading(false);
    }
  }, [id]);

  useEffect(() => {
    fetchProduct();
  }, [fetchProduct]);

  useEffect(() => {
    const syncCart = () => setIsInCart(!!getCartItem(id));
    syncCart();

    window.addEventListener('cart-updated', syncCart);
    return () => window.removeEventListener('cart-updated', syncCart);
  }, [id]);

  useEffect(() => {
    if (
      !product ||
      !product.isAuction ||
      product.isEnded ||
      product.status === 'ENDED' ||
      product.status === 'SOLD'
    ) {
      return;
    }

    const intervalId = setInterval(() => {
      const now = new Date().getTime();
      const end = new Date(product.auctionEndTime).getTime();
      const diff = end - now;

      if (diff <= 0) {
        setTimeLeftStr('0d 0h 0m 0s');
        setProduct((p) => ({ ...p, isEnded: true, status: 'ENDED' }));
        clearInterval(intervalId);
      } else {
        const d = Math.floor(diff / (1000 * 60 * 60 * 24));
        const h = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const m = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));
        const s = Math.floor((diff % (1000 * 60)) / 1000);
        setTimeLeftStr(`${d}d ${h}h ${m}m ${s}s`);
      }
    }, 1000);

    return () => clearInterval(intervalId);
  }, [product]);

  useEffect(() => {
    if (!product || !product.isAuction) return;

    let isSubscribed = true;

    const setupSignalR = async () => {
      await signalrService.connect();
      if (!isSubscribed) return;

      await signalrService.joinProductRoom(id);

      signalrService.onBidPlaced((data) => {
        if (data.productId === Number(id)) {
          setProduct((p) => ({
            ...p,
            currentBid: data.currentBid,
            bidCount: data.bidCount,
          }));

          setLastBidTrigger((prev) => prev + 1);

          if (user && data.bidderId !== user.id) {
            showSuccess(
              `A new higher bid of ${formatCurrency(data.currentBid)} has been placed!`
            );
          }
        }
      });

      signalrService.onAuctionClosed((data) => {
        if (data.productId === Number(id)) {
          setProduct((p) => ({
            ...p,
            isEnded: true,
            status: data.hasWinner ? 'SOLD' : 'ENDED',
            winnerUserId: data.winnerUserId,
            currentBid: data.winningBid || p.currentBid,
          }));

          showSuccess('Auction has ended');
        }
      });
    };

    setupSignalR();

    return () => {
      isSubscribed = false;
      signalrService.offBidPlaced();
      signalrService.offAuctionClosed();
      signalrService.leaveProductRoom(id);
    };
  }, [product?.isAuction, id, user, showSuccess]);

  const handleBuyNow = async () => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    if (user && user.id === product.sellerId) {
      showError('You cannot buy your own item.');
      return;
    }

    if (!product?.id) {
      showError('Product not found.');
      return;
    }

    if (!quantity || Number(quantity) <= 0) {
      showError('Please enter a valid quantity.');
      return;
    }

    if (
      product.availableQuantity &&
      Number(quantity) > Number(product.availableQuantity)
    ) {
      showError(`Only ${product.availableQuantity} item(s) available.`);
      return;
    }

    try {
      setBuyingNow(true);

      const addresses = await getMyAddresses();
      if (!addresses.length) {
        showError('You need to create a shipping address before placing an order.');
        navigate(`/addresses?redirect=/products/${product.id}`);
        return;
      }

      const selectedAddressId = getSelectedShippingAddressId();
      if (!selectedAddressId) {
        showError('Please select a shipping address before placing an order.');
        navigate(`/addresses?redirect=/products/${product.id}`);
        return;
      }

      const payload = {
        paymentMethod: DEFAULT_PAYMENT_METHOD,
        addressId: selectedAddressId,
        items: [
          {
            productId: Number(product.id),
            quantity: Number(quantity),
          },
        ],
      };

      await checkoutOrder(payload);

      showSuccess('Order created successfully!');
      navigate('/orders');
    } catch (err) {
      const apiErr = parseApiError(err, 'Checkout failed');

      if (apiErr.code === 'ADDRESS_NOT_FOUND') {
        navigate(`/addresses?redirect=/products/${product.id}`);
      }

      showError(apiErr.message || 'Checkout failed');
    } finally {
      setBuyingNow(false);
    }
  };

  const handleAddToCart = () => {
    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    if (!canBuy) {
      showError('This item is not available for cart.');
      return;
    }

    addToCart(product, Number(quantity));
    setIsInCart(true);
    showSuccess('Added to cart successfully.');
  };

  const handleSeeInCart = () => {
    navigate('/cart');
  };

  const handlePlaceBid = async (e) => {
    e.preventDefault();

    if (!isAuthenticated) {
      navigate('/login');
      return;
    }

    if (user && user.id === product.sellerId) {
      showError('You cannot bid on your own item.');
      return;
    }

    const amount = Number(bidAmount);
    const minBid =
      product.currentBid != null ? product.currentBid + 1 : product.price;

    if (isNaN(amount) || amount <= 0) {
      showError('Please enter a valid bid amount.');
      return;
    }

    if (amount < minBid) {
      showError(`Bid must be at least ${formatCurrency(minBid)}`);
      return;
    }

    setBidding(true);
    try {
      await placeBid(id, amount);
      showSuccess('Bid placed successfully!');
      setBidAmount('');
    } catch (err) {
      const apiErr = parseApiError(err);
      showError(apiErr.message || 'Failed to place bid');
    } finally {
      setBidding(false);
    }
  };

  if (loading) {
    return (
      <div className="pdp-container">
        <div className="pdp-skeleton">
          <div className="skeleton-img pdp-skeleton-gallery" />
          <div className="pdp-skeleton-info">
            <div
              className="skeleton-text"
              style={{ width: '70%', height: '1.5rem' }}
            />
            <div
              className="skeleton-text"
              style={{ width: '30%', height: '1.2rem', marginTop: '1rem' }}
            />
            <div
              className="skeleton-text"
              style={{ width: '100%', height: '4rem', marginTop: '1rem' }}
            />
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="pdp-container">
        <div style={{ maxWidth: 600, margin: '2rem auto', padding: '0 1rem' }}>
          <ErrorAlert error={error} onRetry={fetchProduct} />
        </div>
      </div>
    );
  }

  if (!product) {
    return (
      <div className="pdp-container">
        <ErrorAlert error={{ message: 'Product not found' }} />
      </div>
    );
  }

  const images = product.images?.length
    ? product.images.map(normalizeProductImageUrl)
    : [null];

  const conditionLabel = getConditionLabel(product.condition);
  const statusLabel = getStatusLabel(product.status);

  const isOwner = user?.id === product.sellerId;
  const isOut = product.status === 'OUT_OF_STOCK';
  const isInactive = product.status === 'INACTIVE';
  const isSold = product.status === 'SOLD';
  const isEnded = product.status === 'ENDED' || product.isEnded;

  const canBuy =
    !product.isAuction &&
    product.inStock &&
    !isOut &&
    !isInactive &&
    !isSold &&
    !isOwner;

  const canBid =
    product.isAuction &&
    !isEnded &&
    !isInactive &&
    !isSold &&
    !isOut &&
    !isOwner;

  const totalPrice = Number(product.price || 0) * Number(quantity || 1);

  return (
    <div className="pdp-container">
      <div className="pdp-layout">
        <div className="pdp-gallery">
          <div className="pdp-gallery__main">
            <img
              src={images[selectedImg] || getPlaceholderImage()}
              alt={product.title}
              className="pdp-gallery__img"
              onError={(e) => {
                e.target.src = getPlaceholderImage();
              }}
            />
          </div>

          {images.length > 1 && (
            <div className="pdp-gallery__thumbs">
              {images.map((url, i) => (
                <button
                  key={i}
                  className={`pdp-gallery__thumb ${
                    i === selectedImg ? 'pdp-gallery__thumb--active' : ''
                  }`}
                  onClick={() => setSelectedImg(i)}
                >
                  <img
                    src={url || getPlaceholderImage()}
                    alt=""
                    onError={(e) => {
                      e.target.src = getPlaceholderImage();
                    }}
                  />
                </button>
              ))}
            </div>
          )}
        </div>

        <div className="pdp-info">
          <h1 className="pdp-title">{product.title}</h1>

          <div className="pdp-meta-stats">
            <span className={`badge bg-${conditionLabel.color}`}>
              {conditionLabel.text}
            </span>

            {product.isAuction ? (
              <span className="badge bg-primary">Auction</span>
            ) : (
              <span className="badge bg-info">Buy It Now</span>
            )}

            <span className={`badge bg-${statusLabel.color}`}>
              {statusLabel.text}
            </span>
          </div>

          <div className="pdp-price-section">
            {product.isAuction ? (
              <>
                <p className="pdp-price-label">Current Bid:</p>
                <p className="pdp-price pdp-price-auction">
                  {product.currentBid != null
                    ? formatCurrency(product.currentBid)
                    : formatCurrency(product.price)}
                </p>

                <div className="pdp-auction-meta-details">
                  <div className="pdp-auction-stat">
                    <span className="stat-value">{product.bidCount || 0}</span>
                    <span className="stat-label">Bids</span>
                  </div>

                  <div className="pdp-auction-stat">
                    {isEnded || isSold ? (
                      <span className="stat-value text-danger">Ended</span>
                    ) : (
                      <>
                        <span className="stat-value text-danger">{timeLeftStr}</span>
                        <span className="stat-label">Time Left</span>
                      </>
                    )}
                  </div>
                </div>

                <div className="pdp-auction-end-time text-muted">
                  Ends: {formatDateTime(product.auctionEndTime)}
                </div>
              </>
            ) : (
              <>
                <p className="pdp-price pdp-price-buynow">
                  {formatCurrency(product.price)}
                </p>

                <div className="pdp-stock-meta">
                  {product.inStock && product.availableQuantity > 0 ? (
                    <span className="text-success">
                      {product.availableQuantity} available
                    </span>
                  ) : (
                    <span className="text-danger">Out of stock</span>
                  )}
                </div>
              </>
            )}
          </div>

          <div className="pdp-seller-info">
            <div className="seller-label">Seller Information</div>
            <div className="seller-name">
              {product.sellerName || `Seller #${product.sellerId}`}
            </div>
            <div className="seller-stats">
              {product.viewCount} views &bull;{' '}
              {product.categoryName || `Category ${product.categoryId}`}
            </div>
          </div>

          <div className="pdp-actions-panel">
            {isOwner ? (
              <div className="pdp-action-group">
                <p className="text-muted" style={{ fontWeight: '500', marginBottom: 0 }}>
                  This is your listing.
                </p>

                <Link
                  to={`/sell/edit/${product.id}`}
                  className="pdp-btn-secondary"
                  style={{ textAlign: 'center', display: 'block' }}
                >
                  Edit Listing
                </Link>
              </div>
            ) : product.isAuction ? (
              <div className="pdp-bid-container">
                {isEnded || isSold ? (
                  <div className="pdp-ended-message">
                    {product.status === 'SOLD'
                      ? 'This auction has ended and the item was sold.'
                      : 'This auction has ended without a winner.'}

                    {product.winnerUserId && user?.id === product.winnerUserId && (
                      <div className="winner-badge">You won this auction!</div>
                    )}
                  </div>
                ) : (
                  <form className="pdp-bid-form" onSubmit={handlePlaceBid}>
                    <input
                      type="number"
                      className="pdp-bid-input"
                      placeholder="Enter your bid..."
                      value={bidAmount}
                      onChange={(e) => setBidAmount(e.target.value)}
                      disabled={!canBid || bidding}
                    />

                    <button
                      type="submit"
                      className="pdp-btn-primary"
                      disabled={!canBid || bidding}
                    >
                      {bidding ? 'Placing...' : 'Place Bid'}
                    </button>
                  </form>
                )}

                <div className="min-bid-hint">
                  {!isEnded &&
                    !isSold &&
                    canBid &&
                    `Enter ${formatCurrency(
                      product.currentBid != null
                        ? product.currentBid + 1
                        : product.price
                    )} or more`}
                </div>
              </div>
            ) : (
              <div className="pdp-action-group">
                <div className="pdp-checkout-card">
                  <div className="pdp-input-group">
                    <label className="pdp-input-label">Quantity</label>
                    <input
                      type="number"
                      min="1"
                      max={product.availableQuantity || 1}
                      className="pdp-qty-input"
                      value={quantity}
                      onChange={(e) => setQuantity(e.target.value)}
                      disabled={!canBuy || buyingNow}
                    />
                  </div>

                  <div className="pdp-order-summary">
                    <div className="pdp-order-summary__row">
                      <span>Unit price</span>
                      <strong>{formatCurrency(product.price)}</strong>
                    </div>

                    <div className="pdp-order-summary__row">
                      <span>Quantity</span>
                      <strong>{quantity}</strong>
                    </div>

                    <div className="pdp-order-summary__row pdp-order-summary__row--total">
                      <span>Total</span>
                      <strong>{formatCurrency(totalPrice)}</strong>
                    </div>
                  </div>

                  <button
                    className="pdp-btn-primary"
                    onClick={handleBuyNow}
                    disabled={!canBuy || buyingNow}
                  >
                    {!canBuy
                      ? 'Currently Unavailable'
                      : buyingNow
                      ? 'Processing...'
                      : 'Buy It Now'}
                  </button>

                  <button
                    className="pdp-btn-secondary"
                    type="button"
                    onClick={isInCart ? handleSeeInCart : handleAddToCart}
                    disabled={!canBuy || buyingNow}
                  >
                    {isInCart ? 'See in cart' : 'Add to cart'}
                  </button>

                  <div className="pdp-checkout-note">
                    If you do not have a shipping address or have not selected one,
                    the app will ask you to choose one before checkout.
                  </div>
                </div>
              </div>
            )}
          </div>

          {product.description && (
            <div className="pdp-description">
              <h2 className="pdp-section-title">About this item</h2>
              <p className="pdp-desc-text">{product.description}</p>
            </div>
          )}
        </div>
      </div>

      {product.isAuction && (
        <div className="pdp-bottom-section">
          <BidHistory
            productId={product.id}
            currentBid={product.currentBid}
            lastBidTrigger={lastBidTrigger}
          />
        </div>
      )}
    </div>
  );
};

export default ProductDetailPage;
