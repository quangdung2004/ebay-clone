import axiosInstance from './axios';

export const getSellerWallet = async () => {
  const response = await axiosInstance.get('/seller/wallet');
  return response.data?.data;
};

export const getSellerSettlements = async (page = 1, pageSize = 20) => {
  const response = await axiosInstance.get('/seller/wallet/settlements', {
    params: { page, pageSize },
  });
  return response.data?.data;
};

// Shipping / Order Management actions for Seller
export const getSellerOrders = async (page = 1, pageSize = 20) => {
  const response = await axiosInstance.get('/seller/orders', {
    params: { page, pageSize }
  });
  return response.data?.data;
};

export const getOrderForSeller = async (orderId) => {
  const response = await axiosInstance.get(`/seller/orders/${orderId}`);
  return response.data?.data;
};

export const markOrderProcessing = async (orderId) => {
  const response = await axiosInstance.post(`/seller/orders/${orderId}/processing`);
  return response.data?.data;
};

export const shipOrder = async (orderId, payload) => {
  // payload: { carrier, trackingNumber, estimatedArrival, provider }
  const response = await axiosInstance.post(`/seller/orders/${orderId}/ship`, payload);
  return response.data?.data;
};

export const applyMockShipmentStatus = async (orderId, payload) => {
  // payload: { status, description, location, eventTime }
  const response = await axiosInstance.post(`/seller/orders/${orderId}/mock-status`, payload);
  return response.data?.data;
};
