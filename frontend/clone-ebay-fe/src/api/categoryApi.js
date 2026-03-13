import axiosInstance from './axios';

/**
 * @returns {Promise<Array<{id: number, name: string}>>}
 */
export const getCategories = async () => {
    const response = await axiosInstance.get('/categories');
    return response.data.data; // unwrap ApiResponse<T>
};
