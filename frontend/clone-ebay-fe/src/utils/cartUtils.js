const CART_KEY = 'cloneEbayCart';

export function getCart() {
  try {
    return JSON.parse(localStorage.getItem(CART_KEY) || '[]');
  } catch {
    return [];
  }
}

export function saveCart(items) {
  localStorage.setItem(CART_KEY, JSON.stringify(items));
  window.dispatchEvent(new Event('cart-updated'));
}

export function getCartCount() {
  return getCart().reduce((sum, item) => sum + Number(item.quantity || 0), 0);
}

export function getCartItem(productId) {
  return getCart().find((item) => Number(item.productId) === Number(productId));
}

export function addToCart(product, quantity = 1) {
  const cart = getCart();
  const existing = cart.find((item) => Number(item.productId) === Number(product.id));
  const nextQty = Number(quantity || 1);

  if (existing) {
    existing.quantity = existing.quantity + nextQty;
  } else {
    cart.push({
      productId: Number(product.id),
      quantity: nextQty,
      title: product.title,
      price: Number(product.price || 0),
      image: product.images?.[0] || null,
      availableQuantity: Number(product.availableQuantity || 0),
      sellerId: product.sellerId,
    });
  }

  saveCart(cart);
}

export function updateCartItemQuantity(productId, quantity) {
  const qty = Number(quantity || 0);

  const cart = getCart()
    .map((item) =>
      Number(item.productId) === Number(productId)
        ? { ...item, quantity: qty }
        : item
    )
    .filter((item) => item.quantity > 0);

  saveCart(cart);
}

export function removeFromCart(productId) {
  const cart = getCart().filter(
    (item) => Number(item.productId) !== Number(productId)
  );
  saveCart(cart);
}

export function clearCart() {
  saveCart([]);
}
