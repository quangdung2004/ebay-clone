import axiosInstance from './axios';

export const placeBid = async (productId, amount) => {
    const response = await axiosInstance.post(`/products/${productId}/bids`, { amount });
    return response.data?.data ?? null;
};

export const getBids = async (productId, page = 1, pageSize = 20) => {
    const response = await axiosInstance.get(`/products/${productId}/bids`, {
        params: { page, pageSize }
    });
    return response.data?.data ?? null;
};
