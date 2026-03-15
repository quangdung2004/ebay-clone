import axiosInstance from './axios';

export const getMyAddresses = async () => {
  const response = await axiosInstance.get('/addresses/my');
  return response.data?.data || [];
};

export const createAddress = async (payload) => {
  const response = await axiosInstance.post('/addresses', payload);
  return response.data?.data;
};

export const updateAddress = async (id, payload) => {
  const response = await axiosInstance.put(`/addresses/${id}`, payload);
  return response.data?.data;
};
