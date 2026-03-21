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
