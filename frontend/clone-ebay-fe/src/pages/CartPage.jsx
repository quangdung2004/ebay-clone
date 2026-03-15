import { useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { getProductById } from '../api/productApi';
import { getMyAddresses } from '../api/addressApi';
import { checkoutOrder } from '../api/orderApi';
import { parseApiError } from '../utils/errorUtils';
import {
  clearCart,
  getCart,
  removeFromCart,
  updateCartItemQuantity,
} from '../utils/cartUtils';
import {
  formatCurrency,
  getPlaceholderImage,
  normalizeProductImageUrl,
} from '../utils/productUtils';
import { useToast } from '../components/Toast';
import './CartPage.css';

const DEFAULT_PAYMENT_METHOD = 'COD';
const CHECKOUT_ADDRESS_KEY = 'selectedShippingAddressId';

const getStoredSelectedAddressId = () => {
  const value = localStorage.getItem(CHECKOUT_ADDRESS_KEY);
  return value ? Number(value) : null;
};

const storeSelectedAddressId = (addressId) => {
  localStorage.setItem(CHECKOUT_ADDRESS_KEY, String(addressId));
};

const CartPage = () => {
  const navigate = useNavigate();
  const { showError, showSuccess } = useToast();

  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [checkingOut, setCheckingOut] = useState(false);
  const [selectedAddress, setSelectedAddress] = useState(null);
  const [selectedItemIds, setSelectedItemIds] = useState([]);

  const isItemAvailable = (product) => {
    return !!(
      product &&
      !product.isAuction &&
      product.inStock &&
      product.status !== 'SOLD' &&
      product.status !== 'OUT_OF_STOCK' &&
      product.status !== 'INACTIVE'
    );
  };

  const loadCart = async () => {
    setLoading(true);
    try {
      const raw = getCart();

      if (!raw.length) {
        setItems([]);
        setSelectedItemIds([]);
        return;
      }

      const products = await Promise.all(
        raw.map(async (item) => {
          try {
            const product = await getProductById(item.productId);
            return { ...item, product };
          } catch {
            return { ...item, product: null };
          }
        })
      );

      setItems(products);

      const selectableIds = products
        .filter((item) => isItemAvailable(item.product))
        .map((item) => Number(item.productId));

      setSelectedItemIds(selectableIds);
    } finally {
      setLoading(false);
    }
  };

  const loadAddresses = async () => {
    try {
      const addresses = await getMyAddresses();

      if (!addresses.length) {
        setSelectedAddress(null);
        return;
      }

      const storedId = getStoredSelectedAddressId();

      const chosen =
        addresses.find((item) => item.id === storedId) ||
        addresses.find((item) => item.isDefault) ||
        addresses[0];

      setSelectedAddress(chosen);

      if (chosen?.id) {
        storeSelectedAddressId(chosen.id);
      }
    } catch {
      setSelectedAddress(null);
    }
  };

  useEffect(() => {
    loadCart();
    loadAddresses();
  }, []);

  const groupedItems = useMemo(() => {
    const groups = new Map();

    items.forEach((item) => {
      const product = item.product;
      const sellerKey =
        product?.sellerId || item.sellerId || `seller-${item.productId}`;
      const sellerName =
        product?.sellerName || item.sellerName || `Seller #${sellerKey}`;

      if (!groups.has(sellerKey)) {
        groups.set(sellerKey, {
          sellerKey,
          sellerName,
          items: [],
        });
      }

      groups.get(sellerKey).items.push(item);
    });

    return Array.from(groups.values());
  }, [items]);

  const selectableIds = useMemo(() => {
    return items
      .filter((item) => isItemAvailable(item.product))
      .map((item) => Number(item.productId));
  }, [items]);

  const selectedItems = useMemo(() => {
    return items.filter((item) =>
      selectedItemIds.includes(Number(item.productId))
    );
  }, [items, selectedItemIds]);

  const selectedValidItems = useMemo(() => {
    return selectedItems.filter((item) => isItemAvailable(item.product));
  }, [selectedItems]);

  const total = useMemo(() => {
    return selectedValidItems.reduce((sum, item) => {
      return sum + Number(item.product?.price || item.price || 0) * Number(item.quantity || 0);
    }, 0);
  }, [selectedValidItems]);

  const allSelectableChecked =
    selectableIds.length > 0 &&
    selectableIds.every((id) => selectedItemIds.includes(id));

  const toggleSelectAll = () => {
    if (allSelectableChecked) {
      setSelectedItemIds([]);
    } else {
      setSelectedItemIds(selectableIds);
    }
  };

  const toggleSelectItem = (productId) => {
    const numericId = Number(productId);

    setSelectedItemIds((prev) =>
      prev.includes(numericId)
        ? prev.filter((id) => id !== numericId)
        : [...prev, numericId]
    );
  };

  const toggleSelectShop = (shopItems) => {
    const shopSelectableIds = shopItems
      .filter((item) => isItemAvailable(item.product))
      .map((item) => Number(item.productId));

    const allShopChecked =
      shopSelectableIds.length > 0 &&
      shopSelectableIds.every((id) => selectedItemIds.includes(id));

    setSelectedItemIds((prev) => {
      if (allShopChecked) {
        return prev.filter((id) => !shopSelectableIds.includes(id));
      }

      const next = new Set(prev);
      shopSelectableIds.forEach((id) => next.add(id));
      return Array.from(next);
    });
  };

  const onChangeQty = (productId, quantity, max) => {
    const nextQty = Math.max(1, Math.min(Number(quantity || 1), Number(max || 1)));

    updateCartItemQuantity(productId, nextQty);

    setItems((prev) =>
      prev.map((item) =>
        Number(item.productId) === Number(productId)
          ? { ...item, quantity: nextQty }
          : item
      )
    );
  };

  const increaseQty = (productId, currentQty, max) => {
    onChangeQty(productId, Number(currentQty) + 1, max);
  };

  const decreaseQty = (productId, currentQty, max) => {
    onChangeQty(productId, Number(currentQty) - 1, max);
  };

  const onRemove = (productId) => {
    removeFromCart(productId);
    setItems((prev) =>
      prev.filter((item) => Number(item.productId) !== Number(productId))
    );
    setSelectedItemIds((prev) =>
      prev.filter((id) => Number(id) !== Number(productId))
    );
  };

  const handleCheckout = async () => {
    if (!selectedValidItems.length) {
      showError('Vui lòng chọn sản phẩm hợp lệ để đặt hàng.');
      return;
    }

    if (!selectedAddress?.id) {
      showError('Vui lòng chọn địa chỉ giao hàng.');
      navigate('/addresses?redirect=/cart');
      return;
    }

    try {
      setCheckingOut(true);

      const payload = {
        paymentMethod: DEFAULT_PAYMENT_METHOD,
        addressId: selectedAddress.id,
        items: selectedValidItems.map((item) => ({
          productId: Number(item.productId),
          quantity: Number(item.quantity),
        })),
      };

      await checkoutOrder(payload);

      selectedValidItems.forEach((item) => removeFromCart(item.productId));

      showSuccess('Đặt hàng thành công.');
      navigate('/orders');
    } catch (error) {
      const parsed = parseApiError(error, 'Checkout failed');

      if (
        parsed.code === 'ADDRESS_NOT_FOUND' ||
        parsed.code === 'ADDRESS_REQUIRED'
      ) {
        navigate('/addresses?redirect=/cart');
      }

      if (
        parsed.code === 'INSUFFICIENT_STOCK' ||
        parsed.code === 'PRODUCT_NOT_AVAILABLE'
      ) {
        await loadCart();
      }

      showError(parsed.message);
    } finally {
      setCheckingOut(false);
    }
  };

  if (loading) {
    return (
      <div className="cart-page">
        <div className="cart-empty-card">Đang tải giỏ hàng...</div>
      </div>
    );
  }

  if (!items.length) {
    return (
      <div className="cart-page">
        <div className="cart-empty-card">
          <h2>Giỏ hàng của bạn đang trống</h2>
          <p>Hãy thêm sản phẩm từ trang chi tiết sản phẩm để tiếp tục.</p>
          <Link to="/" className="cart-buy-btn">
            Tiếp tục mua sắm
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="cart-page">
      <div className="cart-address-card" onClick={() => navigate('/addresses?redirect=/cart')}>
        <div className="cart-address-icon">📍</div>

        <div className="cart-address-content">
          <div className="cart-address-title">Địa chỉ nhận hàng</div>

          {selectedAddress ? (
            <>
              <div className="cart-address-name-line">
                <strong>{selectedAddress.fullName}</strong>
                <span>{selectedAddress.phone}</span>
              </div>
              <div className="cart-address-text">
                {selectedAddress.street}, {selectedAddress.state}, {selectedAddress.city}, {selectedAddress.country}
              </div>
            </>
          ) : (
            <div className="cart-address-placeholder">
              Bạn chưa chọn địa chỉ giao hàng. Bấm vào đây để chọn địa chỉ.
            </div>
          )}
        </div>

        <div className="cart-address-arrow">›</div>
      </div>

      <div className="cart-table-header">
        <label className="cart-checkbox-label cart-product-col">
          <input
            type="checkbox"
            checked={allSelectableChecked}
            onChange={toggleSelectAll}
          />
          <span>Sản phẩm</span>
        </label>
        <div className="cart-col">Đơn giá</div>
        <div className="cart-col">Số lượng</div>
        <div className="cart-col">Số tiền</div>
        <div className="cart-col">Thao tác</div>
      </div>

      <div className="cart-list">
        {groupedItems.map((group) => {
          const shopSelectableIds = group.items
            .filter((item) => isItemAvailable(item.product))
            .map((item) => Number(item.productId));

          const shopChecked =
            shopSelectableIds.length > 0 &&
            shopSelectableIds.every((id) => selectedItemIds.includes(id));

          return (
            <section className="cart-shop-block" key={group.sellerKey}>
              <div className="cart-shop-header">
                <label className="cart-checkbox-label">
                  <input
                    type="checkbox"
                    checked={shopChecked}
                    onChange={() => toggleSelectShop(group.items)}
                    disabled={!shopSelectableIds.length}
                  />
                  <span className="cart-shop-name">{group.sellerName}</span>
                </label>
              </div>

              {group.items.map((item) => {
                const product = item.product;
                const unavailable = !isItemAvailable(product);
                const image = normalizeProductImageUrl(product?.images?.[0] || item.image);
                const rowTotal = Number(product?.price || item.price || 0) * Number(item.quantity || 0);
                const checked = selectedItemIds.includes(Number(item.productId));

                return (
                  <div
                    key={item.productId}
                    className={`cart-item-row ${unavailable ? 'is-unavailable' : ''}`}
                  >
                    <div className="cart-product-col cart-product-info">
                      <label className="cart-row-check">
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={() => toggleSelectItem(item.productId)}
                          disabled={unavailable}
                        />
                      </label>

                      <Link to={`/products/${item.productId}`} className="cart-product-link">
                        <img
                          src={image || getPlaceholderImage()}
                          alt={product?.title || item.title}
                          className="cart-product-image"
                        />
                      </Link>

                      <div className="cart-product-main">
                        <Link to={`/products/${item.productId}`} className="cart-product-title">
                          {product?.title || item.title}
                        </Link>

                        <div className="cart-product-sub">
                          {unavailable
                            ? 'Sản phẩm hiện không thể checkout'
                            : `${product.availableQuantity} sản phẩm có sẵn`}
                        </div>
                      </div>
                    </div>

                    <div className="cart-col cart-unit-price">
                      {formatCurrency(product?.price || item.price || 0)}
                    </div>

                    <div className="cart-col">
                      <div className="cart-qty-box">
                        <button
                          type="button"
                          className="cart-qty-btn"
                          onClick={() =>
                            decreaseQty(
                              item.productId,
                              item.quantity,
                              product?.availableQuantity || 1
                            )
                          }
                          disabled={unavailable || Number(item.quantity) <= 1}
                        >
                          −
                        </button>

                        <input
                          type="number"
                          min="1"
                          max={product?.availableQuantity || 1}
                          value={item.quantity}
                          onChange={(e) =>
                            onChangeQty(
                              item.productId,
                              e.target.value,
                              product?.availableQuantity || 1
                            )
                          }
                          disabled={unavailable}
                          className="cart-qty-input"
                        />

                        <button
                          type="button"
                          className="cart-qty-btn"
                          onClick={() =>
                            increaseQty(
                              item.productId,
                              item.quantity,
                              product?.availableQuantity || 1
                            )
                          }
                          disabled={
                            unavailable ||
                            Number(item.quantity) >= Number(product?.availableQuantity || 1)
                          }
                        >
                          +
                        </button>
                      </div>
                    </div>

                    <div className="cart-col cart-line-total">
                      {formatCurrency(rowTotal)}
                    </div>

                    <div className="cart-col cart-actions-col">
                      <button
                        type="button"
                        className="cart-remove-btn"
                        onClick={() => onRemove(item.productId)}
                      >
                        Xóa
                      </button>
                    </div>
                  </div>
                );
              })}
            </section>
          );
        })}
      </div>

      <div className="cart-bottom-bar">
        <div className="cart-bottom-left">
          <label className="cart-checkbox-label">
            <input
              type="checkbox"
              checked={allSelectableChecked}
              onChange={toggleSelectAll}
            />
            <span>Chọn Tất Cả ({selectableIds.length})</span>
          </label>

          <button
            type="button"
            className="cart-bottom-text-btn"
            onClick={() => {
              clearCart();
              setItems([]);
              setSelectedItemIds([]);
            }}
          >
            Xóa tất cả
          </button>
        </div>

        <div className="cart-bottom-right">
          <div className="cart-summary-text">
            <span>
              Tổng cộng ({selectedValidItems.length} sản phẩm):
            </span>
            <strong>{formatCurrency(total)}</strong>
          </div>

          <button
            type="button"
            className="cart-buy-btn"
            onClick={handleCheckout}
            disabled={checkingOut || !selectedValidItems.length}
          >
            {checkingOut ? 'Đang xử lý...' : 'Mua Hàng'}
          </button>
        </div>
      </div>
    </div>
  );
};

export default CartPage;