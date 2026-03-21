import axiosInstance from './axios';

export const getMyOrders = async (page = 1, pageSize = 20) => {
  const response = await axiosInstance.get('/orders/my', {
    params: { page, pageSize },
  });
  return response.data?.data;
};

export const getOrderById = async (id) => {
  const response = await axiosInstance.get(`/orders/${id}`);
  return response.data?.data;
};

export const payOrder = async (id) => {
  const response = await axiosInstance.post(`/orders/${id}/pay`);
  return response.data?.data;
};

// ALias for clarity
export const createPayPalOrder = payOrder;

export const capturePayPalOrder = async (id, paypalOrderId) => {
  const response = await axiosInstance.post(`/orders/${id}/pay/capture`, {
    paypalOrderId,
  });
  return response.data;
};

export const cancelOrder = async (id) => {
  const response = await axiosInstance.post(`/orders/${id}/cancel`);
  return response.data?.data;
};

export const updateOrderAddress = async (id, addressId) => {
  const response = await axiosInstance.put(`/orders/${id}/address`, {
    addressId,
  });
  return response.data?.data;
};

export const checkoutOrder = async (payload) => {
  const response = await axiosInstance.post('/orders/checkout', payload);
  return response.data?.data;
};

export const getOrderTracking = async (id) => {
  const response = await axiosInstance.get(`/orders/${id}/tracking`);
  return response.data?.data;
};