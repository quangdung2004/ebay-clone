import axiosInstance from './axios';

export const closeAuction = async (productId) => {
    const response = await axiosInstance.post(`/products/${productId}/auction/close`);
    return response.data?.data ?? null;
};

export const getAuctionWinner = async (productId) => {
    const response = await axiosInstance.get(`/products/${productId}/auction/winner`);
    return response.data?.data ?? null;
};
