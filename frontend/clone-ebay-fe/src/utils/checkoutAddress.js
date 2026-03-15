const CHECKOUT_ADDRESS_KEY = 'selectedShippingAddressId';

export function getSelectedShippingAddressId() {
  const value = localStorage.getItem(CHECKOUT_ADDRESS_KEY);
  return value ? Number(value) : null;
}

export function setSelectedShippingAddressId(addressId) {
  localStorage.setItem(CHECKOUT_ADDRESS_KEY, String(addressId));
}

export function clearSelectedShippingAddressId() {
  localStorage.removeItem(CHECKOUT_ADDRESS_KEY);
}
